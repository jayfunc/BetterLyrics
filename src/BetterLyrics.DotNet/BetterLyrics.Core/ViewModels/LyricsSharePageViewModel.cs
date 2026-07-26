using System.Collections.ObjectModel;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.ViewModels;

public partial class LyricsSharePageViewModel : BaseViewModel,
    IRecipient<PropertyChangedMessage<byte[]?>>,
    IRecipient<PropertyChangedMessage<MappedSongSearchQuery?>>
{
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private readonly ISongSearchMapService _songSearchMapService;

    public LyricsSharePageViewModel(IGsmtcService gsmtcService, ISongSearchMapService songSearchMapService,
        ISettingsService settingsService, ILocalizationService localizationService)
    {
        _songSearchMapService = songSearchMapService;
        _settingsService = settingsService;
        _localizationService = localizationService;

        AppSettings = settingsService.AppSettings;
        GSMTCService = gsmtcService;

        SelectedStyleDisplayTypeIndex = AppSettings.LyricsCardSettings.SelectedDisplayTypeIndex;

        _ = RefreshCardDataAsync();
        ActivateCardDataForBinding();
        LoadStyleData();
    }

    public IGsmtcService GSMTCService { get; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }
    [ObservableProperty] public partial LyricsCardData CardData { get; set; } = new();
    [ObservableProperty] public partial LyricsCardConfig CardConfig { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<LyricsCardStyleGroup> StyleGroups { get; set; }
    [ObservableProperty] public partial LyricsCardStyleItem SelectedStyleItem { get; set; }
    [ObservableProperty] public partial int SelectedStyleDisplayTypeIndex { get; set; } = 1;

    public void Receive(PropertyChangedMessage<byte[]?> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.AlbumArtBytes))
                _ = RefreshCardDataAsync();
    }

    public void Receive(PropertyChangedMessage<MappedSongSearchQuery?> message)
    {
        if (message.Sender is LyricsSearchControlViewModel)
            if (message.PropertyName == nameof(LyricsSearchControlViewModel.MappedSongSearchQuery))
                _ = RefreshCardDataAsync();
    }

    private void LoadStyleData()
    {
        // 经典设计
        var classicGroup = new LyricsCardStyleGroup(
            _localizationService.GetLocalizedString("LyricsSharePageGroupClassic"), new[]
            {
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleMinimal"),
                    StyleKey = "LyricsCardMinimalStyle", IsChecked = true
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleSwiss"),
                    StyleKey = "LyricsCardSwissStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleMagazine"),
                    StyleKey = "LyricsCardMagazineStyle"
                }
            });

        // 实体质感
        var physicalGroup = new LyricsCardStyleGroup(
            _localizationService.GetLocalizedString("LyricsSharePageGroupPhysical"), new[]
            {
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleVinyl"),
                    StyleKey = "LyricsCardVinylStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCD"),
                    StyleKey = "LyricsCardCDStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStylePolaroid"),
                    StyleKey = "LyricsCardPolaroidStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleTicket"),
                    StyleKey = "LyricsCardTicketStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleReceipt"),
                    StyleKey = "LyricsCardReceiptStyle"
                }
            });

        // 时光印记
        var tracesOfTimeGroup = new LyricsCardStyleGroup(
            _localizationService.GetLocalizedString("LyricsSharePageGroupTracesOfTime"), new[]
            {
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleJournal"),
                    StyleKey = "LyricsCardJournalStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleStickyNote"),
                    StyleKey = "LyricsCardStickyNoteStyle"
                }
            });

        // 数码怀旧
        var retroGroup = new LyricsCardStyleGroup(
            _localizationService.GetLocalizedString("LyricsSharePageGroupRetro"), new[]
            {
                new LyricsCardStyleItem { DisplayText = "iPod", StyleKey = "LyricsCardPodStyle" },
                new LyricsCardStyleItem { DisplayText = "Windows Phone", StyleKey = "LyricsCardWindowsPhoneStyle" },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleRetroQQ"),
                    StyleKey = "LyricsCardRetroQQStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleRetroMSN"),
                    StyleKey = "LyricsCardRetroMSNStyle"
                }
            });

        // 现代视窗
        var modernGroup = new LyricsCardStyleGroup(
            _localizationService.GetLocalizedString("LyricsSharePageGroupModernVision"), new[]
            {
                new LyricsCardStyleItem { DisplayText = "QQ", StyleKey = "LyricsCardQQStyle" },
                new LyricsCardStyleItem { DisplayText = "微信", StyleKey = "LyricsCardWeChatStyle" },
                new LyricsCardStyleItem { DisplayText = "WhatsApp", StyleKey = "LyricsCardWhatsAppStyle" },
                new LyricsCardStyleItem { DisplayText = "Telegram", StyleKey = "LyricsCardTelegramStyle" },
                new LyricsCardStyleItem { DisplayText = "LINE", StyleKey = "LyricsCardLINEStyle" }
            });

        // 氛围创意
        var atmosphereGroup = new LyricsCardStyleGroup(
            _localizationService.GetLocalizedString("LyricsSharePageGroupAtmosphere"), new[]
            {
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCinematic"),
                    StyleKey = "LyricsCardCinematicStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCyberpunk"),
                    StyleKey = "LyricsCardCyberpunkStyle"
                }
            });

        // 国风雅韵
        var chineseEleganceGroup = new LyricsCardStyleGroup(
            _localizationService.GetLocalizedString("LyricsSharePageGroupChineseElegance"), new[]
            {
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleInkWash"),
                    StyleKey = "LyricsCardInkWashStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleAncientBook"),
                    StyleKey = "LyricsCardAncientBookStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleBambooSlips"),
                    StyleKey = "LyricsCardBambooSlipsStyle"
                },
                new LyricsCardStyleItem
                {
                    DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleDunhuang"),
                    StyleKey = "LyricsCardDunhuangStyle"
                }
            });

        StyleGroups = new ObservableCollection<LyricsCardStyleGroup>
        {
            classicGroup,
            physicalGroup,
            tracesOfTimeGroup,
            retroGroup,
            modernGroup,
            atmosphereGroup,
            chineseEleganceGroup
        };

        var savedStyleKey = AppSettings.LyricsCardSettings.SelectedStyleKey;
        var styleToSelect = StyleGroups.SelectMany(g => g).FirstOrDefault(s => s.StyleKey == savedStyleKey) ?? classicGroup[0];
        SwitchStyle(styleToSelect);

        var displayType = AppSettings.LyricsCardSettings.SelectedDisplayTypeIndex;
        foreach (var group in StyleGroups)
        foreach (var style in group)
            style.IsExpanded = displayType != 0;
    }

    public void SwitchStyle(LyricsCardStyleItem styleItem)
    {
        foreach (var styleGroup in StyleGroups)
        foreach (var item in styleGroup)
            item.IsChecked = false;

        SelectedStyleItem = styleItem;
    }

    public void UpdateSelectedLyrics(List<LyricsLine> lyrics)
    {
        CardData.Lyrics = lyrics;
    }

    private async Task RefreshCardDataAsync()
    {
        var (mappedTitle, mappedArtist, _) =
            await _songSearchMapService.GetMappingAsync(GSMTCService.CurrentSongInfo);
        CardData.Title = mappedTitle;
        CardData.Artist = mappedArtist;
        CardData.CoverImageBytes = GSMTCService.AlbumArtBytes;
        CardData.AccentCoverColor =
            (await GSMTCService.GetAlbumArtAccentColorsAsync(PaletteGeneratorType.CelebiQuantizer, true)).First();

        CardData.Lyrics = [];
    }

    private void ActivateCardDataForBinding()
    {
        _ = CardData.Title;
        _ = CardData.Artist;
        _ = CardData.CoverImageBytes;
        _ = CardData.AccentCoverColor;
        _ = CardData.Lyrics;

        _ = CardConfig;
        _ = CardConfig.FontFamily;
    }

    partial void OnSelectedStyleItemChanged(LyricsCardStyleItem value)
    {
        SwitchStyle(value);

        AppSettings.LyricsCardSettings.SelectedStyleKey = value.StyleKey;

        var found = _settingsService.AppSettings.LyricsCardConfigs.FirstOrDefault(c =>
            c.ResourceKey == value.StyleKey);
        if (found == null)
        {
            found = LyricsCardConfigExtensions.GetDefaultLyricsCardConfig(value.StyleKey);
            _settingsService.AppSettings.LyricsCardConfigs.Add(found);
        }

        CardConfig = found;
    }

    partial void OnSelectedStyleDisplayTypeIndexChanged(int value)
    {
        AppSettings.LyricsCardSettings.SelectedDisplayTypeIndex = value;

        if (StyleGroups == null) return;
        foreach (var group in StyleGroups)
        foreach (var style in group)
            style.IsExpanded = value != 0;
    }
}