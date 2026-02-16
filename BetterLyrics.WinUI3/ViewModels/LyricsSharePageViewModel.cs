using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SettingsService;
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
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsSharePageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<BitmapImage?>>
    {
        private readonly ISettingsService _settingsService;

        public IGSMTCService GSMTCService { get; private set; }

        [ObservableProperty] public partial BitmapImage QRCode { get; set; }
        [ObservableProperty] public partial LyricsShareCardData CardData { get; set; } = new();
        [ObservableProperty] public partial Brush OverlayBrush { get; set; }

        public LyricsSharePageViewModel(IGSMTCService gsmtcService, ISettingsService settingsService)
        {
            GSMTCService = gsmtcService;
            _settingsService = settingsService;

            RefreshCardData();
        }

        public void UpdateSelectedLyrics(List<LyricsLine> lyrics)
        {
            CardData = new LyricsShareCardData
            {
                Title = GSMTCService.CurrentSongInfo.Title,
                Artist = GSMTCService.CurrentSongInfo.Artist,
                CoverImage = GSMTCService.AlbumArtBitmapImage,
                OverlayBrush = CardData.OverlayBrush,
                SelectedLyrics = lyrics
            };
        }

        private void UpdateOverlayBrush()
        {
            var dominantColor = GSMTCService.GetAlbumArtAccentColors(Enums.PaletteGeneratorType.Auto, true).First();

            // 创建一个垂直线性渐变
            // 顶部是提取出的主色调（半透明），底部渐变到黑色（不透明），确保底部文字清晰
            LinearGradientBrush gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1)
            };

            // 渐变起始色：主色调，降低一点透明度让背景图透出来
            gradientBrush.GradientStops.Add(new GradientStop
            {
                Color = Color.FromArgb(180, dominantColor.R, dominantColor.G, dominantColor.B),
                Offset = 0.0
            });

            // 渐变中间色：稍微变暗
            gradientBrush.GradientStops.Add(new GradientStop
            {
                Color = Color.FromArgb(220, (byte)(dominantColor.R / 2), (byte)(dominantColor.G / 2), (byte)(dominantColor.B / 2)),
                Offset = 0.6
            });

            // 渐变结束色：纯黑，确保底部文字绝对清晰
            gradientBrush.GradientStops.Add(new GradientStop
            {
                Color = Colors.Black,
                Offset = 1.0
            });

            // 应用到遮罩层
            OverlayBrush = gradientBrush;
        }

        private void RefreshCardData()
        {
            UpdateOverlayBrush();

            CardData = new LyricsShareCardData
            {
                Title = GSMTCService.CurrentSongInfo.Title,
                Artist = GSMTCService.CurrentSongInfo.Artist,
                CoverImage = GSMTCService.AlbumArtBitmapImage,
                OverlayBrush = OverlayBrush,
                SelectedLyrics = CardData.SelectedLyrics
            };
        }

        public void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.AlbumArtBitmapImage))
                {
                    RefreshCardData();
                }
            }
        }
    }
}
