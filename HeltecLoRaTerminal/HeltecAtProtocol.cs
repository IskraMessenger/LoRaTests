using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HeltecLoRaTerminal;

/// <summary>
/// Heltec AT firmware (CubeCell / AT_Command sketch): LoRa Normal Mode P2P.
/// Docs: CubeCell Series AT Command User Manual — no CR/LF terminator.
/// </summary>
internal static class HeltecAtProtocol
{
    private static readonly Regex RssiLine = new(
        @"RSSI\s*[:=]\s*(-?\d+)\s*,\s*SNR\s*[:=]\s*(-?\d+)(?:\s*,\s*Size\s*[:=]\s*(\d+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Wake() => "AT+XXX";

    public static string DisableAutoLowPower() => "AT+AutoLPM=0";

    public static string SetLoRaNormalMode() => "AT+LORAWAN=0";

    public static string SetPrintHex() => "AT+PrintMode=1";

    public static string SetFrequency(uint hz) => $"AT+FREQ={hz}";

    public static string SetLoraParams(
        uint frequencyHz,
        int powerDbm,
        int spreadingFactor,
        int bandwidthIndex,
        int codeRate,
        int preambleLength,
        bool crcOn,
        bool iqInvert,
        bool saveToFlash)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"AT+LoraSet={frequencyHz},{powerDbm},{spreadingFactor},{bandwidthIndex},{codeRate},{preambleLength},{(crcOn ? 1 : 0)},{(iqInvert ? 1 : 0)},{(saveToFlash ? 1 : 0)}");
    }

    /// <summary>
    /// Continuous listen. Some Heltec builds treat 0 oddly and end with RX Timeout —
    /// use a long window; the host re-arms on timeout.
    /// </summary>
    public static string EnterRxContinuous() => "AT+RX=60000";

    public static string EnterRxTimeout(uint timeoutMs) => $"AT+RX={timeoutMs}";

    public static string SendHex(ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0)
            throw new ArgumentException("Пустой payload.");
        if (payload.Length > 64)
            throw new ArgumentException("Heltec AT+SendHex: максимум 64 байта.");

        var sb = new StringBuilder(12 + payload.Length * 2);
        sb.Append("AT+SendHex=");
        foreach (var b in payload)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }

    public static string QueryChipId() => "AT+ChipID=?";

    public static string QueryBusy() => "AT+Busy=?";

    public static HeltecLineKind Classify(string line, out HeltecRxMessage? rx)
    {
        rx = null;
        if (string.IsNullOrWhiteSpace(line))
            return HeltecLineKind.Ignore;

        var trimmed = line.Trim();

        if (trimmed.StartsWith("+ERROR", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("Rx error", StringComparison.OrdinalIgnoreCase))
            return HeltecLineKind.Error;

        if (trimmed.Contains("RX Timeout", StringComparison.OrdinalIgnoreCase))
            return HeltecLineKind.RxTimeout;

        if (trimmed.Contains("TX timeout", StringComparison.OrdinalIgnoreCase))
            return HeltecLineKind.Error;

        if (trimmed.StartsWith("Received Hex:", StringComparison.OrdinalIgnoreCase))
        {
            var hex = trimmed["Received Hex:".Length..].Trim();
            rx = new HeltecRxMessage(FormatHexSpaced(hex), null, null, null, null, true);
            return HeltecLineKind.ReceivedPayload;
        }

        if (trimmed.StartsWith("Received String:", StringComparison.OrdinalIgnoreCase))
        {
            var text = trimmed["Received String:".Length..];
            rx = new HeltecRxMessage(ToHexSpaced(Encoding.ASCII.GetBytes(text)), text, null, null, null, false);
            return HeltecLineKind.ReceivedPayload;
        }

        // ESP32 LoRaWAN-style: +REV DATA:AABBCC
        if (trimmed.StartsWith("+REV DATA:", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("RXSIZE", StringComparison.OrdinalIgnoreCase))
        {
            var hex = trimmed["+REV DATA:".Length..].Trim();
            if (LooksLikeHex(hex))
            {
                rx = new HeltecRxMessage(FormatHexSpaced(hex), null, null, null, null, true);
                return HeltecLineKind.ReceivedPayload;
            }
        }

        var m = RssiLine.Match(trimmed);
        if (m.Success)
        {
            int? size = m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : null;
            rx = new HeltecRxMessage(
                null,
                null,
                int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                size,
                true);
            return HeltecLineKind.RssiInfo;
        }

        if (trimmed.StartsWith("+OK", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("TX done", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Contains("ASR is Waked", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+LORAWAN", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+LoraPara", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+FREQ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+PrintMode", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+RX", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+Busy", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+ChipID", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+Send Hex", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("+Send String", StringComparison.OrdinalIgnoreCase))
            return HeltecLineKind.Status;

        return HeltecLineKind.Other;
    }

    private static bool LooksLikeHex(string s)
    {
        if (string.IsNullOrWhiteSpace(s) || s.Length % 2 != 0)
            return false;
        foreach (var c in s)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }

        return true;
    }

    private static string FormatHexSpaced(string compactHex)
    {
        var cleaned = compactHex.Replace(" ", "", StringComparison.Ordinal);
        if (cleaned.Length == 0)
            return string.Empty;

        var sb = new StringBuilder(cleaned.Length + cleaned.Length / 2);
        for (var i = 0; i < cleaned.Length; i += 2)
        {
            if (i > 0)
                sb.Append(' ');
            if (i + 1 < cleaned.Length)
                sb.Append(cleaned.AsSpan(i, 2));
            else
                sb.Append(cleaned[i]);
        }

        return sb.ToString().ToUpperInvariant();
    }

    private static string ToHexSpaced(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder(data.Length * 3);
        for (var i = 0; i < data.Length; i++)
        {
            if (i > 0)
                sb.Append(' ');
            sb.Append(data[i].ToString("X2"));
        }

        return sb.ToString();
    }
}

internal enum HeltecLineKind
{
    Ignore,
    Status,
    Error,
    RxTimeout,
    ReceivedPayload,
    RssiInfo,
    Other
}

internal sealed record HeltecRxMessage(
    string? PayloadHex,
    string? Text,
    int? Rssi,
    int? Snr,
    int? Size = null,
    bool IsHex = true);
