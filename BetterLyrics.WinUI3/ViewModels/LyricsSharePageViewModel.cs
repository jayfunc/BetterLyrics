using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsSharePageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<BitmapImage?>>,
        IRecipient<PropertyChangedMessage<MappedSongSearchQuery?>>
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISongSearchMapService _songSearchMapService;
        private readonly ISettingsService _settingsService;

        public IGSMTCService GSMTCService { get; private set; }

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }
        [ObservableProperty] public partial LyricsCardData CardData { get; set; } = new();
        [ObservableProperty] public partial LyricsCardConfig CardConfig { get; set; } = new();
        [ObservableProperty] public partial ObservableCollection<LyricsCardStyleGroup> StyleGroups { get; set; }
        [ObservableProperty] public partial LyricsCardStyleItem SelectedStyleItem { get; set; }
        [ObservableProperty] public partial int SelectedStyleDisplayTypeIndex { get; set; } = 1;

        public LyricsSharePageViewModel(IGSMTCService gsmtcService, ISongSearchMapService songSearchMapService, ISettingsService settingsService, ILocalizationService localizationService)
        {
            _songSearchMapService = songSearchMapService;
            _settingsService = settingsService;
            _localizationService = localizationService;

            AppSettings = settingsService.AppSettings; 
            GSMTCService = gsmtcService;

            _ = RefreshCardDataAsync();
            ActivateCardDataForBinding();
            LoadStyleData();
        }

        private void LoadStyleData()
        {
            // 经典设计
            var classicGroup = new LyricsCardStyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupClassic"), new[]
            {
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleMinimal"), StyleKey = "LyricsCardMinimalStyle", IsChecked = true },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleSwiss"), StyleKey = "LyricsCardSwissStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleMagazine"), StyleKey = "LyricsCardMagazineStyle" }
            });

            // 实体质感
            var physicalGroup = new LyricsCardStyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupPhysical"), new[]
            {
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleVinyl"), StyleKey = "LyricsCardVinylStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCD"), StyleKey = "LyricsCardCDStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStylePolaroid"), StyleKey = "LyricsCardPolaroidStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleTicket"), StyleKey = "LyricsCardTicketStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleReceipt"), StyleKey = "LyricsCardReceiptStyle" }
            });

            // 时光印记
            var tracesOfTimeGroup = new LyricsCardStyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupTracesOfTime"), new[]
            {
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleJournal"), StyleKey = "LyricsCardJournalStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleStickyNote"), StyleKey = "LyricsCardStickyNoteStyle" }
            });

            // 数码怀旧
            var retroGroup = new LyricsCardStyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupRetro"), new[]
            {
                new LyricsCardStyleItem { DisplayText = "iPod", StyleKey = "LyricsCardPodStyle" },
                new LyricsCardStyleItem { DisplayText = "Windows Phone", StyleKey = "LyricsCardWindowsPhoneStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleRetroQQ"), StyleKey = "LyricsCardRetroQQStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleRetroMSN"), StyleKey = "LyricsCardRetroMSNStyle" }
            });

            // 现代视窗
            var modernGroup = new LyricsCardStyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupModernVision"), new[]
            {
                new LyricsCardStyleItem { DisplayText = "QQ", StyleKey = "LyricsCardQQStyle" },
                new LyricsCardStyleItem { DisplayText = "微信", StyleKey = "LyricsCardWeChatStyle" },
                new LyricsCardStyleItem { DisplayText = "WhatsApp", StyleKey = "LyricsCardWhatsAppStyle" },
                new LyricsCardStyleItem { DisplayText = "Telegram", StyleKey = "LyricsCardTelegramStyle" },
                new LyricsCardStyleItem { DisplayText = "LINE", StyleKey = "LyricsCardLINEStyle" },
            });

            // 氛围创意
            var atmosphereGroup = new LyricsCardStyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupAtmosphere"), new[]
            {
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCinematic"), StyleKey = "LyricsCardCinematicStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCyberpunk"), StyleKey = "LyricsCardCyberpunkStyle" }
            });

            // 国风雅韵
            var chineseEleganceGroup = new LyricsCardStyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupChineseElegance"), new[]
            {
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleInkWash"), StyleKey = "LyricsCardInkWashStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleAncientBook"), StyleKey = "LyricsCardAncientBookStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleBambooSlips"), StyleKey = "LyricsCardBambooSlipsStyle" },
                new LyricsCardStyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleDunhuang"), StyleKey = "LyricsCardDunhuangStyle" }
            });

            StyleGroups = new ObservableCollection<LyricsCardStyleGroup>
            {
                classicGroup,
                physicalGroup,
                tracesOfTimeGroup,
                retroGroup,
                modernGroup,
                atmosphereGroup,
                chineseEleganceGroup,
            };

            SwitchStyle(classicGroup[0]);
        }

        public void SwitchStyle(LyricsCardStyleItem styleItem)
        {
            foreach (var styleGroup in StyleGroups)
            {
                foreach (var item in styleGroup)
                {
                    item.IsChecked = false;
                }
            }
            SelectedStyleItem = styleItem;
        }

        public void UpdateSelectedLyrics(List<LyricsLine> lyrics)
        {
            CardData.Lyrics = lyrics;
        }

        private async Task RefreshCardDataAsync()
        {
            var (mappedTitle, mappedArtist, _) = await _songSearchMapService.GetMappingAsync(GSMTCService.CurrentSongInfo);
            CardData.Title = mappedTitle;
            CardData.Artist = mappedArtist;
            CardData.CoverImage = GSMTCService.AlbumArtBitmapImage;
            CardData.AccentCoverColor = (await GSMTCService.GetAlbumArtAccentColorsAsync(Enums.PaletteGeneratorType.Auto, true)).First();

            CardData.Lyrics = [];
        }

        private void ActivateCardDataForBinding()
        {
            _ = CardData.Title;
            _ = CardData.Artist;
            _ = CardData.CoverImage;
            _ = CardData.AccentCoverColor;
            _ = CardData.Lyrics;

            _ = CardConfig;
            _ = CardConfig.FontFamily;
        }

        partial void OnSelectedStyleItemChanged(LyricsCardStyleItem value)
        {
            SwitchStyle(value);

            var found = _settingsService.AppSettings.LyricsCardConfigs.FirstOrDefault(c => c.ResourceKey == value.StyleKey);
            if (found == null)
            {
                found = LyricsCardConfigExtensions.GetDefaultLyricsCardConfig(value.StyleKey);
                _settingsService.AppSettings.LyricsCardConfigs.Add(found);
            }
            CardConfig = found;
        }

        partial void OnSelectedStyleDisplayTypeIndexChanged(int value)
        {
            foreach (var group in StyleGroups)
            {
                foreach (var style in group)
                {
                    style.IsExpanded = value != 0;
                }
            }
        }

        public void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.AlbumArtBitmapImage))
                {
                    _ = RefreshCardDataAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<MappedSongSearchQuery?> message)
        {
            if (message.Sender is LyricsSearchControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSearchControlViewModel.MappedSongSearchQuery))
                {
                    _ = RefreshCardDataAsync();
                }
            }
        }
    }
}
