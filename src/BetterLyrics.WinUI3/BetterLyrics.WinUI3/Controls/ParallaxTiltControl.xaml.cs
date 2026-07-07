using BetterLyrics.Core.Effects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.Controls;

[ContentProperty(Name = "Child")]
public sealed partial class ParallaxTiltControl : UserControl
{
    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register("Child", typeof(UIElement), typeof(ParallaxTiltControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsParallaxEnabledProperty =
        DependencyProperty.Register("IsParallaxEnabled", typeof(bool), typeof(ParallaxTiltControl),
            new PropertyMetadata(false, OnIsParallaxEnabledChanged));

    private bool _isLoaded;
    private bool _isRenderingSubscribed;

    public ParallaxTiltControl()
    {
        InitializeComponent();
    }

    public UIElement Child
    {
        get => (UIElement)GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public bool IsParallaxEnabled
    {
        get => (bool)GetValue(IsParallaxEnabledProperty);
        set => SetValue(IsParallaxEnabledProperty, value);
    }

    public ParallaxTiltEffect? ParallaxContext { get; set; }

    private static void OnIsParallaxEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ParallaxTiltControl control)
        {
            control.UpdateRenderingSubscription();

            if (!(bool)e.NewValue) control.ResetTilt();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        UpdateRenderingSubscription();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        UpdateRenderingSubscription();
    }

    private void UpdateRenderingSubscription()
    {
        var shouldSubscribe = _isLoaded && IsParallaxEnabled;

        if (shouldSubscribe && !_isRenderingSubscribed)
        {
            CompositionTarget.Rendering += OnRendering;
            _isRenderingSubscribed = true;
        }
        else if (!shouldSubscribe && _isRenderingSubscribed)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRenderingSubscribed = false;
        }
    }

    private void ResetTilt()
    {
        if (TiltProjection.RotationX != 0 || ParallaxTransform.TranslateX != 0)
        {
            TiltProjection.RotationX = 0;
            TiltProjection.RotationY = 0;
            ParallaxTransform.TranslateX = 0;
            ParallaxTransform.TranslateY = 0;
        }
    }

    private void OnRendering(object? sender, object e)
    {
        if (ParallaxContext == null) return;

        TiltProjection.RotationX = ParallaxContext.CurrentRotationX;
        TiltProjection.RotationY = ParallaxContext.CurrentRotationY;
        ParallaxTransform.TranslateX = ParallaxContext.CurrentTranslateX;
        ParallaxTransform.TranslateY = ParallaxContext.CurrentTranslateY;
    }
}