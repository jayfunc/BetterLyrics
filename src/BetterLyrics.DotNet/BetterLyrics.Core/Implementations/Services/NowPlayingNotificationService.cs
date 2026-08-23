using System.ComponentModel;
using System.Threading.Tasks;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Implementations.Services;

public class NowPlayingNotificationService : INowPlayingNotificationService
{
    private readonly IGsmtcService _gsmtcService;
    private readonly INowPlayingToastProvider _nowPlayingToastProvider;
    private readonly ISettingsService _settingsService;
    private readonly ISongSearchMapService _songSearchMapService;
    private readonly Debouncer _debouncer = new();

    private SongInfo _lastNotifiedSong = SongInfoExtensions.Placeholder;

    public NowPlayingNotificationService(
        IGsmtcService gsmtcService,
        INowPlayingToastProvider nowPlayingToastProvider,
        ISettingsService settingsService,
        ISongSearchMapService songSearchMapService)
    {
        _gsmtcService = gsmtcService;
        _nowPlayingToastProvider = nowPlayingToastProvider;
        _settingsService = settingsService;
        _songSearchMapService = songSearchMapService;
    }

    public void Initialize()
    {
        _gsmtcService.PropertyChanged += GsmtcService_PropertyChanged;
    }

    private void GsmtcService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IGsmtcService.CurrentSongInfo) ||
            e.PropertyName == nameof(IGsmtcService.AlbumArtBytes))
        {
            if (_settingsService.AppSettings.GeneralSettings.ShowNowPlayingNotification)
            {
                _ = _debouncer.RunAsync(async token =>
                {
                    await Task.Delay(200, token); // Small delay to let album art load

                    if (token.IsCancellationRequested) return;

                    var currentSong = _gsmtcService.CurrentSongInfo;

                    if (currentSong == SongInfoExtensions.Placeholder)
                        return;

                    // Do not show again if it's the same song
                    if (_lastNotifiedSong.Title == currentSong.Title && _lastNotifiedSong.Artist == currentSong.Artist)
                    {
                        return;
                    }

                    _lastNotifiedSong = currentSong;
                    var albumArt = _gsmtcService.AlbumArtBytes;

                    var mappedInfo = await _songSearchMapService.GetMappingAsync(currentSong, token);
                    var displaySong = (SongInfo)currentSong.Clone();
                    displaySong.Title = mappedInfo.Title;
                    displaySong.Artist = mappedInfo.Artist;
                    displaySong.Album = mappedInfo.Album;

                    await _nowPlayingToastProvider.ShowAsync(displaySong, albumArt);
                });
            }
        }
    }
}
