using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Lyrics;
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
using Windows.ApplicationModel.DataTransfer;
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

        private void LyricsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var totalCount = LyricsListView.Items.Count;
            var selectedCount = LyricsListView.SelectedItems.Count;

            if (selectedCount == 0) LyricsHostCheckBox.IsChecked = false;
            else if (selectedCount == totalCount) LyricsHostCheckBox.IsChecked = true;
            else LyricsHostCheckBox.IsChecked = null;

            ViewModel.UpdateSelectedLyrics(LyricsListView.SelectedItems.Cast<LyricsLine>().ToList());
        }

        private async void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            await SetPreviewModeAsync(true);

            try
            {
                using (var memoryStream = await RenderToStreamAsync(PreviewCard, ImageQualitySlider.Value / 100.0 * 4.0))
                {
                    StorageFile? file = await PickerHelper.PickSaveFileAsync<LyricsShareWindow>(
                        new Dictionary<string, IList<string>> { { "PNG Image", new List<string> { ".png" } } },
                        $"BetterLyrics_{ViewModel.SelectedStyleItem.StyleKey}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"
                    );

                    if (file != null)
                    {
                        using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            await RandomAccessStream.CopyAndCloseAsync(memoryStream.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
                        }

                        GlobalToastManager.Show("ActionCompleted", file.Path, InfoBarSeverity.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalToastManager.Show("Error", ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                await SetPreviewModeAsync(false);
            }
        }

        private async void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            await SetPreviewModeAsync(true);

            try
            {
                var memoryStream = await RenderToStreamAsync(PreviewCard, ImageQualitySlider.Value / 100.0 * 4.0);

                var dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;

                var streamRef = RandomAccessStreamReference.CreateFromStream(memoryStream);
                dataPackage.SetBitmap(streamRef);

                Clipboard.SetContent(dataPackage);

                GlobalToastManager.Show("ActionCompleted", null, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                GlobalToastManager.Show("Error", ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                await SetPreviewModeAsync(false);
            }
        }

        private async Task SetPreviewModeAsync(bool isPreviewing)
        {
            if (isPreviewing)
            {
                ProcessingOverlay.Visibility = Visibility.Visible;
                ProcessingOverlay.Opacity = 1;
                await Task.Delay(Constants.Time.AnimationDuration);

                PreviewCardContainer.Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill;
                LeftColDef.Width = RightColDef.Width = new GridLength(0, GridUnitType.Pixel);
                PreviewCard.UpdateLayout();
            }
            else
            {
                LeftColDef.Width = RightColDef.Width = new GridLength(1, GridUnitType.Star);
                PreviewCardContainer.Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform;

                ProcessingOverlay.Opacity = 0;
                await Task.Delay(Constants.Time.AnimationDuration);
                ProcessingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<InMemoryRandomAccessStream> RenderToStreamAsync(UIElement element, double scaleFactor = 4.0)
        {
            int width = (int)(element.XamlRoot.Size.Width * scaleFactor);
            int renderWidth = (int)(((FrameworkElement)element).ActualWidth * scaleFactor);
            int renderHeight = (int)(((FrameworkElement)element).ActualHeight * scaleFactor);

            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(element, renderWidth, renderHeight);

            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            var stream = new InMemoryRandomAccessStream();

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

            stream.Seek(0);
            return stream;
        }

        private void LyricsHostCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (LyricsHostCheckBox.IsChecked == true)
            {
                LyricsListView.SelectAll();
            }
            else if (LyricsHostCheckBox.IsChecked == false)
            {
                LyricsListView.SelectedItems.Clear();
            }
        }

        private void ConfigNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            ViewModel.ConfigNavViewSelectedItemTag = $"{((NavigationViewItem)sender.SelectedItem).Tag}";
        }

    }
}