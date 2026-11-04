using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Interfaces.Services;

/// <summary>
/// Migrates and normalizes the monitor bindings of the lyrics windows so that they are stored
/// against a stable monitor identity (device path / EDID) instead of the GDI display name.
/// </summary>
public interface IMonitorBindingService
{
    /// <summary>
    /// Normalizes every lyrics window status. Safe to call on topology changes; monitors that are
    /// currently unavailable keep their previous binding so that they can be restored later.
    /// </summary>
    void NormalizeAll();

    /// <summary>
    /// Normalizes a single lyrics window status. Returns <c>true</c> when the binding could be
    /// resolved against the current topology.
    /// </summary>
    bool Normalize(LyricsWindowStatus status);
}
