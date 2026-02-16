using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsSharePage : Page
    {
        public LyricsSharePageViewModel ViewModel { get; set; }

        public LyricsSharePage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<LyricsSharePageViewModel>();
        }

        // 处理样式切换
        private void StyleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string styleKey)
            {
                if (Resources.TryGetValue(styleKey, out object template))
                {
                    PreviewCard.ContentTemplate = template as DataTemplate;
                }
            }
        }

        // 处理歌词选择，将数据传回 ViewModel
        private void LyricsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 假设你的 LyricsLines 集合中的对象有一个 Text 属性
            // 如果 ViewModel.GSMTCService.CurrentLyricsData.LyricsLines 里的对象类型是 string，直接 Cast<string>
            // 如果是类（例如 LyricLine），则需要提取 Text

            var selectedLyrics = new List<string>();
            foreach (var item in LyricsListView.SelectedItems)
            {
                // 这里用反射或dynamic简化处理，或者你如果知道具体类型，强转一下
                // 假设是 dynamic 或者你有具体的 LyricModel 类
                if (item is string str) selectedLyrics.Add(str);
                else
                {
                    var prop = item.GetType().GetProperty("Text");
                    if (prop != null) selectedLyrics.Add(prop.GetValue(item)?.ToString() ?? "");
                }
            }

            ViewModel.UpdateSelectedLyrics(selectedLyrics);
        }

        private async void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            ProcessingOverlay.Visibility = Visibility.Visible;
            ProcessingOverlay.Opacity = 1;
            await Task.Delay(Constants.Time.AnimationDuration);

            PreviewCardContainer.Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill;
            LeftColDef.Width = RightColDef.Width = new GridLength(0, GridUnitType.Pixel);
            PreviewCard.UpdateLayout();

            double scaleFactor = 4.0;

            int width = (int)(PreviewCard.ActualWidth * scaleFactor);
            int height = (int)(PreviewCard.ActualHeight * scaleFactor);

            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();

            await renderTargetBitmap.RenderAsync(PreviewCard, width, height);

            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

            StorageFile? file = await PickerHelper.PickSaveFileAsync<LyricsShareWindow>(
                new Dictionary<string, IList<string>>
                {
            { "PNG Image", new List<string> { ".png" } }
                },
                $"BetterLyrics_Share_{DateTime.Now:MMddHHmm}.png"
            );

            if (file != null)
            {
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        (uint)renderTargetBitmap.PixelWidth,
                        (uint)renderTargetBitmap.PixelHeight,
                        96,
                        96,
                        pixelBuffer.ToArray());

                    await encoder.FlushAsync();
                }
            }

            LeftColDef.Width = RightColDef.Width = new GridLength(1, GridUnitType.Star);
            PreviewCardContainer.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;

            ProcessingOverlay.Opacity = 0;
            await Task.Delay(Constants.Time.AnimationDuration);
            ProcessingOverlay.Visibility = Visibility.Collapsed;
        }

        private void CopyImage_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}