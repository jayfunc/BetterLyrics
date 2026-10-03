using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Extensions;

public static class SettingSearchItemExtensions
{
    public static List<SettingSearchItem> AllItems { get; } =
    [
        // App apperance and behavior settings

        // App apperance and behavior settings - App apperance settings

        new SettingSearchItem { Uid = "AppSettingsControlTheme", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },
        new SettingSearchItem { Uid = "SettingsPageLanguage", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },
        new SettingSearchItem { Uid = "SettingsPageGlobalFont", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },
        new SettingSearchItem { Uid = "AppSettingsControlEnhanceControlAnimations", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },

        new SettingSearchItem { Uid = "SettingsPageShowNowPlayingNotification", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },
        new SettingSearchItem { Uid = "SettingsPageNowPlayingNotificationTheme", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance, ParentUid = "SettingsPageShowNowPlayingNotification" },
        new SettingSearchItem { Uid = "SettingsPageNowPlayingNotificationCorner", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance, ParentUid = "SettingsPageShowNowPlayingNotification" },
        new SettingSearchItem { Uid = "SettingsPageNowPlayingNotificationAllMonitors", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance, ParentUid = "SettingsPageShowNowPlayingNotification" },
        new SettingSearchItem { Uid = "SettingsPageNowPlayingNotificationDuration", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance, ParentUid = "SettingsPageShowNowPlayingNotification" },

        // App apperance and behavior settings - Window settings

        new SettingSearchItem { Uid = "SettingsPageAutoStart", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageAutoOpenSplashWindow", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageAutoOpenMusicGalleryWindow", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageAutoOpenLyricsWindow", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageAutoPlayWhenOpenMusicGalleryWindow", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageStopTrackOnGalleryWindowClosed", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageExitOnGalleryWindowClosed", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageExitOnLyricsWindowClosed", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },

        // App apperance and behavior settings - Shortcut settings

        new SettingSearchItem { Uid = "SettingsPageShowHideHotKey", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },
        new SettingSearchItem { Uid = "SettingsPageLyricsWindowSwitchHotKey", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },
        new SettingSearchItem { Uid = "SettingsPagePlayOrPauseSongHotKey", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },
        new SettingSearchItem { Uid = "SettingsPageNextSongHotKey", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },
        new SettingSearchItem { Uid = "SettingsPagePreviousSongHotKey", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },

        // App apperance and behavior settings - System tray settings

        new SettingSearchItem { Uid = "AppSettingsControlSystemTrayColorfulIcon", Section = SettingsSection.App, Subsection = AppSettingsSection.SystemTray },
        new SettingSearchItem { Uid = "AppSettingsControlSystemTrayClickCallback", Section = SettingsSection.App, Subsection = AppSettingsSection.SystemTray },
        new SettingSearchItem { Uid = "AppSettingsControlSystemTrayDoubleClickCallback", Section = SettingsSection.App, Subsection = AppSettingsSection.SystemTray },
        new SettingSearchItem { Uid = "AppSettingsControlSystemTrayMiddleClickCallback", Section = SettingsSection.App, Subsection = AppSettingsSection.SystemTray },

        // Media library settings

        new SettingSearchItem { Uid = "SettingsPageMusicLib", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MediaSettingsControlNameSetting", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "SettingsPageLocalMusicFilePattern", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "SettingsPageLocalLyricsFilePattern", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MediaSettingsControlLastSyncTime", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MusicSettingsControlAutoSyncInterval", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MusicSettingsControlRealTimeScan", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MusicSettingsControlScanSubDirectories", Section = SettingsSection.MediaLib, Subsection = null },

        // Playback source and lyrics processing settings

        // Playback source and lyrics processing settings - Playback source settings

        new SettingSearchItem { Uid = "SettingsPageListenNewSession", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },

        new SettingSearchItem { Uid = "SettingsPageMediaSourceProvidersConfig", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "PlaybackSettingsControlMemoryReader", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageMediaSourceProvidersConfig" },

        new SettingSearchItem { Uid = "SettingsPageLastFMTrack", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "SettingsPageDiscordPresence", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "SettingsPageLXMusicServer", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },

        new SettingSearchItem { Uid = "SettingsPageLyricsTimeline", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "SettingsPageLyricsTimelineThreshold", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsTimeline" },

        new SettingSearchItem { Uid = "MainPagePositionOffsetSlider", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "LyricsPagePositionOffsetHint", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "MainPagePositionOffsetSlider" },

        new SettingSearchItem { Uid = "AlbumArtSearchProvidersTargetSize", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageAlbumArtSearchProvidersConfig" },

        new SettingSearchItem { Uid = "SettingsPageLyricsSearchType", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsSearchProvidersConfig" },
        new SettingSearchItem { Uid = "LyricsSearchControlIgnoreCache", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsSearchProvidersConfig" },
        new SettingSearchItem { Uid = "SettingsPageOverwriteMatchingThreshold", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsSearchProvidersConfig" },
        new SettingSearchItem { Uid = "SettingsPageMatchingThreshold", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsSearchProvidersConfig" },
        
        // Playback source and lyrics processing settings - Lyrics processing settings

        new SettingSearchItem { Uid = "LyricsPageTranslationEnabled", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "SettingsPageTargetLanguage", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "LyricsPageTranslationEnabled" },
        new SettingSearchItem { Uid = "SettingsPageTranslationConfig", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "LyricsPageTranslationEnabled" },
        new SettingSearchItem { Uid = "SettingsPageLibreTranslateServer", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "LyricsPageTranslationEnabled" },
        
        new SettingSearchItem { Uid = "PlaybackSettingsControlNoLyricsFoundHandling", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "PlaybackSettingsControlCustomNotFoundMessageCard", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "PlaybackSettingsControlNoLyricsFoundHandling" },
        
        new SettingSearchItem { Uid = "SettingsPagePhonetics", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "SettingsPagePinyin", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "SettingsPagePhonetics" },
        new SettingSearchItem { Uid = "SettingsPageJyutping", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "SettingsPagePhonetics" },
        new SettingSearchItem { Uid = "SettingsPageJapanese", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "SettingsPagePhonetics" },
        new SettingSearchItem { Uid = "SettingsPageKorean", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "SettingsPagePhonetics" },
        
        new SettingSearchItem { Uid = "SettingsPageChinesePreference", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "SettingsPageLyricsFilter", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "SettingsPageLyricsAutoRetryCount", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },

        // Playback source and lyrics processing settings - Integration settings
        
        new SettingSearchItem { Uid = "SettingsPageLastFMUsername", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },
        new SettingSearchItem { Uid = "SettingsPageLastFMPlaycount", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },
        new SettingSearchItem { Uid = "SettingsPageLastFMRegistered", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },
        
        new SettingSearchItem { Uid = "SettingsPageDiscordUsername", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },
        new SettingSearchItem { Uid = "SettingsPageDiscordAlbumArtSource", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },
        
        new SettingSearchItem { Uid = "SettingsPageAmllTtmlDbBaseUrl", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },

        // Playback source and lyrics processing settings - Realtime status settings

        new SettingSearchItem { Uid = "LyricsSaveConfigInSyllablesFormat", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigIncludeTranslation", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigIncludeTransliteration", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigInOneLine", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigFileNamePattern", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigFolder", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },

        // Lyrics window manager settings

        // Lyrics window manager settings - Window settings

        new SettingSearchItem { Uid = "SettingsPageConfigName", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        
        new SettingSearchItem { Uid = "SettingsPageWorkArea", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageWorkAreaHeight", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageWorkArea" },
        new SettingSearchItem { Uid = "SettingsPageDockPlacement", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageWorkArea" },
        new SettingSearchItem { Uid = "SettingsPageDockMonitor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageWorkArea" },
        
        new SettingSearchItem { Uid = "SettingsPageTaskbarPlacement", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "SettingsPageAOT", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageForceAlwaysOnTop", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageAOT" },
        
        new SettingSearchItem { Uid = "SettingsPageKeepScreenOpen", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageShowInSwitchers", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageDragArea", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeathering", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeatheringLeft", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlEdgeFeathering" },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeatheringTop", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlEdgeFeathering" },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeatheringRight", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlEdgeFeathering" },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeatheringBottom", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlEdgeFeathering" },

        new SettingSearchItem { Uid = "WindowSettingsControlSpoutOutput", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageHideWindow", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageHideWindowWhenNullSession", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageHideWindowDelay", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlTheme", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "SettingsPageAdaptEnvColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageEnvColorSample", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageAdaptEnvColor" },

        new SettingSearchItem { Uid = "SettingsPageAdaptAlbumArtAccentColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "SettingsPagePaletteGeneratorType", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPagePaletteChromaWeight", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType" },
        new SettingSearchItem { Uid = "SettingsPagePaletteToneWeight", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType" },
        new SettingSearchItem { Uid = "SettingsPagePalettePopulationWeight", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType" },
        new SettingSearchItem { Uid = "SettingsPagePaletteDarkToneThreshold", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType" },
        new SettingSearchItem { Uid = "SettingsPagePaletteLightToneThreshold", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType" },

        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePalette", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteAccentColor1", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette" },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteAccentColor2", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette" },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteAccentColor3", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette" },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteAccentColor4", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette" },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteUnderlayColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette" },

        new SettingSearchItem { Uid = "SettingsPageAlwaysHideUnlockButton", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlRemoveBorder", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageKeepNowPlayingBarInteractive", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageKeepNowPlayingBarLyricsPreview", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarAutoAdaptive", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarShowTimeArea", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlNowPlayingBarAutoAdaptive" },
        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarShowProgressBar", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlNowPlayingBarAutoAdaptive" },
        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarShowMoreButton", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlNowPlayingBarAutoAdaptive" },
        new SettingSearchItem { Uid = "WindowSettingsControlAlwaysHideNowPlayingBar", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlNowPlayingBarAutoAdaptive" },

        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarResident", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarBackgroundStyle", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageFPS", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageDebugOverlay", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        // Lyrics window manager settings - Lyrics style settings

        new SettingSearchItem { Uid = "SettingsPageLyricsAlignment", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageUseInternalLyricsAlignment", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsAlignment" },

        new SettingSearchItem { Uid = "SettingsPageLyricsContentOrientation", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageAutoWrap", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageLyricsCenterTopOffset", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },

        new SettingSearchItem { Uid = "SettingsPageLyricsLineSpacingFactor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageLyricsLineOverallSpacingFactor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsLineSpacingFactor" },
        new SettingSearchItem { Uid = "SettingsPageLyricsLineInnerSpacingFactor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsLineSpacingFactor" },

        new SettingSearchItem { Uid = "SettingsPageLyricsFontFamily", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageCJK", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontFamily" },
        new SettingSearchItem { Uid = "SettingsPageWesternChar", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontFamily" },

        new SettingSearchItem { Uid = "SettingsPageLyricsFontWeight", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageLyricsFontStrokeWidth", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },

        new SettingSearchItem { Uid = "SettingsPageFontColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPagePlayedStrokeFontColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor" },
        new SettingSearchItem { Uid = "SettingsPageUnplayedStrokeFontColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor" },
        new SettingSearchItem { Uid = "SettingsPageLyricsBgFontColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor" },
        new SettingSearchItem { Uid = "SettingsPageLyricsPlayedFgFontColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor" },
        new SettingSearchItem { Uid = "SettingsPageLyricsUnplayedFgFontColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor" },

        new SettingSearchItem { Uid = "SettingsPageLyricsFontSize", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontSize" },
        new SettingSearchItem { Uid = "SettingsPagePhoneticText", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontSize" },
        new SettingSearchItem { Uid = "SettingsPageOriginalText", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontSize" },
        new SettingSearchItem { Uid = "SettingsPageTranslatedText", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontSize" },

        new SettingSearchItem { Uid = "SettingsPageLyricsOpacity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPagePhoneticText", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsOpacity" },
        new SettingSearchItem { Uid = "SettingsPagePlayedOriginalText", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsOpacity" },
        new SettingSearchItem { Uid = "SettingsPageUnplayedOriginalText", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsOpacity" },
        new SettingSearchItem { Uid = "SettingsPageTranslatedText", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsOpacity" },

        new SettingSearchItem { Uid = "LyricsSharePageStyleTitle", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },

        // Lyrics window manager settings - Lyrics background settings

        new SettingSearchItem { Uid = "SettingsPagePureLayer", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPagePureLayer" },
        new SettingSearchItem { Uid = "SettingsPageOpacity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPagePureLayer" },

        new SettingSearchItem { Uid = "SettingsPageAlbumArtLayer", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageOpacity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpeed", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer" },
        new SettingSearchItem { Uid = "SettingsPageBlurAmount", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer" },

        new SettingSearchItem { Uid = "SettingsPageFluidLayer", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageOpacity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpeed", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer" },
        new SettingSearchItem { Uid = "SettingsPageLightWaveEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer" },
        new SettingSearchItem { Uid = "SettingsPageFluidOverlayStatic", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer" },

        new SettingSearchItem { Uid = "SettingsPageSnowFlakeLayer", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageAmount", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpeed", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer" },

        new SettingSearchItem { Uid = "SettingsPageFogLayer", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFogLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFogLayer" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFogLayer" },

        new SettingSearchItem { Uid = "SettingsPageRaindropLayer", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageSpeed", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer" },
        new SettingSearchItem { Uid = "SettingsPageSize", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer" },
        new SettingSearchItem { Uid = "SettingsPageDensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer" },
        new SettingSearchItem { Uid = "SettingsPageLightAngle", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer" },
        new SettingSearchItem { Uid = "SettingsPageShadowIntensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer" },

        new SettingSearchItem { Uid = "SettingsPageSpectrumLayer", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageColor", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageOpacity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumLayerPlacement", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumLayerStyle", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageAmount", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumSensitivity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumDelay", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumGlowEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer" },

        new SettingSearchItem { Uid = "SettingsPageSemanticEffects", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },

        // Lyrics window manager settings - Lyrics effect settings

        new SettingSearchItem { Uid = "SettingsPageLyricsWordByWordEffectMode", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLyricsBlurEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLyricsFadeOutEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeathering", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLyricsOutOfSightEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },

        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageSpectrumLayer" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageSpectrumBrethingEffect" },

        new SettingSearchItem { Uid = "SettingsPageLyricsGlowEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageScope", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGlowEffect" },
        new SettingSearchItem { Uid = "SettingsPageLongSyllableDuration", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGlowEffect" },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGlowEffect" },
        new SettingSearchItem { Uid = "SettingsPageAmount", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGlowEffect" },

        new SettingSearchItem { Uid = "SettingsPageLyricsScaleEffect", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLongSyllableDuration", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsScaleEffect" },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsScaleEffect" },
        new SettingSearchItem { Uid = "SettingsPageAmount", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsScaleEffect" },

        new SettingSearchItem { Uid = "SettingsPageLyricsFloatAnimation", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsFloatAnimation" },
        new SettingSearchItem { Uid = "SettingsPageAmount", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsFloatAnimation" },
        new SettingSearchItem { Uid = "LyricsEffectSettingsControlAnimationDuration", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsFloatAnimation" },

        new SettingSearchItem { Uid = "SettingsPageFan", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },

        new SettingSearchItem { Uid = "SettingsPageParallaxTilt", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageParallaxTilt" },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageParallaxTilt" },
        new SettingSearchItem { Uid = "SettingsPage3DLyricsDepth", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageParallaxTilt" },

        new SettingSearchItem { Uid = "SettingsPageScrollEasing", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageEasingMode", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing" },
        new SettingSearchItem { Uid = "SettingsPageScrollTopDuration", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing" },
        new SettingSearchItem { Uid = "SettingsPageScrollDuration", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing" },
        new SettingSearchItem { Uid = "SettingsPageScrollBottomDuration", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing" },
        new SettingSearchItem { Uid = "SettingsPageScrollTopDelay", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing" },
        new SettingSearchItem { Uid = "SettingsPageScrollBottomDelay", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing" },

        // Lyrics window manager settings - Album art settings

        new SettingSearchItem { Uid = "SettingsPageAlbumRadius", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },
        new SettingSearchItem { Uid = "SettingsPageAlbumShadowAmount", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },
        new SettingSearchItem { Uid = "SettingsPageImageSwitchType", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },

        new SettingSearchItem { Uid = "AlbumArtAreaSettingsControlFadeOut", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },
        new SettingSearchItem { Uid = "AlbumArtAreaSettingsControlStartPoint", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt, ParentUid = "AlbumArtAreaSettingsControlFadeOut" },
        new SettingSearchItem { Uid = "AlbumArtAreaSettingsControlEndPoint", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt, ParentUid = "AlbumArtAreaSettingsControlFadeOut" },

        new SettingSearchItem { Uid = "SettingsPageParallaxTilt", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },

        new SettingSearchItem { Uid = "AlbumArtAreaEffectSettingsControlSongInfoAutoScroll", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },

        new SettingSearchItem { Uid = "SettingsPageMonitorLayout", Section = SettingsSection.LyricsWindowMgr, Subsection = null },
        new SettingSearchItem { Uid = "SettingsPageOpenDisplaySettings", Section = SettingsSection.LyricsWindowMgr, Subsection = null, ParentUid = "SettingsPageMonitorLayout" },
    ];

    public readonly static SettingSearchItem LoadingPlaceholder = new() { Uid = "SEARCH_LOADING" };

    public readonly static SettingSearchItem NoResultsPlaceholder = new() { Uid = "SEARCH_NO_RESULTS" };
}
