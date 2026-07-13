using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

[TemplatePart(Name = "PART_LyricsItemsControl", Type = typeof(ItemsControl))]
public sealed partial class LyricsCard : Control
{
    // ===

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(LyricsCard), new PropertyMetadata(null));

    // ===

    public static readonly DependencyProperty ArtistProperty =
        DependencyProperty.Register(nameof(Artist), typeof(string), typeof(LyricsCard), new PropertyMetadata(null));

    // ===

    public static readonly DependencyProperty CoverAccentColorProperty =
        DependencyProperty.Register(nameof(CoverAccentColor), typeof(Color), typeof(LyricsCard),
            new PropertyMetadata(null, OnDependencyPropertyChanged));

    // ===

    public static readonly DependencyProperty OverlayBrushProperty =
        DependencyProperty.Register(nameof(OverlayBrush), typeof(Brush), typeof(LyricsCard),
            new PropertyMetadata(null));

    // ===

    public static readonly DependencyProperty CoverImageProperty =
        DependencyProperty.Register(nameof(CoverImage), typeof(ImageSource), typeof(LyricsCard),
            new PropertyMetadata(null));

    // ===

    public static readonly DependencyProperty LyricsLinesProperty =
        DependencyProperty.Register(nameof(LyricsLines), typeof(IList<LyricsLine>), typeof(LyricsCard),
            new PropertyMetadata(null, OnDependencyPropertyChanged));

    // ===

    public static readonly DependencyProperty IsScrollableProperty =
        DependencyProperty.Register(nameof(IsScrollable), typeof(bool), typeof(LyricsCard),
            new PropertyMetadata(false, OnDependencyPropertyChanged));

    public static readonly DependencyProperty ConfigProperty =
        DependencyProperty.Register(nameof(Config), typeof(LyricsCardConfig), typeof(LyricsCard),
            new PropertyMetadata(new LyricsCardConfig()));

    // ===

    public static readonly DependencyProperty LyricsAreaSizeProperty =
        DependencyProperty.Register(
            nameof(LyricsAreaSize),
            typeof(double),
            typeof(LyricsCard),
            new PropertyMetadata(double.NaN)
        );

    // ===

    public static readonly DependencyProperty IsAutoScrollEnabledProperty =
        DependencyProperty.Register(
            nameof(IsAutoScrollEnabled),
            typeof(bool),
            typeof(LyricsCard),
            new PropertyMetadata(false));

    public static readonly DependencyProperty PlaybackPositionProperty =
        DependencyProperty.Register(
            nameof(PlaybackPosition),
            typeof(TimeSpan),
            typeof(LyricsCard),
            new PropertyMetadata(TimeSpan.Zero, OnDependencyPropertyChanged));

    public static readonly DependencyProperty StyleKeyProperty =
        DependencyProperty.Register(
            nameof(StyleKey),
            typeof(string),
            typeof(LyricsCard),
            new PropertyMetadata(null, OnDependencyPropertyChanged));

    private readonly IGsmtcService _gsmtcService;
    private readonly ISettingsService _settingsService;
    private int _currentActiveIndex = -1;
    private ItemsControl? _itemsControl;

    public LyricsCard()
    {
        DefaultStyleKey = typeof(LyricsCard);
        _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Artist
    {
        get => (string)GetValue(ArtistProperty);
        set => SetValue(ArtistProperty, value);
    }

    public Color? CoverAccentColor
    {
        get => (Color?)GetValue(CoverAccentColorProperty);
        set => SetValue(CoverAccentColorProperty, value);
    }

    public Brush OverlayBrush
    {
        get => (Brush)GetValue(OverlayBrushProperty);
        private set => SetValue(OverlayBrushProperty, value);
    }

    public ImageSource? CoverImage
    {
        get => (ImageSource?)GetValue(CoverImageProperty);
        set => SetValue(CoverImageProperty, value);
    }

    public IList<LyricsLine> LyricsLines
    {
        get => (IList<LyricsLine>)GetValue(LyricsLinesProperty);
        set => SetValue(LyricsLinesProperty, value);
    }

    public bool IsScrollable
    {
        get => (bool)GetValue(IsScrollableProperty);
        set => SetValue(IsScrollableProperty, value);
    }

    // ===

    public LyricsCardConfig Config
    {
        get => (LyricsCardConfig)GetValue(ConfigProperty);
        set => SetValue(ConfigProperty, value);
    }

    public double LyricsAreaSize
    {
        get => (double)GetValue(LyricsAreaSizeProperty);
        private set => SetValue(LyricsAreaSizeProperty, value);
    }

    public bool IsAutoScrollEnabled
    {
        get => (bool)GetValue(IsAutoScrollEnabledProperty);
        set => SetValue(IsAutoScrollEnabledProperty, value);
    }

    // ===

    public TimeSpan PlaybackPosition
    {
        get => (TimeSpan)GetValue(PlaybackPositionProperty);
        set => SetValue(PlaybackPositionProperty, value);
    }

    // ===

    public string StyleKey
    {
        get => (string)GetValue(StyleKeyProperty);
        set => SetValue(StyleKeyProperty, value);
    }

    // ===

    public string DateLong => DateTime.Now.ToString("dddd, MMMM d");
    public string DateShort => DateTime.Now.ToString("yyyy.MM.dd");

    public string TimeShort => DateTime.Now.ToString("HH:mm");
    public string TimeWithSeconds => DateTime.Now.ToString("HH:mm:ss");
    public string TimeWithSecondsReply => DateTime.Now.AddSeconds(2).ToString("HH:mm:ss");

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LyricsCard card)
        {
            if (e.Property == IsScrollableProperty)
            {
                card.LyricsAreaSize = (bool)e.NewValue ? 0.0 : double.NaN;
            }
            else if (e.Property == PlaybackPositionProperty || e.Property == LyricsLinesProperty)
            {
                if (card.LyricsLines != null)
                    card.UpdateActiveLine(card.PlaybackPosition +
                                          TimeSpan.FromMilliseconds(card._gsmtcService.CurrentMediaSourceProviderInfo
                                              ?.PositionOffset ?? 0));
            }
            else if (e.Property == CoverAccentColorProperty)
            {
                var color = (Color)e.NewValue;
                LinearGradientBrush gradientBrush = new()
                    { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };

                gradientBrush.GradientStops.Add(new GradientStop { Color = color.WithAlpha(180), Offset = 0.0 });
                gradientBrush.GradientStops.Add(new GradientStop
                {
                    Color = Color.FromArgb(220, (byte)(color.R / 2), (byte)(color.G / 2), (byte)(color.B / 2)),
                    Offset = 0.6
                });
                gradientBrush.GradientStops.Add(new GradientStop { Color = Colors.Black, Offset = 1.0 });

                card.OverlayBrush = gradientBrush;
            }
            else if (e.Property == StyleKeyProperty)
            {
                var styleKey = (string)e.NewValue;
                var found = card._settingsService.AppSettings.LyricsCardConfigs.FirstOrDefault(x =>
                    x.ResourceKey == styleKey);

                if (found == null)
                {
                    found = LyricsCardConfigExtensions.GetDefaultLyricsCardConfig(styleKey);
                    card._settingsService.AppSettings.LyricsCardConfigs.Add(found);
                }

                card.Config = found;
                if (App.Current.Resources.TryGetValue(styleKey, out var style)) card.Style = (Style)style;
            }
        }
    }

    private void UpdateActiveLine(TimeSpan position)
    {
        var lyrics = LyricsLines;
        var newActiveIndex = -1;

        for (var i = 0; i < lyrics.Count; i++)
            if (lyrics[i].StartMs <= position.TotalMilliseconds)
                newActiveIndex = i;
            else
                break;

        if (newActiveIndex != _currentActiveIndex && newActiveIndex != -1)
        {
            _currentActiveIndex = newActiveIndex;
            ScrollToLine(_currentActiveIndex);
        }
    }

    private void ScrollToLine(int index)
    {
        if (_itemsControl == null || index < 0 || index >= _itemsControl.Items.Count)
            return;

        var container = _itemsControl.ContainerFromIndex(index) as UIElement;
        container?.StartBringIntoView(new BringIntoViewOptions
        {
            AnimationDesired = true
        });
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _itemsControl = GetTemplateChild("PART_LyricsItemsControl") as ItemsControl;
    }
}