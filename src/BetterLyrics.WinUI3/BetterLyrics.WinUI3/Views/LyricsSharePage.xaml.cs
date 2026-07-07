using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Helpers;
using BetterLyrics.WinUI3.Providers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using TagLib;
using File = System.IO.File;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class LyricsSharePage : Page
{
    private readonly IGlobalToastProvider _globalToastProvider =
        Ioc.Default.GetRequiredService<IGlobalToastProvider>();

    private readonly IFilePickerProvider _filePickerProvider =
        Ioc.Default.GetRequiredService<IFilePickerProvider>();

    public LyricsSharePage()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<LyricsSharePageViewModel>();
    }

    public LyricsSharePageViewModel ViewModel { get; set; }

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
            using (var memoryStream =
                   await RenderToStreamAsync(PreviewCard, ImageQualitySlider.Value / 100.0 * 4.0))
            {
                var (_, filePath) = await _filePickerProvider.PickSaveFileAsync(
                    new Dictionary<string, IList<string>> { { "PNG Image", new List<string> { ".png" } } },
                    $"BetterLyrics_{ViewModel.SelectedStyleItem.StyleKey}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"
                    , WindowType.LyricsShareWindow);

                if (filePath != null)
                {
                    await using (var sourceStream = memoryStream.AsStream())
                    {
                        sourceStream.Position = 0;
                        await using (var fileStream = File.OpenWrite(filePath))
                        {
                            await sourceStream.CopyToAsync(fileStream);
                        }
                    }

                    _globalToastProvider.Show("ActionCompleted", filePath, MessageSeverity.Success);
                }
            }
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
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

            _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
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
            await Task.Delay(Time.AnimationDuration);

            PreviewCardContainer.Stretch = Stretch.UniformToFill;
            LeftColDef.Width = RightColDef.Width = new GridLength(0, GridUnitType.Pixel);
            PreviewCard.UpdateLayout();
        }
        else
        {
            LeftColDef.Width = RightColDef.Width = new GridLength(1, GridUnitType.Star);
            PreviewCardContainer.Stretch = Stretch.Uniform;

            ProcessingOverlay.Opacity = 0;
            await Task.Delay(Time.AnimationDuration);
            ProcessingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private async Task<InMemoryRandomAccessStream> RenderToStreamAsync(UIElement element, double scaleFactor = 4.0)
    {
        var width = (int)(element.XamlRoot.Size.Width * scaleFactor);
        var renderWidth = (int)(((FrameworkElement)element).ActualWidth * scaleFactor);
        var renderHeight = (int)(((FrameworkElement)element).ActualHeight * scaleFactor);

        var renderTargetBitmap = new RenderTargetBitmap();
        await renderTargetBitmap.RenderAsync(element, renderWidth, renderHeight);

        var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        var stream = new InMemoryRandomAccessStream();

        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

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
            LyricsListView.SelectAll();
        else if (LyricsHostCheckBox.IsChecked == false) LyricsListView.SelectedItems.Clear();
    }
}