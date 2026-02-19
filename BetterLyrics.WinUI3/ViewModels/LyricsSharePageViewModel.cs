using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsSharePageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<BitmapImage?>>,
        IRecipient<PropertyChangedMessage<MappedSongSearchQuery?>>
    {
        private readonly ISongSearchMapService _songSearchMapService;
        public IGSMTCService GSMTCService { get; private set; }

        [ObservableProperty] public partial LyricsCardData CardData { get; set; } = new();

        public LyricsSharePageViewModel(IGSMTCService gsmtcService, ISongSearchMapService songSearchMapService)
        {
            _songSearchMapService = songSearchMapService;
            GSMTCService = gsmtcService;

            RefreshCardDataAsync();
            ActivateCardDataForBinding();
        }

        public void UpdateSelectedLyrics(List<LyricsLine> lyrics)
        {
#if DEBUG && false
            return;
#endif
            CardData.SelectedLyrics = lyrics;
        }

        private Brush GetOverlayBrush()
        {
            var dominantColor = GSMTCService.GetAlbumArtAccentColors(Enums.PaletteGeneratorType.Auto, true).First();

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
#if DEBUG && false
            CardData = new LyricsShareCardData
            {
                Title = "This is a very long song title for testing the text wrapping functionality in the lyrics share card. Let's see how it handles this!",
                Artist = "An artist with an equally long name to test the layout of the share card when both title and artist are unusually verbose. This should be interesting!",
                CoverImage = GSMTCService.AlbumArtBitmapImage,
                OverlayBrush = OverlayBrush,
                SelectedLyrics = new List<LyricsLine>
                {
                    // 1. 标准短句测试
                    new LyricsLine
                    {
                        StartMs = 1500,
                        PrimaryText = "最後のキスはタバコの flavor がした",
                        SecondaryText = "最后的一个吻 带有香烟的味道"
                    },
                    // 2. 长句测试（测试自动换行是否正常）
                    new LyricsLine
                    {
                        StartMs = 8000,
                        PrimaryText = "ニガくてせつない香り 明日の今頃には あなたはどこにいるんだろう",
                        SecondaryText = "苦涩而难过的香味，明天的这个时候，你会在哪里呢"
                    },
                    // 3. 英文与特殊字符测试
                    new LyricsLine
                    {
                        StartMs = 18000,
                        PrimaryText = "You are always gonna be my love",
                        SecondaryText = "你永远是我的挚爱"
                    },
                    // 4. 【重点】空翻译测试（SecondaryText 为空，测试 Converter 是否隐藏了 Margin）
                    new LyricsLine
                    {
                        StartMs = 24000,
                        PrimaryText = "いつか誰かとまた恋に落ちても",
                        SecondaryText = ""
                    },
                    // 5. 【重点】更长的原文测试（多行换行）
                    new LyricsLine
                    {
                        StartMs = 30000,
                        PrimaryText = "I'll remember to love you taught me how. You are always gonna be the one.",
                        SecondaryText = "我会记得去爱，是你教会了我。你永远是那唯一。"
                    },
                    // 6. 只有原文，无翻译的情况2
                    new LyricsLine
                    {
                        StartMs = 38000,
                        PrimaryText = "今はまだ悲しい love song",
                        SecondaryText = ""
                    },
                    // 7. 再次出现标准双语
                    new LyricsLine
                    {
                        StartMs = 42000,
                        PrimaryText = "新しい歌 うたえるまで",
                        SecondaryText = "直到能唱出新的歌"
                    },
                    // 8. 填充数据以增加列表长度，测试滚动
                    new LyricsLine
                    {
                        StartMs = 48000,
                        PrimaryText = "立ち止まる時間が 動き出そうとしている",
                        SecondaryText = "停滞的时间 正准备开始转动"
                    },
                    new LyricsLine
                    {
                        StartMs = 54000,
                        PrimaryText = "忘れたくないこと",
                        SecondaryText = "不想忘记的事情"
                    },
                    new LyricsLine
                    {
                        StartMs = 58000,
                        PrimaryText = "Oh, tell me why, tell me why",
                        SecondaryText = "告诉我为什么"
                    }
                }
            };
#else
            var (mappedTitle, mappedArtist, _) = await _songSearchMapService.GetMappingAsync(GSMTCService.CurrentSongInfo);
            CardData = new LyricsCardData
            {
                Title = mappedTitle,
                Artist = mappedArtist,
                CoverImage = GSMTCService.AlbumArtBitmapImage,
                OverlayBrush = GetOverlayBrush(),
                SelectedLyrics = CardData.SelectedLyrics
            };
#endif
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
        }

        public void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.AlbumArtBitmapImage))
                {
                    RefreshCardDataAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<MappedSongSearchQuery?> message)
        {
            if (message.Sender is LyricsSearchControlViewModel)
            {
                if (message.PropertyName == nameof(LyricsSearchControlViewModel.MappedSongSearchQuery))
                {
                    RefreshCardDataAsync();
                }
            }
        }
    }
}
