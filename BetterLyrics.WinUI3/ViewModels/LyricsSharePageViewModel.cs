using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
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

        [ObservableProperty] public partial string SelectedStyleResourceKey { get; set; } = "LyricsSharePageStyleMinimal";
        [ObservableProperty] public partial string ConfigNavViewSelectedItemTag { get; set; } = "Style";
        [ObservableProperty] public partial LyricsCardData CardData { get; set; } = new();
        [ObservableProperty] public partial DataTemplate? CardDataTemplate { get; set; }
        [ObservableProperty] public partial ObservableCollection<StyleGroup> StyleGroups { get; set; }
        [ObservableProperty] public partial StyleItem SelectedStyleItem { get; set; }

        public LyricsSharePageViewModel(IGSMTCService gsmtcService, ISongSearchMapService songSearchMapService, ISettingsService settingsService, ILocalizationService localizationService)
        {
            _songSearchMapService = songSearchMapService;
            _settingsService = settingsService;
            _localizationService = localizationService;

            GSMTCService = gsmtcService;

            _ = RefreshCardDataAsync();
            ActivateCardDataForBinding();
            LoadStyleData();
        }

        private void LoadStyleData()
        {
            // 经典设计
            var classicGroup = new StyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupClassic"), new[]
            {
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleMinimal"), StyleKey = "LyricsCardMinimalStyle", IsChecked = true },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleSwiss"), StyleKey = "LyricsCardSwissStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleMagazine"), StyleKey = "LyricsCardMagazineStyle" }
            });

            // 实体质感
            var physicalGroup = new StyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupPhysical"), new[]
            {
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleVinyl"), StyleKey = "LyricsCardVinylStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCD"), StyleKey = "LyricsCardCDStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStylePolaroid"), StyleKey = "LyricsCardPolaroidStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleTicket"), StyleKey = "LyricsCardTicketStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleReceipt"), StyleKey = "LyricsCardReceiptStyle" }
            });

            // 时光印记
            var tracesOfTimeGroup = new StyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupTracesOfTime"), new[]
            {
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleJournal"), StyleKey = "LyricsCardJournalStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleStickyNote"), StyleKey = "LyricsCardStickyNoteStyle" }
            });

            // 数码怀旧
            var retroGroup = new StyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupRetro"), new[]
            {
                new StyleItem { DisplayText = "iPod", StyleKey = "LyricsCardPodStyle" },
                new StyleItem { DisplayText = "Windows Phone", StyleKey = "LyricsCardWindowsPhoneStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleRetroQQ"), StyleKey = "LyricsCardRetroQQStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleRetroMSN"), StyleKey = "LyricsCardRetroMSNStyle" }
            });

            // 现代视窗
            var modernGroup = new StyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupModernVision"), new[]
            {
                new StyleItem { DisplayText = "QQ", StyleKey = "LyricsCardQQStyle" },
                new StyleItem { DisplayText = "微信", StyleKey = "LyricsCardWeChatStyle" },
                new StyleItem { DisplayText = "WhatsApp", StyleKey = "LyricsCardWhatsAppStyle" },
                new StyleItem { DisplayText = "Telegram", StyleKey = "LyricsCardTelegramStyle" },
                new StyleItem { DisplayText = "LINE", StyleKey = "LyricsCardLINEStyle" },
            });

            // 氛围创意
            var atmosphereGroup = new StyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupAtmosphere"), new[]
            {
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCinematic"), StyleKey = "LyricsCardCinematicStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleCyberpunk"), StyleKey = "LyricsCardCyberpunkStyle" }
            });

            // 国风雅韵
            var chineseEleganceGroup = new StyleGroup(_localizationService.GetLocalizedString("LyricsSharePageGroupChineseElegance"), new[]
            {
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleInkWash"), StyleKey = "LyricsCardInkWashStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleAncientBook"), StyleKey = "LyricsCardAncientBookStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleBambooSlips"), StyleKey = "LyricsCardBambooSlipsStyle" },
                new StyleItem { DisplayText = _localizationService.GetLocalizedString("LyricsSharePageStyleDunhuang"), StyleKey = "LyricsCardDunhuangStyle" }
            });

            StyleGroups = new ObservableCollection<StyleGroup>
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

        public void SwitchStyle(StyleItem styleItem)
        {
            foreach (var styleGroup in StyleGroups)
            {
                foreach (var style in styleGroup)
                {
                    style.IsChecked = false;
                }
            }
            SelectedStyleItem = styleItem;
            var styleKey = styleItem.StyleKey;
            if (App.Current.Resources.TryGetValue(styleKey, out object template))
            {
                SelectedStyleResourceKey = styleKey;
                CardDataTemplate = (DataTemplate)template;
            }
        }

        public void UpdateSelectedLyrics(List<LyricsLine> lyrics)
        {
            CardData.SelectedLyrics = lyrics;
        }

        private async Task<Brush> GetOverlayBrushAsync()
        {
            var dominantColor = (await GSMTCService.GetAlbumArtAccentColorsAsync(Enums.PaletteGeneratorType.Auto, true)).First();

            LinearGradientBrush gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1)
            };

            gradientBrush.GradientStops.Add(new GradientStop
            {
                Color = Color.FromArgb(180, dominantColor.R, dominantColor.G, dominantColor.B),
                Offset = 0.0
            });

            gradientBrush.GradientStops.Add(new GradientStop
            {
                Color = Color.FromArgb(220, (byte)(dominantColor.R / 2), (byte)(dominantColor.G / 2), (byte)(dominantColor.B / 2)),
                Offset = 0.6
            });

            gradientBrush.GradientStops.Add(new GradientStop
            {
                Color = Colors.Black,
                Offset = 1.0
            });

            return gradientBrush;
        }

        private async Task RefreshCardDataAsync()
        {
            var (mappedTitle, mappedArtist, _) = await _songSearchMapService.GetMappingAsync(GSMTCService.CurrentSongInfo);
            CardData.Title = mappedTitle;
            CardData.Artist = mappedArtist;
            CardData.CoverImage = GSMTCService.AlbumArtBitmapImage;
            CardData.OverlayBrush = await GetOverlayBrushAsync();
            CardData.SelectedLyrics = [];
        }

        private void ActivateCardDataForBinding()
        {
            _ = CardData.Title;
            _ = CardData.Artist;
            _ = CardData.CoverImage;
            _ = CardData.OverlayBrush;
            _ = CardData.SelectedLyrics;

            _ = CardData.DateLong;
            _ = CardData.DateShort;

            _ = CardData.TimeShort;
            _ = CardData.TimeWithSeconds;
            _ = CardData.TimeWithSecondsReply;

            _ = CardData.Config;
            _ = CardData.Config.FontFamily;
        }

        partial void OnSelectedStyleResourceKeyChanged(string value)
        {
            var found = _settingsService.AppSettings.LyricsCardConfigs.FirstOrDefault(c => c.ResourceKey == value);
            if (found == null)
            {
                found = LyricsCardConfigExtensions.GetDefaultLyricsCardConfig(value);
                _settingsService.AppSettings.LyricsCardConfigs.Add(found);
            }
            CardData.Config = found;
        }

        partial void OnSelectedStyleItemChanged(StyleItem value)
        {
            SwitchStyle(value);
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

    public partial class StyleItem : ObservableObject
    {
        public string DisplayText { get; set; }
        public string StyleKey { get; set; }
        public DataTemplate CardDataTemplate => (DataTemplate)App.Current.Resources[StyleKey];
        public LyricsCardData CardData => LyricsCardDataExtensions.DemoLyricsCardData;
        [ObservableProperty] public partial bool IsChecked { get; set; } = false;
    }

    public class StyleGroup : ObservableCollection<StyleItem>
    {
        public string GroupTitle { get; set; }

        public StyleGroup(string title, IEnumerable<StyleItem> items) : base(items)
        {
            GroupTitle = title;
        }
    }

}
