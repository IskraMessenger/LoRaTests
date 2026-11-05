using System.Management;
using System.Text;
using Microsoft.Win32;

namespace HeltecLoRaTerminal;

internal static class PortDiagnostics
{
    private static readonly string[] SerialHints =
    [
        "COM", "Serial", "UART", "CP210", "CH340", "CH910", "USB-SERIAL",
        "Silicon Labs", "QinHeng", "Espressif", "USB Serial Device", "CDC"
    ];

    public static string[] GetPortNames()
    {
        var fromApi = SerialPortNamesSafe();
        var fromRegistry = GetRegistryPortNames();
        return fromApi.Concat(fromRegistry)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string BuildNoPortReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("В Windows нет ни одного COM-порта (не проблема приложения).");
        sb.AppendLine("Проверьте кабель DATA (не charge-only), порт USB 2.0 без хаба, питание платы.");
        sb.AppendLine("Heltec V2/V3 обычно: Silicon Labs CP210x. Нужен драйвер CP210x, не «просто USB».");
        sb.AppendLine("В Device Manager должно быть: Ports (COM & LPT) → Silicon Labs CP210x (COMx).");
        sb.AppendLine();

        var suspects = QueryUsbSuspects();
        if (suspects.Count == 0)
        {
            sb.AppendLine("USB-устройств, похожих на UART/Heltec, сейчас не видно.");
            sb.AppendLine("Плата, скорее всего, не подключена или не отвечает по USB.");
        }
        else
        {
            sb.AppendLine("Подозрительные USB/serial устройства:");
            foreach (var line in suspects)
                sb.AppendLine("  • " + line);
        }

        return sb.ToString().TrimEnd();
    }

    private static string[] SerialPortNamesSafe()
    {
        try
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string[] GetRegistryPortNames()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
            if (key is null)
                return Array.Empty<string>();

            return key.GetValueNames()
                .Select(n => key.GetValue(n) as string)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Cast<string>()
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static List<string> QueryUsbSuspects()
    {
        var result = new List<string>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Status, ConfigManagerErrorCode, PNPDeviceID FROM Win32_PnPEntity");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? string.Empty;
                var status = obj["Status"]?.ToString() ?? string.Empty;
                var pnp = obj["PNPDeviceID"]?.ToString() ?? string.Empty;
                var err = obj["ConfigManagerErrorCode"]?.ToString() ?? "0";

                var interesting =
                    name.Contains("Unknown USB Device", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Device Descriptor Request Failed", StringComparison.OrdinalIgnoreCase) ||
                    SerialHints.Any(h => name.Contains(h, StringComparison.OrdinalIgnoreCase)) ||
                    pnp.Contains("VID_10C4", StringComparison.OrdinalIgnoreCase) || // CP210x
                    pnp.Contains("VID_1A86", StringComparison.OrdinalIgnoreCase) || // CH340
                    pnp.Contains("VID_303A", StringComparison.OrdinalIgnoreCase) || // Espressif
                    pnp.Contains("VID_0403", StringComparison.OrdinalIgnoreCase);  // FTDI

                if (!interesting)
                    continue;

                result.Add($"{name} | status={status}, code={err} | {ShortPnp(pnp)}");
            }
        }
        catch (Exception ex)
        {
            result.Add("WMI недоступен: " + ex.Message);
        }

        return result;
    }

    private static string ShortPnp(string pnp)
    {
        if (string.IsNullOrEmpty(pnp))
            return "-";
        var parts = pnp.Split('\\');
        return parts.Length >= 2 ? parts[0] + "\\" + parts[1] : pnp;
    }
}
