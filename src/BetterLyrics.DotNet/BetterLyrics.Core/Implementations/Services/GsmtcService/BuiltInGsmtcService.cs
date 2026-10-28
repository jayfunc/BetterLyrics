using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services.GsmtcService;

/// <summary>
/// Singleton Service
/// </summary>
public sealed class BuiltInGsmtcService : GsmtcServiceBase, IBuiltInGsmtcService
{
    public BuiltInGsmtcService(
    ISettingsService settingsService,
    IAlbumArtSearchService albumArtSearchService,
    ILyricsSearchService lyricsSearchService,
    ITranslationService translationService,
    ITransliterationService transliterationService,
    IAppUIThreadProvider appUiThreadProvider,
    ILogger<BuiltInGsmtcService> logger,
    IGlobalToastProvider globalToastProvider,
    IWindowManagerProvider windowManagerProvider)
        : base(settingsService, albumArtSearchService, lyricsSearchService, translationService,
            transliterationService, appUiThreadProvider, logger, globalToastProvider, windowManagerProvider)
    {
        Initialize();
    }

    protected override string? MessageToken => GsmtcMessageToken.BuiltIn;

    protected override IMediaSessionProvider? GetCurrentDesiredSession()
    {
        return _mediaManagerProvider.CurrentMediaSessions.FirstOrDefault(x => PlayerIdHelper.IsBetterLyrics(x.SessionId));
    }
}
