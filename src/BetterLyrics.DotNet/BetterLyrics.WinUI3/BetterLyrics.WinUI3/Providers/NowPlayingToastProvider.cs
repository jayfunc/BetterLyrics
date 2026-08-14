using System.Collections.Generic;
using System.Threading.Tasks;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;

namespace BetterLyrics.WinUI3.Providers;

public class NowPlayingToastProvider : INowPlayingToastProvider
{
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly IAppUIThreadProvider _appUIThreadProvider = Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

    private readonly List<NowPlayingToastWindow> _overlayWindows = [];
    private bool _isInitialized;

    public void Initialize()
    {
        if (_isInitialized) return;

        var displayAreas = DisplayArea.FindAll();

        for (var i = 0; i < displayAreas.Count; i++)
        {
            var display = displayAreas[i];
            if (display == null) continue;

            var window = new NowPlayingToastWindow();
            // Position gets updated in ShowAsync based on current settings
            _overlayWindows.Add(window);
        }

        _isInitialized = true;
    }

    public async Task ShowAsync(SongInfo song, byte[]? albumArtBytes)
    {
        if (!_isInitialized) Initialize();

        var showOnAll = _settingsService.AppSettings.GeneralSettings.NowPlayingNotificationAllMonitors;
        var corner = _settingsService.AppSettings.GeneralSettings.NowPlayingNotificationCorner;
        var primaryDisplay = DisplayArea.Primary;

        var displayAreas = DisplayArea.FindAll();
        
        // Ensure we have enough windows if display count changed
        if (displayAreas.Count > _overlayWindows.Count)
        {
            for (var i = _overlayWindows.Count; i < displayAreas.Count; i++)
            {
                _overlayWindows.Add(new NowPlayingToastWindow());
            }
        }

        for (var i = 0; i < displayAreas.Count; i++)
        {
            var display = displayAreas[i];
            var window = _overlayWindows[i];

            if (!showOnAll && display.DisplayId.Value != primaryDisplay.DisplayId.Value)
            {
                continue;
            }

            _appUIThreadProvider.Execute(() =>
            {
                window.Init(display, corner);
                _ = window.ShowAsync(song, albumArtBytes);
            });
        }
    }
}
