using System;
using global::Avalonia;
using global::Avalonia.Animation;
using global::Avalonia.Controls;
using global::Avalonia.Media;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.Avalonia.Controls;

public partial class ImageSwitcher : UserControl
{
    public static readonly StyledProperty<int> CornerRadiusAmountProperty =
        AvaloniaProperty.Register<ImageSwitcher, int>(nameof(CornerRadiusAmount), 0);

    public static readonly StyledProperty<int> ShadowAmountProperty =
        AvaloniaProperty.Register<ImageSwitcher, int>(nameof(ShadowAmount), 0);

    // ImageSource translates to IImage in Avalonia
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<ImageSwitcher, IImage?>(nameof(Source));

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<ImageSwitcher, Stretch>(nameof(Stretch), Stretch.Uniform);

    public static readonly StyledProperty<ImageSwitchType> SwitchTypeProperty =
        AvaloniaProperty.Register<ImageSwitcher, ImageSwitchType>(nameof(SwitchType), ImageSwitchType.Crossfade);

    public ImageSwitcher()
    {
        InitializeComponent();

        // Initialize Transforms to avoid null references during animation
        LastAlbumArtImage.RenderTransform = new TranslateTransform();
        AlbumArtImage.RenderTransform = new TranslateTransform();
    }

    public int CornerRadiusAmount
    {
        get => GetValue(CornerRadiusAmountProperty);
        set => SetValue(CornerRadiusAmountProperty, value);
    }

    public int ShadowAmount
    {
        get => GetValue(ShadowAmountProperty);
        set => SetValue(ShadowAmountProperty, value);
    }

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public ImageSwitchType SwitchType
    {
        get => GetValue(SwitchTypeProperty);
        set => SetValue(SwitchTypeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty)
        {
            UpdateSource();
        }
    }

    private void UpdateSource()
    {
        switch (SwitchType)
        {
            case ImageSwitchType.Crossfade:
                UpdateSourceCrossfade();
                break;
            case ImageSwitchType.Slide:
                UpdateSourceSlide();
                break;
        }
    }

    private void UpdateSourceCrossfade()
    {
        LastAlbumArtImage.Source = AlbumArtImage.Source;

        // Snap values without animation
        ClearTransitions(LastAlbumArtImage);
        ClearTransitions(AlbumArtImage);

        SnapTransform(LastAlbumArtImage, 0);
        LastAlbumArtImage.Opacity = 1;

        SnapTransform(AlbumArtImage, 0);
        AlbumArtImage.Opacity = 0;

        AlbumArtImage.Source = Source;

        // Enable animations
        // Note: Assuming constants:Time.AnimationDuration is a TimeSpan.
        SetOpacityTransition(LastAlbumArtImage, Time.AnimationDuration);
        SetOpacityTransition(AlbumArtImage, Time.AnimationDuration);

        LastAlbumArtImage.Opacity = 0;
        AlbumArtImage.Opacity = 1;
    }

    private void UpdateSourceSlide()
    {
        LastAlbumArtImage.Source = AlbumArtImage.Source;

        // Snap values without animation
        ClearTransitions(LastAlbumArtImage);
        ClearTransitions(AlbumArtImage);

        LastAlbumArtImage.Opacity = 1;
        SnapTransform(LastAlbumArtImage, 0);

        AlbumArtImage.Opacity = 0;
        SnapTransform(AlbumArtImage, -Bounds.Width);

        AlbumArtImage.Source = Source;

        // Enable animations
        SetOpacityTransition(LastAlbumArtImage, Time.AnimationDuration);
        SetOpacityTransition(AlbumArtImage, Time.AnimationDuration);

        // Avalonia allows you to animate the properties of the Transform directly
        AnimateTransformX(LastAlbumArtImage, Time.AnimationDuration, -Bounds.Width);
        AnimateTransformX(AlbumArtImage, Time.AnimationDuration, 0);

        LastAlbumArtImage.Opacity = 0;
        AlbumArtImage.Opacity = 1;
    }

    // --- Helper Methods to handle dynamic transitions ---

    private void ClearTransitions(Control control)
    {
        control.Transitions = null;
        if (control.RenderTransform is TranslateTransform tt)
        {
            tt.Transitions = null;
        }
    }

    private void SetOpacityTransition(Control control, TimeSpan duration)
    {
        control.Transitions = new Transitions
        {
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = duration }
        };
    }

    private void SnapTransform(Control control, double xOffset)
    {
        if (control.RenderTransform is TranslateTransform tt)
        {
            tt.X = xOffset;
        }
    }

    private void AnimateTransformX(Control control, TimeSpan duration, double targetX)
    {
        if (control.RenderTransform is TranslateTransform tt)
        {
            tt.Transitions = new Transitions
            {
                new DoubleTransition { Property = TranslateTransform.XProperty, Duration = duration }
            };
            tt.X = targetX;
        }
    }
}