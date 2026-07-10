using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using global::Avalonia.Input;
using System.Threading.Tasks;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Interfaces.Providers;
using System.Linq;
using System.Collections.Generic;

namespace BetterLyrics.Avalonia.Views;

public partial class LyricsShareWindow : Window
{
    public LyricsSharePageViewModel ViewModel { get; }

    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly IFilePickerProvider _filePickerProvider;

    public LyricsShareWindow()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<LyricsSharePageViewModel>();
        _globalToastProvider = Ioc.Default.GetRequiredService<IGlobalToastProvider>();
        _filePickerProvider = Ioc.Default.GetRequiredService<IFilePickerProvider>();
        DataContext = ViewModel;
    }

    private void LyricsHostCheckBox_Click(object? sender, RoutedEventArgs e)
    {
        if (LyricsHostCheckBox.IsChecked == true)
        {
            LyricsListView.SelectAll();
        }
        else
        {
            LyricsListView.SelectedItems.Clear();
        }
    }

    private void LyricsListView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var totalCount = LyricsListView.Items.Count;
        var selectedCount = LyricsListView.SelectedItems.Count;

        if (selectedCount == 0) LyricsHostCheckBox.IsChecked = false;
        else if (selectedCount == totalCount) LyricsHostCheckBox.IsChecked = true;
        else LyricsHostCheckBox.IsChecked = null;

        ViewModel.UpdateSelectedLyrics(LyricsListView.SelectedItems.Cast<LyricsLine>().ToList());
    }

    private void ZoomToggle_Click(object? sender, RoutedEventArgs e)
    {
        StylesSemanticZoom.ToggleView();
    }

    private async void SaveImage_Click(object? sender, RoutedEventArgs e)
    {
        await SetPreviewModeAsync(true);

        try
        {
            using var memoryStream = await RenderToStreamAsync(PreviewCard, ImageQualitySlider.Value / 100.0 * 4.0);
            
            var (_, filePath) = await _filePickerProvider.PickSaveFileAsync(
                new Dictionary<string, IList<string>> { { "PNG Image", new List<string> { ".png" } } },
                $"BetterLyrics_{ViewModel.SelectedStyleItem?.StyleKey ?? "Share"}_{System.DateTime.Now:yyyyMMdd_HHmmss_fff}.png",
                BetterLyrics.Core.Enums.WindowType.LyricsShareWindow);

            if (filePath != null)
            {
                memoryStream.Position = 0;
                using (var fileStream = System.IO.File.OpenWrite(filePath))
                {
                    await memoryStream.CopyToAsync(fileStream);
                }

                _globalToastProvider.Show("ActionCompleted", filePath, BetterLyrics.Core.Enums.MessageSeverity.Success);
            }
        }
        catch (System.Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, BetterLyrics.Core.Enums.MessageSeverity.Error);
        }
        finally
        {
            await SetPreviewModeAsync(false);
        }
    }

    private async void CopyImage_Click(object? sender, RoutedEventArgs e)
    {
        await SetPreviewModeAsync(true);

        try
        {
            // In Avalonia, Clipboard currently only reliably supports text cross-platform in some versions without extensions.
            // But we can try to copy bitmap if supported.
            // (Leaving this as a Toast for now if Clipboard bitmap is not fully implemented in Core)
            _globalToastProvider.Show("NotSupported", "Copying image to clipboard is not fully supported yet.", BetterLyrics.Core.Enums.MessageSeverity.Warning);
        }
        catch (System.Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, BetterLyrics.Core.Enums.MessageSeverity.Error);
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
            ProcessingOverlay.IsVisible = true;
            ProcessingOverlay.Opacity = 1;
            await Task.Delay(BetterLyrics.Core.Constants.Time.AnimationDuration);

            // Hide side panels
            MainGrid.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Pixel);
            MainGrid.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            
            // Allow layout to update
            await Task.Delay(50);
        }
        else
        {
            MainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            MainGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);

            ProcessingOverlay.Opacity = 0;
            await Task.Delay(BetterLyrics.Core.Constants.Time.AnimationDuration);
            ProcessingOverlay.IsVisible = false;
        }
    }
    
    private async Task<System.IO.MemoryStream> RenderToStreamAsync(Control element, double scaleFactor = 4.0)
    {
        // Wait for any layout changes
        await Task.Delay(100);
        
        var renderWidth = (int)(element.Bounds.Width * scaleFactor);
        var renderHeight = (int)(element.Bounds.Height * scaleFactor);

        if (renderWidth <= 0 || renderHeight <= 0)
        {
            renderWidth = 800;
            renderHeight = 800;
        }

        var renderTargetBitmap = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(renderWidth, renderHeight), new Vector(96 * scaleFactor, 96 * scaleFactor));
        renderTargetBitmap.Render(element);
        
        var stream = new System.IO.MemoryStream();
        renderTargetBitmap.Save(stream);
        stream.Position = 0;
        return stream;
    }
}