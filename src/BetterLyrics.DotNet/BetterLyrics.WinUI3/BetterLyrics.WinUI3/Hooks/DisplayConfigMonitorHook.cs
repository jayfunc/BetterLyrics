using BetterLyrics.Core.Models.Domain;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BetterLyrics.WinUI3.Hooks;

/// <summary>
/// Bridges the GDI display names (<c>\\.\DISPLAY1</c>) used by the rest of the app to the stable
/// monitor device paths exposed by the Windows Display Configuration (CCD) API.
/// </summary>
public static class DisplayConfigMonitorHook
{
    private const int ErrorInsufficientBuffer = 122;
    private const uint QdcAllPaths = 0x1;
    private const uint QdcOnlyActivePaths = 0x2;
    private const uint DisplayConfigPathActive = 0x1;

    private const uint GetSourceNameType = 1;
    private const uint GetTargetNameType = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathSourceInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigRational
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathTargetInfo
    {
        public Luid AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint OutputTechnology;
        public uint Rotation;
        public uint Scaling;
        public DisplayConfigRational RefreshRate;
        public uint ScanLineOrdering;

        [MarshalAs(UnmanagedType.Bool)]
        public bool TargetAvailable;

        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigPathInfo
    {
        public DisplayConfigPathSourceInfo SourceInfo;
        public DisplayConfigPathTargetInfo TargetInfo;
        public uint Flags;
    }

    // The contents of the union are not needed; only the size matters to QueryDisplayConfig.
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct DisplayConfigModeInfo
    {
        public uint InfoType;
        public uint Id;
        public Luid AdapterId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DisplayConfigDeviceInfoHeader
    {
        public uint Type;
        public uint Size;
        public Luid AdapterId;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayConfigSourceDeviceName
    {
        public DisplayConfigDeviceInfoHeader Header;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ViewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayConfigTargetDeviceName
    {
        public DisplayConfigDeviceInfoHeader Header;
        public uint Flags;
        public uint OutputTechnology;
        public ushort EdidManufactureId;
        public ushort EdidProductCodeId;
        public uint ConnectorInstance;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string MonitorFriendlyDeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string MonitorDevicePath;
    }

    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPath, out uint numMode);

    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPath,
        [Out] DisplayConfigPathInfo[] pathArray,
        ref uint numMode,
        [Out] DisplayConfigModeInfo[] modeArray,
        IntPtr topologyId);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    private static extern int GetSourceName(ref DisplayConfigSourceDeviceName packet);

    [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    private static extern int GetTargetName(ref DisplayConfigTargetDeviceName packet);

    /// <summary>
    /// Enumerates monitors using DisplayConfig. Returns an empty list when the API is unavailable
    /// (for example in a remote session); callers are expected to fall back to
    /// <c>EnumDisplayDevices</c> in that case.
    /// </summary>
    public static IReadOnlyList<MonitorInfo> GetMonitors(bool activeOnly = true)
    {
        try
        {
            return GetMonitorsCore(activeOnly);
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<MonitorInfo> GetMonitorsCore(bool activeOnly)
    {
        var flags = activeOnly ? QdcOnlyActivePaths : QdcAllPaths;
        var result = new Dictionary<string, MonitorInfo>(StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (GetDisplayConfigBufferSizes(flags, out var pathCount, out var modeCount) != 0)
                return [];

            var paths = new DisplayConfigPathInfo[pathCount];
            var modes = new DisplayConfigModeInfo[modeCount];

            var error = QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (error == ErrorInsufficientBuffer) continue;
            if (error != 0) return [];

            for (var i = 0; i < pathCount; i++)
            {
                var path = paths[i];
                var isActive = (path.Flags & DisplayConfigPathActive) != 0;

                var target = QueryTargetName(path.TargetInfo);
                if (target is null || string.IsNullOrEmpty(target.Value.DevicePath))
                    continue;

                var gdiName = QuerySourceName(path.SourceInfo) ?? string.Empty;
                var edidId = BuildEdidId(
                    target.Value.EdidManufactureId,
                    target.Value.EdidProductCodeId,
                    target.Value.DevicePath);

                // The same physical monitor appears once per possible source/target combination
                // with QDC_ALL_PATHS. Keep a single entry and prefer the active path.
                if (result.TryGetValue(target.Value.DevicePath, out var existing))
                {
                    if (!existing.IsActive && isActive)
                    {
                        result[target.Value.DevicePath] = existing with
                        {
                            GdiDeviceName = gdiName,
                            EdidId = edidId,
                            IsActive = true
                        };
                    }

                    continue;
                }

                result[target.Value.DevicePath] = new MonitorInfo(
                    gdiName,
                    target.Value.DevicePath,
                    target.Value.FriendlyName,
                    edidId,
                    isActive);
            }

            return result.Values
                .OrderBy(m => m.GdiDeviceName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return [];
    }

    private static string? QuerySourceName(DisplayConfigPathSourceInfo info)
    {
        var packet = new DisplayConfigSourceDeviceName
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = GetSourceNameType,
                Size = (uint)Marshal.SizeOf<DisplayConfigSourceDeviceName>(),
                AdapterId = info.AdapterId,
                Id = info.Id
            }
        };

        return GetSourceName(ref packet) == 0 ? packet.ViewGdiDeviceName : null;
    }

    private static (string DevicePath, string FriendlyName, ushort EdidManufactureId, ushort EdidProductCodeId)?
        QueryTargetName(DisplayConfigPathTargetInfo info)
    {
        var packet = new DisplayConfigTargetDeviceName
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = GetTargetNameType,
                Size = (uint)Marshal.SizeOf<DisplayConfigTargetDeviceName>(),
                AdapterId = info.AdapterId,
                Id = info.Id
            }
        };

        if (GetTargetName(ref packet) != 0) return null;

        return (packet.MonitorDevicePath, packet.MonitorFriendlyDeviceName,
            packet.EdidManufactureId, packet.EdidProductCodeId);
    }

    private static string BuildEdidId(ushort manufactureId, ushort productCodeId, string devicePath)
    {
        if (manufactureId == 0 && productCodeId == 0) return string.Empty;

        var serial = TryGetEdidSerial(devicePath);

        return string.IsNullOrEmpty(serial)
            ? $"{manufactureId:X4}:{productCodeId:X4}"
            : $"{manufactureId:X4}:{productCodeId:X4}:{serial}";
    }

    /// <summary>
    /// Reads the EDID blob from the monitor device registry key and extracts its serial number.
    /// This is used to recognize the same physical monitor after its device path changed.
    /// </summary>
    private static string? TryGetEdidSerial(string monitorDevicePath)
    {
        try
        {
            // \\?\DISPLAY#DEL41A1#5&2b3c...&0&UID4352#{e6f07b5f-...}
            var parts = monitorDevicePath.TrimStart('\\', '?', '\\').Split('#');
            if (parts.Length < 3 || !parts[0].Equals("DISPLAY", StringComparison.OrdinalIgnoreCase))
                return null;

            var keyPath = $@"SYSTEM\CurrentControlSet\Enum\DISPLAY\{parts[1]}\{parts[2]}\Device Parameters";
            using var key = Registry.LocalMachine.OpenSubKey(keyPath);
            if (key?.GetValue("EDID") is not byte[] edid || edid.Length < 128)
                return null;

            // Four 18-byte descriptors start at offset 54. Tag 0xFF is the serial number string.
            for (var i = 54; i + 18 <= 126; i += 18)
            {
                if (edid[i] == 0 && edid[i + 1] == 0 && edid[i + 3] == 0xFF)
                {
                    var text = Encoding.ASCII.GetString(edid, i + 5, 13).TrimEnd('\0', ' ');
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }

            // Fallback to the numeric serial number stored in the EDID base block.
            var serial = edid[12] | (edid[13] << 8) | (edid[14] << 16) | (edid[15] << 24);
            return serial == 0 ? null : serial.ToString("X8");
        }
        catch
        {
            return null;
        }
    }
}
