using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.LyricsWindow;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.WinUI3.Services;

/// <summary>
/// Connects Wallpaper Engine binding changes to the central lyrics window
/// visibility service. The actual show/hide decision is owned by
/// <see cref="LyricsWindowVisibilityService"/>.
/// </summary>
public sealed class WallpaperEngineLyricsWindowService : IWallpaperEngineLyricsWindowService,
    IRecipient<LyricsWindowWallpaperEngineBindingChangedMessage>
{
    private readonly ISettingsService _settingsService;
    private readonly ILyricsWindowVisibilityService _lyricsWindowVisibilityService;
    private readonly object _syncRoot = new();

    private bool _initialized;

    public WallpaperEngineLyricsWindowService(
        ISettingsService settingsService,
        ILyricsWindowVisibilityService lyricsWindowVisibilityService)
    {
        _settingsService = settingsService;
        _lyricsWindowVisibilityService = lyricsWindowVisibilityService;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Initialize()
    {
        lock (_syncRoot)
        {
            if (_initialized) return;
            _initialized = true;
        }

        _lyricsWindowVisibilityService.Initialize();
    }

    public void Receive(LyricsWindowWallpaperEngineBindingChangedMessage message)
    {
        lock (_syncRoot)
        {
            if (!_initialized) return;
        }

        // Clones used for copy/import/share also raise this message, but only
        // statuses that belong to the window manager should affect windows.
        if (!_settingsService.AppSettings.WindowBoundsRecords.Contains(message.Sender)) return;

        _lyricsWindowVisibilityService.NotifyBindingChanged(message.Sender);
    }
}
