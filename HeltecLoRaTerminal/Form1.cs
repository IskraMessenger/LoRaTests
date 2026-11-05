using System.IO.Ports;
using System.Text;

namespace HeltecLoRaTerminal;

public partial class Form1 : Form
{
    private SerialPort? _serial;
    private readonly StringBuilder _lineBuffer = new();
    private readonly object _lineLock = new();
    private string? _pendingPayload;
    private int? _pendingRssi;
    private int? _pendingSnr;
    private int? _pendingSize;
    private DateTime _lastRxTimeoutLog = DateTime.MinValue;
    private bool _rearmRxInProgress;

    public Form1()
    {
        InitializeComponent();
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        cmbBaud.SelectedItem = "115200";
        cmbSf.SelectedItem = "7";
        cmbBw.SelectedIndex = 0;
        RefreshPorts();
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e) => Disconnect();

    private void BtnRefreshPorts_Click(object? sender, EventArgs e) => RefreshPorts();

    private void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_serial?.IsOpen == true)
        {
            Disconnect();
            return;
        }

        Connect();
    }

    private async void BtnInitHeltec_Click(object? sender, EventArgs e)
    {
        btnInitHeltec.Enabled = false;
        try
        {
            await InitHeltecAsync().ConfigureAwait(true);
        }
        finally
        {
            btnInitHeltec.Enabled = _serial?.IsOpen == true;
        }
    }

    private async void BtnListen_Click(object? sender, EventArgs e)
    {
        try
        {
            await SendAtAsync(HeltecAtProtocol.EnterRxContinuous()).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppendError($"Ошибка RX: {ex.Message}");
        }
    }

    private async void BtnSend_Click(object? sender, EventArgs e)
    {
        btnSend.Enabled = false;
        try
        {
            await SendHeltecHexAsync().ConfigureAwait(true);
        }
        finally
        {
            btnSend.Enabled = _serial?.IsOpen == true;
        }
    }

    private void BtnClearReceived_Click(object? sender, EventArgs e) => lstReceived.Items.Clear();

    private void BtnClearErrors_Click(object? sender, EventArgs e) => txtErrors.Clear();

    private void RefreshPorts()
    {
        var selected = cmbPort.SelectedItem as string;
        cmbPort.Items.Clear();

        try
        {
            var ports = PortDiagnostics.GetPortNames();
            cmbPort.Items.AddRange(ports);

            if (ports.Length == 0)
            {
                AppendError(PortDiagnostics.BuildNoPortReport());
                return;
            }

            if (!string.IsNullOrEmpty(selected) && cmbPort.Items.Contains(selected))
                cmbPort.SelectedItem = selected;
            else
                cmbPort.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            AppendError($"Ошибка списка портов: {ex.Message}");
        }
    }

    private void Connect()
    {
        if (cmbPort.SelectedItem is not string portName || string.IsNullOrWhiteSpace(portName))
        {
            AppendError("Выберите COM-порт.");
            return;
        }

        if (cmbBaud.SelectedItem is not string baudText || !int.TryParse(baudText, out var baud))
        {
            AppendError("Некорректная скорость.");
            return;
        }

        try
        {
            Disconnect();

            // Heltec manual: DTR/RTS not required. Keep them off to avoid ESP32 reset loops.
            _serial = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                Encoding = Encoding.ASCII,
                NewLine = "\n",
                ReadTimeout = 500,
                WriteTimeout = 2000,
                DtrEnable = false,
                RtsEnable = false
            };

            _serial.DataReceived += Serial_DataReceived;
            _serial.ErrorReceived += Serial_ErrorReceived;
            _serial.Open();

            lock (_lineLock)
                _lineBuffer.Clear();

            SetConnectedUi(true);
        }
        catch (Exception ex)
        {
            AppendError($"Ошибка подключения к {portName}: {ex.Message}");
            Disconnect();
        }
    }

    private void Disconnect()
    {
        try
        {
            if (_serial != null)
            {
                _serial.DataReceived -= Serial_DataReceived;
                _serial.ErrorReceived -= Serial_ErrorReceived;

                if (_serial.IsOpen)
                    _serial.Close();

                _serial.Dispose();
                _serial = null;
            }
        }
        catch (Exception ex)
        {
            AppendError($"Ошибка отключения: {ex.Message}");
        }
        finally
        {
            SetConnectedUi(false);
        }
    }

    private void SetConnectedUi(bool connected)
    {
        btnConnect.Text = connected ? "Отключить" : "Подключить";
        btnSend.Enabled = connected;
        btnInitHeltec.Enabled = connected;
        btnListen.Enabled = connected;
        cmbPort.Enabled = !connected;
        cmbBaud.Enabled = !connected;
        btnRefreshPorts.Enabled = !connected;
    }

    private async Task InitHeltecAsync()
    {
        try
        {
            var sf = int.Parse((string)cmbSf.SelectedItem!);
            var bw = cmbBw.SelectedIndex; // 0=125k, 1=250k, 2=500k
            var freq = (uint)nudFreq.Value;
            var power = (int)nudPower.Value;

            // Wake + disable sleep so continuous RX works.
            await SendAtAsync(HeltecAtProtocol.Wake()).ConfigureAwait(true);
            await Task.Delay(200).ConfigureAwait(true);
            await SendAtAsync(HeltecAtProtocol.DisableAutoLowPower()).ConfigureAwait(true);
            await Task.Delay(80).ConfigureAwait(true);
            await SendAtAsync(HeltecAtProtocol.SetLoRaNormalMode()).ConfigureAwait(true);
            await Task.Delay(250).ConfigureAwait(true);
            await SendAtAsync(HeltecAtProtocol.SetPrintHex()).ConfigureAwait(true);
            await Task.Delay(80).ConfigureAwait(true);
            await SendAtAsync(HeltecAtProtocol.SetLoraParams(
                freq, power, sf, bw, codeRate: 1, preambleLength: 8,
                crcOn: true, iqInvert: false, saveToFlash: false)).ConfigureAwait(true);
            await Task.Delay(120).ConfigureAwait(true);
            await SendAtAsync(HeltecAtProtocol.EnterRxContinuous()).ConfigureAwait(true);

            AppendStatus($"Init OK: {freq} Hz, SF{sf}, BW{bw}, {power} dBm, RX armed");
        }
        catch (Exception ex)
        {
            AppendError($"Init Heltec: {ex.Message}");
        }
    }

    private async Task SendHeltecHexAsync()
    {
        if (_serial?.IsOpen != true)
        {
            AppendError("Порт не открыт.");
            return;
        }

        try
        {
            var bytes = ParseHex(txtSendHex.Text);
            if (bytes.Length == 0)
            {
                AppendError("Нечего отправлять: пустой или некорректный hex.");
                return;
            }

            var cmd = HeltecAtProtocol.SendHex(bytes);
            await SendAtAsync(cmd).ConfigureAwait(true);

            if (chkAutoListen.Checked)
            {
                await Task.Delay(150).ConfigureAwait(true);
                await SendAtAsync(HeltecAtProtocol.EnterRxContinuous()).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            AppendError($"Ошибка отправки: {ex.Message}");
        }
    }

    private Task SendAtAsync(string command)
    {
        if (_serial?.IsOpen != true)
            throw new InvalidOperationException("Порт не открыт.");

        // Heltec AT manual: no CR/LF terminator.
        var payload = Encoding.ASCII.GetBytes(command);
        _serial.Write(payload, 0, payload.Length);
        return Task.CompletedTask;
    }

    private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serial?.IsOpen != true)
                return;

            var chunk = _serial.ReadExisting();
            if (string.IsNullOrEmpty(chunk))
                return;

            List<string> lines;
            lock (_lineLock)
            {
                _lineBuffer.Append(chunk);
                lines = ExtractLines(_lineBuffer);
            }

            if (lines.Count == 0)
                return;

            BeginInvoke(() =>
            {
                foreach (var line in lines)
                    HandleHeltecLine(line);
            });
        }
        catch (Exception ex)
        {
            BeginInvoke(() => AppendError($"Ошибка приёма: {ex.Message}"));
        }
    }

    private void Serial_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        BeginInvoke(() => AppendError($"Ошибка последовательного порта: {e.EventType}"));
    }

    private static List<string> ExtractLines(StringBuilder buffer)
    {
        var text = buffer.ToString();
        var lines = new List<string>();
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is not ('\n' or '\r'))
                continue;

            if (i > start)
                lines.Add(text[start..i]);

            if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                i++;

            start = i + 1;
        }

        buffer.Clear();
        if (start < text.Length)
            buffer.Append(text[start..]);

        return lines;
    }

    private void HandleHeltecLine(string line)
    {
        var kind = HeltecAtProtocol.Classify(line, out var rx);

        switch (kind)
        {
            case HeltecLineKind.Error:
                AppendError(line);
                break;

            case HeltecLineKind.RxTimeout:
                // Normal when nobody transmits — do not spam the error box.
                if (DateTime.Now - _lastRxTimeoutLog > TimeSpan.FromSeconds(5))
                {
                    _lastRxTimeoutLog = DateTime.Now;
                    AppendStatus("RX Timeout — пакетов нет, слушаю снова");
                }

                if (chkAutoListen.Checked)
                    _ = RearmRxAsync();
                break;

            case HeltecLineKind.ReceivedPayload when rx is not null:
                _pendingPayload = rx.PayloadHex ?? rx.Text ?? string.Empty;
                _pendingRssi = null;
                _pendingSnr = null;
                _pendingSize = null;
                // If RSSI line is missing, still show the payload shortly after.
                _ = FlushPendingAfterDelayAsync();
                break;

            case HeltecLineKind.RssiInfo when rx is not null:
                _pendingRssi = rx.Rssi;
                _pendingSnr = rx.Snr;
                _pendingSize = rx.Size;
                FlushPendingRxIfNeeded(force: true);
                break;

            case HeltecLineKind.Status:
            case HeltecLineKind.Other:
                break;
        }
    }

    private async Task RearmRxAsync()
    {
        if (_rearmRxInProgress || _serial?.IsOpen != true)
            return;

        _rearmRxInProgress = true;
        try
        {
            await Task.Delay(50).ConfigureAwait(true);
            await SendAtAsync(HeltecAtProtocol.EnterRxContinuous()).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppendError($"Не удалось снова включить RX: {ex.Message}");
        }
        finally
        {
            _rearmRxInProgress = false;
        }
    }

    private async Task FlushPendingAfterDelayAsync()
    {
        await Task.Delay(120).ConfigureAwait(true);
        FlushPendingRxIfNeeded(force: true);
    }

    private void FlushPendingRxIfNeeded(bool force = false)
    {
        if (_pendingPayload is null)
            return;

        if (!force && _pendingRssi is null)
            return;

        var stamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var meta = _pendingRssi is int rssi
            ? $"  RSSI={rssi} SNR={_pendingSnr} Size={_pendingSize}"
            : string.Empty;

        lstReceived.Items.Add($"[{stamp}] {_pendingPayload}{meta}");
        lstReceived.TopIndex = lstReceived.Items.Count - 1;

        _pendingPayload = null;
        _pendingRssi = null;
        _pendingSnr = null;
        _pendingSize = null;
    }

    private void AppendError(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendError(message));
            return;
        }

        var stamp = DateTime.Now.ToString("HH:mm:ss.fff");
        txtErrors.AppendText($"[{stamp}] {message}{Environment.NewLine}");
    }

    private void AppendStatus(string message)
    {
        // Non-error operational note also goes to errors field only if needed —
        // put into list as status line for visibility without mixing RX.
        var stamp = DateTime.Now.ToString("HH:mm:ss.fff");
        lstReceived.Items.Add($"[{stamp}] STATUS: {message}");
        lstReceived.TopIndex = lstReceived.Items.Count - 1;
    }

    private static byte[] ParseHex(string input)
    {
        var cleaned = input
            .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
            .Replace(",", " ")
            .Replace("-", " ")
            .Replace(":", " ")
            .Trim();

        if (string.IsNullOrWhiteSpace(cleaned))
            return Array.Empty<byte>();

        var tokens = cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var bytes = new List<byte>(tokens.Length);

        foreach (var token in tokens)
        {
            if (token.Length % 2 != 0)
                throw new FormatException($"Нечётная длина hex-токена: '{token}'");

            for (var i = 0; i < token.Length; i += 2)
            {
                var pair = token.Substring(i, 2);
                if (!byte.TryParse(pair, System.Globalization.NumberStyles.HexNumber, null, out var value))
                    throw new FormatException($"Некорректный hex: '{pair}'");
                bytes.Add(value);
            }
        }

        return bytes.ToArray();
    }
}
