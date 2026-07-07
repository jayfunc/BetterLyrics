using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterLyrics.Avalonia.Controls;

[TemplatePart("PART_LyricsItemsControl", typeof(ItemsControl))]
public sealed partial class LyricsCard : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<LyricsCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> ArtistProperty =
        AvaloniaProperty.Register<LyricsCard, string?>(nameof(Artist));

    public static readonly StyledProperty<Color?> CoverAccentColorProperty =
        AvaloniaProperty.Register<LyricsCard, Color?>(nameof(CoverAccentColor));

    public static readonly StyledProperty<IBrush?> OverlayBrushProperty =
        AvaloniaProperty.Register<LyricsCard, IBrush?>(nameof(OverlayBrush));

    // WinUI's ImageSource is translated to Avalonia's IImage
    public static readonly StyledProperty<IImage?> CoverImageProperty =
        AvaloniaProperty.Register<LyricsCard, IImage?>(nameof(CoverImage));

    public static readonly StyledProperty<IList<LyricsLine>?> LyricsLinesProperty =
        AvaloniaProperty.Register<LyricsCard, IList<LyricsLine>?>(nameof(LyricsLines));

    public static readonly StyledProperty<bool> IsScrollableProperty =
        AvaloniaProperty.Register<LyricsCard, bool>(nameof(IsScrollable), false);

    public static readonly StyledProperty<LyricsCardConfig> ConfigProperty =
        AvaloniaProperty.Register<LyricsCard, LyricsCardConfig>(nameof(Config), new LyricsCardConfig());

    public static readonly StyledProperty<double> LyricsAreaSizeProperty =
        AvaloniaProperty.Register<LyricsCard, double>(nameof(LyricsAreaSize), double.NaN);

    public static readonly StyledProperty<bool> IsAutoScrollEnabledProperty =
        AvaloniaProperty.Register<LyricsCard, bool>(nameof(IsAutoScrollEnabled), false);

    public static readonly StyledProperty<TimeSpan> PlaybackPositionProperty =
        AvaloniaProperty.Register<LyricsCard, TimeSpan>(nameof(PlaybackPosition), TimeSpan.Zero);

    public static readonly StyledProperty<string?> StyleKeyProperty =
        AvaloniaProperty.Register<LyricsCard, string?>(nameof(StyleKey));

    private readonly IGsmtcService _gsmtcService;
    private readonly ISettingsService _settingsService;
    private int _currentActiveIndex = -1;
    private ItemsControl? _itemsControl;

    public LyricsCard()
    {
        _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Artist
    {
        get => GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public Color? CoverAccentColor
    {
        get => GetValue(CoverAccentColorProperty);
        set => SetValue(CoverAccentColorProperty, value);
    }

    public IBrush? OverlayBrush
    {
        get => GetValue(OverlayBrushProperty);
        private set => SetValue(OverlayBrushProperty, value);
    }

    public IImage? CoverImage
    {
        get => GetValue(CoverImageProperty);
        set => SetValue(CoverImageProperty, value);
    }

    public IList<LyricsLine>? LyricsLines
    {
        get => GetValue(LyricsLinesProperty);
        set => SetValue(LyricsLinesProperty, value);
    }

    public bool IsScrollable
    {
        get => GetValue(IsScrollableProperty);
        set => SetValue(IsScrollableProperty, value);
    }

    public LyricsCardConfig Config
    {
        get => GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    public double LyricsAreaSize
    {
        get => GetValue(LyricsAreaSizeProperty);
        private set => SetValue(LyricsAreaSizeProperty, value);
    }

    public bool IsAutoScrollEnabled
    {
        get => GetValue(IsAutoScrollEnabledProperty);
        set => SetValue(IsAutoScrollEnabledProperty, value);
    }

    public TimeSpan PlaybackPosition
    {
        get => GetValue(PlaybackPositionProperty);
        set => SetValue(PlaybackPositionProperty, value);
    }

    public string? StyleKey
    {
        get => GetValue(StyleKeyProperty);
        set => SetValue(StyleKeyProperty, value);
    }

    public string DateLong => DateTime.Now.ToString("dddd, MMMM d");
    public string DateShort => DateTime.Now.ToString("yyyy.MM.dd");
    public string TimeShort => DateTime.Now.ToString("HH:mm");
    public string TimeWithSeconds => DateTime.Now.ToString("HH:mm:ss");
    public string TimeWithSecondsReply => DateTime.Now.AddSeconds(2).ToString("HH:mm:ss");

    // Avalonia's native way to react to StyledProperty changes
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsScrollableProperty)
        {
            LyricsAreaSize = change.GetNewValue<bool>() ? 0.0 : double.NaN;
        }
        else if (change.Property == PlaybackPositionProperty || change.Property == LyricsLinesProperty)
        {
            if (LyricsLines != null)
            {
                UpdateActiveLine(PlaybackPosition +
                                 TimeSpan.FromMilliseconds(_gsmtcService.CurrentMediaSourceProviderInfo?.PositionOffset ?? 0));
            }
        }
        else if (change.Property == CoverAccentColorProperty)
        {
            var newColor = change.GetNewValue<Color?>();
            if (newColor.HasValue)
            {
                var color = newColor.Value;

                // Use RelativePoint for standard Top to Bottom linear gradients
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
                };

                // Replicated the WithAlpha extension manually for Avalonia Colors
                gradientBrush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(180, color.R, color.G, color.B), Offset = 0.0 });
                gradientBrush.GradientStops.Add(new GradientStop
                {
                    Color = Color.FromArgb(220, (byte)(color.R / 2), (byte)(color.G / 2), (byte)(color.B / 2)),
                    Offset = 0.6
                });
                gradientBrush.GradientStops.Add(new GradientStop { Color = Colors.Black, Offset = 1.0 });

                OverlayBrush = gradientBrush;
            }
        }
        else if (change.Property == StyleKeyProperty)
        {
            var styleKey = change.GetNewValue<string?>();
            if (!string.IsNullOrEmpty(styleKey))
            {
                var found = _settingsService.AppSettings.LyricsCardConfigs.FirstOrDefault(x => x.ResourceKey == styleKey);

                if (found == null)
                {
                    // Assuming you have an Avalonia equivalent extensions file for this fallback
                    found = LyricsCardConfigExtensions.GetDefaultLyricsCardConfig(styleKey);
                    _settingsService.AppSettings.LyricsCardConfigs.Add(found);
                }

                Config = found;

                // Avalonia 11 styling lookup for ControlThemes
                if (Application.Current!.TryGetResource(styleKey, out var styleResource) && styleResource is ControlTheme theme)
                {
                    Theme = theme;
                }
            }
        }
    }

    private void UpdateActiveLine(TimeSpan position)
    {
        var lyrics = LyricsLines;
        if (lyrics == null) return;

        var newActiveIndex = -1;

        for (var i = 0; i < lyrics.Count; i++)
        {
            if (lyrics[i].StartMs <= position.TotalMilliseconds)
                newActiveIndex = i;
            else
                break;
        }

        if (newActiveIndex != _currentActiveIndex && newActiveIndex != -1)
        {
            _currentActiveIndex = newActiveIndex;
            ScrollToLine(_currentActiveIndex);
        }
    }

    private void ScrollToLine(int index)
    {
        if (_itemsControl == null || index < 0 || index >= _itemsControl.ItemCount)
            return;

        // ContainerFromIndex exists in Avalonia's ItemsControl
        if (_itemsControl.ContainerFromIndex(index) is Control container)
        {
            // Avalonia's native method to bring UI elements into view
            container.BringIntoView();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Use the NameScope provided by the TemplateAppliedEventArgs to find named parts
        _itemsControl = e.NameScope.Find<ItemsControl>("PART_LyricsItemsControl");
    }
}