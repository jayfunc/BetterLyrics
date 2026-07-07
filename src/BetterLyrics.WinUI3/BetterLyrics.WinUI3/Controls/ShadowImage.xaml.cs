using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class ShadowImage : UserControl
{
    public static readonly DependencyProperty CornerRadiusAmountProperty =
        DependencyProperty.Register(nameof(CornerRadiusAmount), typeof(int), typeof(ShadowImage),
            new PropertyMetadata(0, OnDependencyPropertyChanged));

    public static readonly DependencyProperty ShadowAmountProperty =
        DependencyProperty.Register(nameof(ShadowAmount), typeof(int), typeof(ShadowImage),
            new PropertyMetadata(0, OnDependencyPropertyChanged));

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(ShadowImage),
            new PropertyMetadata(null));

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(ShadowImage),
            new PropertyMetadata(Stretch.Uniform));

    public ShadowImage()
    {
        InitializeComponent();
    }

    public int CornerRadiusAmount
    {
        get => (int)GetValue(CornerRadiusAmountProperty);
        set => SetValue(CornerRadiusAmountProperty, value);
    }

    public int ShadowAmount
    {
        get => (int)GetValue(ShadowAmountProperty);
        set => SetValue(ShadowAmountProperty, value);
    }

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShadowImage shadowImage)
        {
            if (e.Property == CornerRadiusAmountProperty)
            {
                shadowImage.UpdateShadowCastGridCornerRadius();
                shadowImage.UpdateShadowRectCornerRadius();
            }
            else if (e.Property == ShadowAmountProperty)
            {
                shadowImage.UpdateShadowRectShadow();
            }
        }
    }

    private void UpdateShadowRectShadow()
    {
        ShadowRect.Translation = new Vector3(0, 0, ShadowAmount);
    }

    private void UpdateShadowCastGridCornerRadius()
    {
        var minSize = Math.Min(ShadowCastGrid.ActualHeight, ShadowCastGrid.ActualWidth);
        ShadowCastGrid.CornerRadius = new CornerRadius(CornerRadiusAmount / 100.0 * (minSize / 2));
    }

    private void UpdateShadowRectCornerRadius()
    {
        var minSize = Math.Min(ShadowRect.ActualHeight, ShadowRect.ActualWidth);
        ShadowRect.CornerRadius = new CornerRadius(CornerRadiusAmount / 100.0 * (minSize / 2));
    }

    private void ShadowCastGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateShadowCastGridCornerRadius();
    }

    private void ShadowRect_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateShadowRectCornerRadius();
    }

    private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
    {
        Shadow.Receivers.Add(ShadowCastGrid);
    }
}