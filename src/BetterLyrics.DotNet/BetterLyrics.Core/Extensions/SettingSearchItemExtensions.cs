using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;

namespace BetterLyrics.Core.Extensions;

public static class SettingSearchItemExtensions
{
    public static List<SettingSearchItem> AllItems { get; } =
    [
        // App apperance and behavior settings

        // App apperance and behavior settings - App apperance settings

        new SettingSearchItem { Uid = "AppSettingsControlTheme/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },
        new SettingSearchItem { Uid = "SettingsPageLanguage/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },
        new SettingSearchItem { Uid = "SettingsPageGlobalFont/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },
        new SettingSearchItem { Uid = "AppSettingsControlEnhanceControlAnimations/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },

        new SettingSearchItem { Uid = "SettingsPageShowNowPlayingNotification/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance },
        new SettingSearchItem { Uid = "SettingsPageNowPlayingNotificationTheme/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance, ParentUid = "SettingsPageShowNowPlayingNotification/Header" },
        new SettingSearchItem { Uid = "SettingsPageNowPlayingNotificationCorner/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance, ParentUid = "SettingsPageShowNowPlayingNotification/Header" },
        new SettingSearchItem { Uid = "SettingsPageNowPlayingNotificationAllMonitors/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance, ParentUid = "SettingsPageShowNowPlayingNotification/Header" },
        new SettingSearchItem { Uid = "SettingsPageNowPlayingNotificationDuration/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Appearance, ParentUid = "SettingsPageShowNowPlayingNotification/Header" },

        // App apperance and behavior settings - Window settings

        new SettingSearchItem { Uid = "SettingsPageAutoStart/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageAutoOpenSplashWindow/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageAutoOpenMusicGalleryWindow/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageAutoOpenLyricsWindow/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageAutoPlayWhenOpenMusicGalleryWindow/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageStopTrackOnGalleryWindowClosed/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageExitOnGalleryWindowClosed/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageExitOnLyricsWindowClosed/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Window },

        // App apperance and behavior settings - Shortcut settings

        new SettingSearchItem { Uid = "SettingsPageShowHideHotKey/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },
        new SettingSearchItem { Uid = "SettingsPageLyricsWindowSwitchHotKey/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },
        new SettingSearchItem { Uid = "SettingsPagePlayOrPauseSongHotKey/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },
        new SettingSearchItem { Uid = "SettingsPageNextSongHotKey/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },
        new SettingSearchItem { Uid = "SettingsPagePreviousSongHotKey/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Shortcut },

        // App apperance and behavior settings - System tray settings

        new SettingSearchItem { Uid = "AppSettingsControlSystemTrayColorfulIcon/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.SystemTray },
        new SettingSearchItem { Uid = "AppSettingsControlSystemTrayClickCallback/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.SystemTray },
        new SettingSearchItem { Uid = "AppSettingsControlSystemTrayDoubleClickCallback/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.SystemTray },
        new SettingSearchItem { Uid = "AppSettingsControlSystemTrayMiddleClickCallback/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.SystemTray },

        // App apperance and behavior settings - Advanced settings

        new SettingSearchItem { Uid = "SettingsPageWriteDebugLogs/Header", Section = SettingsSection.App, Subsection = AppSettingsSection.Advanced },

        // Media library settings

        new SettingSearchItem { Uid = "SettingsPageMusicLib/Header", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MediaSettingsControlNameSetting/Header", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "SettingsPageLocalMusicFilePattern/Header", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "SettingsPageLocalLyricsFilePattern/Header", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MediaSettingsControlLastSyncTime/Header", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MusicSettingsControlAutoSyncInterval/Header", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MusicSettingsControlRealTimeScan/Header", Section = SettingsSection.MediaLib, Subsection = null },
        new SettingSearchItem { Uid = "MusicSettingsControlScanSubDirectories/Header", Section = SettingsSection.MediaLib, Subsection = null },

        // Playback source and lyrics processing settings

        // Playback source and lyrics processing settings - Playback source settings

        new SettingSearchItem { Uid = "SettingsPageListenNewSession/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },

        new SettingSearchItem { Uid = "SettingsPageMediaSourceProvidersConfig/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "PlaybackSettingsControlMemoryReader/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageMediaSourceProvidersConfig/Header" },

        new SettingSearchItem { Uid = "SettingsPageLastFMTrack/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "SettingsPageDiscordPresence/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "SettingsPageLXMusicServer/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },

        new SettingSearchItem { Uid = "SettingsPageLyricsTimeline/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "SettingsPageLyricsTimelineThreshold/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsTimeline/Header" },

        new SettingSearchItem { Uid = "MainPagePositionOffsetSlider/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General },
        new SettingSearchItem { Uid = "LyricsPagePositionOffsetHint/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "MainPagePositionOffsetSlider/Header" },

        new SettingSearchItem { Uid = "AlbumArtSearchProvidersTargetSize/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageAlbumArtSearchProvidersConfig/Text" },

        new SettingSearchItem { Uid = "SettingsPageLyricsSearchType/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsSearchProvidersConfig/Text" },
        new SettingSearchItem { Uid = "LyricsSearchControlIgnoreCache/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsSearchProvidersConfig/Text" },
        new SettingSearchItem { Uid = "SettingsPageOverwriteMatchingThreshold/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsSearchProvidersConfig/Text" },
        new SettingSearchItem { Uid = "SettingsPageMatchingThreshold/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.General, ParentUid = "SettingsPageLyricsSearchProvidersConfig/Text" },
        
        // Playback source and lyrics processing settings - Lyrics processing settings

        new SettingSearchItem { Uid = "LyricsPageTranslationEnabled/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "SettingsPageTargetLanguage/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "LyricsPageTranslationEnabled/Header" },
        new SettingSearchItem { Uid = "SettingsPageTranslationConfig/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "LyricsPageTranslationEnabled/Header" },
        new SettingSearchItem { Uid = "SettingsPageLibreTranslateServer/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "LyricsPageTranslationEnabled/Header" },

        new SettingSearchItem { Uid = "PlaybackSettingsControlNoLyricsFoundHandling/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "PlaybackSettingsControlCustomNotFoundMessageCard/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "PlaybackSettingsControlNoLyricsFoundHandling/Header" },

        new SettingSearchItem { Uid = "SettingsPagePhonetics/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "SettingsPagePinyin/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "SettingsPagePhonetics/Header" },
        new SettingSearchItem { Uid = "SettingsPageJyutping/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "SettingsPagePhonetics/Header" },
        new SettingSearchItem { Uid = "SettingsPageJapanese/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "SettingsPagePhonetics/Header" },
        new SettingSearchItem { Uid = "SettingsPageKorean/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing, ParentUid = "SettingsPagePhonetics/Header" },

        new SettingSearchItem { Uid = "SettingsPageChinesePreference/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "SettingsPageLyricsFilter/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },
        new SettingSearchItem { Uid = "SettingsPageLyricsAutoRetryCount/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.LyricsProcessing },

        // Playback source and lyrics processing settings - Integration settings
        
        new SettingSearchItem { Uid = "SettingsPageLastFMUsername/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },
        new SettingSearchItem { Uid = "SettingsPageLastFMPlaycount/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },
        new SettingSearchItem { Uid = "SettingsPageLastFMRegistered/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },

        new SettingSearchItem { Uid = "SettingsPageDiscordUsername/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },
        new SettingSearchItem { Uid = "SettingsPageDiscordAlbumArtSource/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },

        new SettingSearchItem { Uid = "SettingsPageAmllTtmlDbBaseUrl/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.Integration },

        // Playback source and lyrics processing settings - Realtime status settings

        new SettingSearchItem { Uid = "LyricsSaveConfigInSyllablesFormat/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigIncludeTranslation/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigIncludeTransliteration/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigInOneLine/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigFileNamePattern/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },
        new SettingSearchItem { Uid = "LyricsSaveConfigFolder/Header", Section = SettingsSection.PlaybackLib, Subsection = PlaybackLibSettingsSection.RealtimeStatus },

        // Lyrics window manager settings

        // Lyrics window manager settings - Window settings

        new SettingSearchItem { Uid = "SettingsPageConfigName/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "SettingsPageWorkArea/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageWorkAreaHeight/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageWorkArea/Header" },
        new SettingSearchItem { Uid = "SettingsPageDockPlacement/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageWorkArea/Header" },
        new SettingSearchItem { Uid = "SettingsPageDockMonitor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageWorkArea/Header" },

        new SettingSearchItem { Uid = "SettingsPageTaskbarPlacement/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "SettingsPageAOT/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageForceAlwaysOnTop/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageAOT/Header" },

        new SettingSearchItem { Uid = "SettingsPageKeepScreenOpen/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageShowInSwitchers/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageDragArea/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeathering/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeatheringLeft/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlEdgeFeathering/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeatheringTop/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlEdgeFeathering/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeatheringRight/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlEdgeFeathering/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeatheringBottom/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlEdgeFeathering/Header" },

        new SettingSearchItem { Uid = "WindowSettingsControlSpoutOutput/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageHideWindow/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageHideWindowWhenNullSession/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageHideWindowDelay/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlWallpaperEngineBinding/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlTheme/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "SettingsPageAdaptEnvColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageEnvColorSample/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPageAdaptEnvColor/Header" },

        new SettingSearchItem { Uid = "SettingsPageAdaptAlbumArtAccentColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "SettingsPagePaletteGeneratorType/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPagePaletteChromaWeight/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType/Header" },
        new SettingSearchItem { Uid = "SettingsPagePaletteToneWeight/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType/Header" },
        new SettingSearchItem { Uid = "SettingsPagePalettePopulationWeight/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType/Header" },
        new SettingSearchItem { Uid = "SettingsPagePaletteDarkToneThreshold/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType/Header" },
        new SettingSearchItem { Uid = "SettingsPagePaletteLightToneThreshold/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "SettingsPagePaletteGeneratorType/Header" },

        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePalette/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteAccentColor1/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteAccentColor2/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteAccentColor3/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteAccentColor4/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlRealTimePaletteUnderlayColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlRealTimePalette/Header" },

        new SettingSearchItem { Uid = "SettingsPageAlwaysHideUnlockButton/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlRemoveBorder/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageKeepNowPlayingBarInteractive/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageKeepNowPlayingBarLyricsPreview/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarAutoAdaptive/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarShowTimeArea/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlNowPlayingBarAutoAdaptive/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarShowProgressBar/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlNowPlayingBarAutoAdaptive/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarShowMoreButton/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlNowPlayingBarAutoAdaptive/Header" },
        new SettingSearchItem { Uid = "WindowSettingsControlAlwaysHideNowPlayingBar/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window, ParentUid = "WindowSettingsControlNowPlayingBarAutoAdaptive/Header" },

        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarResident/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "WindowSettingsControlNowPlayingBarBackgroundStyle/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageFPS/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },
        new SettingSearchItem { Uid = "SettingsPageDebugOverlay/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.Window },

        // Lyrics window manager settings - Lyrics style settings

        new SettingSearchItem { Uid = "SettingsPageLyricsAlignment/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageUseInternalLyricsAlignment/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsAlignment/Header" },

        new SettingSearchItem { Uid = "SettingsPageLyricsContentOrientation/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageAutoWrap/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageLyricsCenterTopOffset/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },

        new SettingSearchItem { Uid = "SettingsPageLyricsLineSpacingFactor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageLyricsLineOverallSpacingFactor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsLineSpacingFactor/Header" },
        new SettingSearchItem { Uid = "SettingsPageLyricsLineInnerSpacingFactor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsLineSpacingFactor/Header" },

        new SettingSearchItem { Uid = "SettingsPageLyricsFontFamily/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageCJK/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontFamily/Header" },
        new SettingSearchItem { Uid = "SettingsPageWesternChar/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontFamily/Header" },

        new SettingSearchItem { Uid = "SettingsPageLyricsFontWeight/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageLyricsFontStrokeWidth/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },

        new SettingSearchItem { Uid = "SettingsPageFontColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPagePlayedStrokeFontColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor/Header" },
        new SettingSearchItem { Uid = "SettingsPageUnplayedStrokeFontColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor/Header" },
        new SettingSearchItem { Uid = "SettingsPageLyricsBgFontColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor/Header" },
        new SettingSearchItem { Uid = "SettingsPageLyricsPlayedFgFontColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor/Header" },
        new SettingSearchItem { Uid = "SettingsPageLyricsUnplayedFgFontColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageFontColor/Header" },

        new SettingSearchItem { Uid = "SettingsPageLyricsFontSize/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontSize/Header" },
        new SettingSearchItem { Uid = "SettingsPagePhoneticText/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontSize/Header" },
        new SettingSearchItem { Uid = "SettingsPageOriginalText/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontSize/Header" },
        new SettingSearchItem { Uid = "SettingsPageTranslatedText/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsFontSize/Header" },

        new SettingSearchItem { Uid = "SettingsPageLyricsOpacity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },
        new SettingSearchItem { Uid = "SettingsPagePhoneticText/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsOpacity/Header" },
        new SettingSearchItem { Uid = "SettingsPagePlayedOriginalText/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsOpacity/Header" },
        new SettingSearchItem { Uid = "SettingsPageUnplayedOriginalText/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsOpacity/Header" },
        new SettingSearchItem { Uid = "SettingsPageTranslatedText/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle, ParentUid = "SettingsPageLyricsOpacity/Header" },

        new SettingSearchItem { Uid = "LyricsSharePageStyleTitle/Text", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsStyle },

        // Lyrics window manager settings - Lyrics background settings

        new SettingSearchItem { Uid = "SettingsPagePureLayer/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPagePureLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageOpacity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPagePureLayer/Header" },

        new SettingSearchItem { Uid = "SettingsPageDesktopWallpaperLayer/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageOpacity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageDesktopWallpaperLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageBlurAmount/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageDesktopWallpaperLayer/Header" },

        new SettingSearchItem { Uid = "SettingsPageAlbumArtLayer/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageOpacity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpeed/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageBlurAmount/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageAlbumArtLayer/Header" },

        new SettingSearchItem { Uid = "SettingsPageFluidLayer/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageOpacity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpeed/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageLightWaveEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageFluidOverlayStatic/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFluidLayer/Header" },

        new SettingSearchItem { Uid = "SettingsPageSnowFlakeLayer/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageAmount/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpeed/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSnowFlakeLayer/Header" },

        new SettingSearchItem { Uid = "SettingsPageFogLayer/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFogLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFogLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageFogLayer/Header" },

        new SettingSearchItem { Uid = "SettingsPageRaindropLayer/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageSpeed/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSize/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageDensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageLightAngle/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageShadowIntensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageRaindropLayer/Header" },

        new SettingSearchItem { Uid = "SettingsPageSpectrumLayer/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },
        new SettingSearchItem { Uid = "SettingsPageColor/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageOpacity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumLayerPlacement/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumLayerStyle/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageAmount/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumSensitivity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumDelay/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageSpectrumGlowEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },
        new SettingSearchItem { Uid = "SettingsPageParallaxTilt/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground, ParentUid = "SettingsPageSpectrumLayer/Header" },

        new SettingSearchItem { Uid = "SettingsPageSemanticEffects/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsBackground },

        // Lyrics window manager settings - Lyrics effect settings

        new SettingSearchItem { Uid = "SettingsPageLyricsWordByWordEffectMode/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLyricsBlurEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLyricsFadeOutEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "WindowSettingsControlEdgeFeathering/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLyricsOutOfSightEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },

        new SettingSearchItem { Uid = "SettingsPageLyricsGapDots/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLyricsGapDotsMinimumDuration/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGapDots/Header" },

        new SettingSearchItem { Uid = "SettingsPageSpectrumBrethingEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageSpectrumBreathingIntensity/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageSpectrumBrethingEffect/Header" },

        new SettingSearchItem { Uid = "SettingsPageLyricsGlowEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageScope/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGlowEffect/Header" },
        new SettingSearchItem { Uid = "SettingsPageLongSyllableDuration/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGlowEffect/Header" },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGlowEffect/Header" },
        new SettingSearchItem { Uid = "SettingsPageAmount/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsGlowEffect/Header" },

        new SettingSearchItem { Uid = "SettingsPageLyricsScaleEffect/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageLongSyllableDuration/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsScaleEffect/Header" },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsScaleEffect/Header" },
        new SettingSearchItem { Uid = "SettingsPageAmount/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsScaleEffect/Header" },

        new SettingSearchItem { Uid = "SettingsPageLyricsFloatAnimation/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsFloatAnimation/Header" },
        new SettingSearchItem { Uid = "SettingsPageAmount/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsFloatAnimation/Header" },
        new SettingSearchItem { Uid = "LyricsEffectSettingsControlAnimationDuration/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageLyricsFloatAnimation/Header" },

        new SettingSearchItem { Uid = "SettingsPageFan/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },

        new SettingSearchItem { Uid = "SettingsPageParallaxTilt/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageAutoAdjust/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageParallaxTilt/Header" },
        new SettingSearchItem { Uid = "SettingsPage3DLyricsDepth/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageParallaxTilt/Header" },

        new SettingSearchItem { Uid = "SettingsPageScrollEasing/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect },
        new SettingSearchItem { Uid = "SettingsPageEasingMode/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing/Header" },
        new SettingSearchItem { Uid = "SettingsPageScrollTopDuration/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing/Header" },
        new SettingSearchItem { Uid = "SettingsPageScrollDuration/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing/Header" },
        new SettingSearchItem { Uid = "SettingsPageScrollBottomDuration/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing/Header" },
        new SettingSearchItem { Uid = "SettingsPageScrollTopDelay/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing/Header" },
        new SettingSearchItem { Uid = "SettingsPageScrollBottomDelay/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.LyricsEffect, ParentUid = "SettingsPageScrollEasing/Header" },

        // Lyrics window manager settings - Album art settings

        new SettingSearchItem { Uid = "SettingsPageAlbumRadius/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },
        new SettingSearchItem { Uid = "SettingsPageAlbumShadowAmount/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },
        new SettingSearchItem { Uid = "SettingsPageImageSwitchType/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },

        new SettingSearchItem { Uid = "AlbumArtAreaSettingsControlFadeOut/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },
        new SettingSearchItem { Uid = "AlbumArtAreaSettingsControlStartPoint/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt, ParentUid = "AlbumArtAreaSettingsControlFadeOut/Header" },
        new SettingSearchItem { Uid = "AlbumArtAreaSettingsControlEndPoint/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt, ParentUid = "AlbumArtAreaSettingsControlFadeOut/Header" },

        new SettingSearchItem { Uid = "SettingsPageParallaxTilt/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },

        new SettingSearchItem { Uid = "AlbumArtAreaEffectSettingsControlSongInfoAutoScroll/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = LyricsWindowManagerSettingsSection.AlbumArt },

        new SettingSearchItem { Uid = "SettingsPageMonitorLayout/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = null },
        new SettingSearchItem { Uid = "SettingsPageOpenDisplaySettings/Header", Section = SettingsSection.LyricsWindowMgr, Subsection = null, ParentUid = "SettingsPageMonitorLayout/Header" },
    ];

    public readonly static SettingSearchItem LoadingPlaceholder = new() { Uid = "SEARCH_LOADING" };

    public readonly static SettingSearchItem NoResultsPlaceholder = new() { Uid = "SEARCH_NO_RESULTS" };
}
