using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace MonitorServicePlugin;

public class MonitorService
{
    public static IEnumerable<MonitorInfo> GetMonitors()
    {
        var list = new List<MonitorInfo>();
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var searcher = new ManagementObjectSearcher(@"root\\WMI", "SELECT * FROM WmiMonitorDescriptor");
                foreach (ManagementObject mo in searcher.Get())
                {
                    try
                    {
                        var type = (ushort)(mo["DescriptorType"] ?? 0);
                        if (type != 1) continue; // 1 indicates EDID
                        if (mo["Descriptor"] is byte[] edid)
                        {
                            var info = ParseEdid(edid);
                            if (info != null) list.Add(info);
                        }
                    }
                    catch
                    {
                        // ignore malformed entries
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                foreach (var file in Directory.EnumerateFiles("/sys/class/drm", "edid", SearchOption.AllDirectories))
                {
                    try
                    {
                        var edid = File.ReadAllBytes(file);
                        var info = ParseEdid(edid);
                        if (info != null) list.Add(info);
                    }
                    catch
                    {
                        // ignore errors reading individual files
                    }
                }
            }
        }
        catch
        {
            // ignore failures on unsupported platforms
        }
        return list;
    }

    private static MonitorInfo? ParseEdid(byte[] edid)
    {
        if (edid.Length < 128) return null;
        string manufacturer = ParseManufacturer((ushort)((edid[8] << 8) | edid[9]));
        string serial = BitConverter.ToString(edid, 12, 4).Replace("-", string.Empty);
        string model = string.Empty;

        for (int i = 54; i <= 108; i += 18)
        {
            if (edid[i] == 0x00 && edid[i + 1] == 0x00 && edid[i + 3] == 0xFC)
            {
                model = Encoding.ASCII.GetString(edid, i + 5, 13).Trim('\0', '\n', '\r', ' ');
                break;
            }
        }

        if (string.IsNullOrEmpty(model))
        {
            model = (edid[11] << 8 | edid[10]).ToString("X4");
        }

        return new MonitorInfo(manufacturer, model, serial);
    }

    private static string ParseManufacturer(ushort code)
    {
        char c1 = (char)('A' + ((code >> 10) & 0x1F) - 1);
        char c2 = (char)('A' + ((code >> 5) & 0x1F) - 1);
        char c3 = (char)('A' + (code & 0x1F) - 1);
        return new string(new[] { c1, c2, c3 });
    }

    public record MonitorInfo(string Manufacturer, string Model, string SerialNumber);
}

