using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace BetterLyrics.Avalonia.Controls;

public partial class ShadowImage : UserControl
{
    public static readonly StyledProperty<int> CornerRadiusAmountProperty =
        AvaloniaProperty.Register<ShadowImage, int>(nameof(CornerRadiusAmount), 0);

    public static readonly StyledProperty<int> ShadowAmountProperty =
        AvaloniaProperty.Register<ShadowImage, int>(nameof(ShadowAmount), 0);

    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<ShadowImage, IImage?>(nameof(Source));

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<ShadowImage, Stretch>(nameof(Stretch), Stretch.Uniform);

    public ShadowImage()
    {
        InitializeComponent();
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CornerRadiusAmountProperty)
        {
            UpdateCornerRadius();
        }
        else if (change.Property == ShadowAmountProperty)
        {
            UpdateShadow();
        }
        // If the control bounds change, re-evaluate the size-dependent corner radius calculation
        else if (change.Property == BoundsProperty)
        {
            UpdateCornerRadius();
        }
    }

    private void UpdateShadow()
    {
        if (ShadowRect == null) return;

        if (ShadowAmount <= 0)
        {
            // TODO
            //ShadowRect.BoxShadow = null;
            return;
        }

        // Translates WinUI's translation shadow into a modern look using Avalonia's BoxShadows.
        // Parameters: offsetX, offsetY, blurRadius, spreadRadius, color
        // Adjust the multipliers below if you want a softer or tighter shadow layout.
        double offset = ShadowAmount * 0.25;
        double blur = ShadowAmount * 1.5;

        ShadowRect.BoxShadow = new BoxShadows(
            new BoxShadow
            {
                OffsetX = 0,
                OffsetY = offset,
                Blur = blur,
                Spread = 0,
                Color = Color.FromArgb(80, 0, 0, 0) // Semi-transparent black shadow
            }
        );
    }

    private void UpdateCornerRadius()
    {
        if (ShadowRect == null) return;

        // Uses the inner Border's rendering bounds to correctly scale percentages
        var minSize = Math.Min(ShadowRect.Bounds.Height, ShadowRect.Bounds.Width);

        if (minSize <= 0) return;

        // Ported original calculation: mapping 0-100 values smoothly down to a circular radius edge
        double radiusValue = (CornerRadiusAmount / 100.0) * (minSize / 2.0);
        ShadowRect.CornerRadius = new CornerRadius(radiusValue);
    }
}