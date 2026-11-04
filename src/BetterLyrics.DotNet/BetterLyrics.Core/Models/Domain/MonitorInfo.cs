namespace BetterLyrics.Core.Models.Domain;

/// <summary>
/// Describes a monitor as seen by the Windows Display Configuration (CCD) API.
/// </summary>
/// <param name="GdiDeviceName">
/// The GDI source name, for example <c>\\.\DISPLAY1</c>. This name is only valid for the current
/// display topology and must not be persisted as the primary key.
/// </param>
/// <param name="DevicePath">
/// The monitor device interface path, for example
/// <c>\\?\DISPLAY#DEL41A1#5&amp;...&amp;UID4352#{e6f07b5f-...}</c>. This is stable across reboots and is
/// used as the persisted monitor identity.
/// </param>
/// <param name="FriendlyName">The monitor friendly name reported by EDID/DisplayConfig.</param>
/// <param name="EdidId">
/// An EDID based identity (manufacturer, product and serial number when available), used as a
/// fallback when the device path changes (for example after moving the monitor to another port).
/// </param>
/// <param name="IsActive">Whether the monitor is part of the current desktop topology.</param>
public sealed record MonitorInfo(
    string GdiDeviceName,
    string DevicePath,
    string FriendlyName,
    string EdidId,
    bool IsActive)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(FriendlyName)
            ? (string.IsNullOrWhiteSpace(GdiDeviceName) ? DevicePath : GdiDeviceName)
            : $"{FriendlyName} ({GdiDeviceName})";
}
