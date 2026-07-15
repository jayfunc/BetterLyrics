using System.Runtime.CompilerServices;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Helpers;

public static class GridColumnVisibilityHelper
{
    private static readonly ConditionalWeakTable<Grid, System.ComponentModel.PropertyChangedEventHandler> _handlers = new();

    public static readonly DependencyProperty SyncWithMusicGallerySettingsProperty =
        DependencyProperty.RegisterAttached(
            "SyncWithMusicGallerySettings",
            typeof(bool),
            typeof(GridColumnVisibilityHelper),
            new PropertyMetadata(false, OnSyncWithMusicGallerySettingsChanged));

    public static bool GetSyncWithMusicGallerySettings(DependencyObject obj)
        => (bool)obj.GetValue(SyncWithMusicGallerySettingsProperty);

    public static void SetSyncWithMusicGallerySettings(DependencyObject obj, bool value)
        => obj.SetValue(SyncWithMusicGallerySettingsProperty, value);

    private static void OnSyncWithMusicGallerySettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Grid grid)
        {
            if ((bool)e.NewValue)
            {
                grid.Loaded -= Grid_Loaded;
                grid.Unloaded -= Grid_Unloaded;
                grid.Loaded += Grid_Loaded;
                grid.Unloaded += Grid_Unloaded;

                if (grid.IsLoaded)
                {
                    Grid_Loaded(grid, new RoutedEventArgs());
                }
            }
            else
            {
                grid.Loaded -= Grid_Loaded;
                grid.Unloaded -= Grid_Unloaded;
                Grid_Unloaded(grid, new RoutedEventArgs());
            }
        }
    }

    private static void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            var settings = Ioc.Default.GetRequiredService<ISettingsService>().AppSettings.MusicGallerySettings;

            void UpdateColumns()
            {
                if (grid.ColumnDefinitions.Count < 15) return;
                
                grid.ColumnDefinitions[2].Width = settings.ShowAlbumColumn ? new GridLength(2, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[3].Width = settings.ShowGenreColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[4].Width = settings.ShowYearColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[5].Width = settings.ShowTrackNumberColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[6].Width = settings.ShowBitrateColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[7].Width = settings.ShowSampleRateColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[8].Width = settings.ShowFormatColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[9].Width = settings.ShowFileSizeColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[10].Width = settings.ShowFolderColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[11].Width = settings.ShowDurationColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[12].Width = settings.ShowDateCreatedColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
                grid.ColumnDefinitions[13].Width = settings.ShowDateModifiedColumn ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

                foreach (UIElement child in grid.Children)
                {
                    if (child is FrameworkElement fe)
                    {
                        int col = Grid.GetColumn(fe);
                        bool isVisible = true;
                        switch (col)
                        {
                            case 2: isVisible = settings.ShowAlbumColumn; break;
                            case 3: isVisible = settings.ShowGenreColumn; break;
                            case 4: isVisible = settings.ShowYearColumn; break;
                            case 5: isVisible = settings.ShowTrackNumberColumn; break;
                            case 6: isVisible = settings.ShowBitrateColumn; break;
                            case 7: isVisible = settings.ShowSampleRateColumn; break;
                            case 8: isVisible = settings.ShowFormatColumn; break;
                            case 9: isVisible = settings.ShowFileSizeColumn; break;
                            case 10: isVisible = settings.ShowFolderColumn; break;
                            case 11: isVisible = settings.ShowDurationColumn; break;
                            case 12: isVisible = settings.ShowDateCreatedColumn; break;
                            case 13: isVisible = settings.ShowDateModifiedColumn; break;
                        }
                        fe.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }

            if (_handlers.TryGetValue(grid, out var oldHandler))
            {
                settings.PropertyChanged -= oldHandler;
                _handlers.Remove(grid);
            }

            System.ComponentModel.PropertyChangedEventHandler handler = (s, args) =>
            {
                grid.DispatcherQueue.TryEnqueue(() => UpdateColumns());
            };

            settings.PropertyChanged += handler;
            _handlers.Add(grid, handler);

            UpdateColumns();
        }
    }

    private static void Grid_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            var settings = Ioc.Default.GetRequiredService<ISettingsService>().AppSettings.MusicGallerySettings;
            if (_handlers.TryGetValue(grid, out var handler))
            {
                settings.PropertyChanged -= handler;
                _handlers.Remove(grid);
            }
        }
    }
}
