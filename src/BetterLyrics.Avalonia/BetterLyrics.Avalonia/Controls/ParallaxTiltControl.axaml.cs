using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using BetterLyrics.Core.Effects;
using System;

namespace BetterLyrics.Avalonia.Controls;

public partial class ParallaxTiltControl : UserControl
{
    public static readonly StyledProperty<bool> IsParallaxEnabledProperty =
        AvaloniaProperty.Register<ParallaxTiltControl, bool>(nameof(IsParallaxEnabled), false);

    private bool _isLoaded;
    private DispatcherTimer? _renderTimer;

    private Rotate3DTransform? TiltProjection => (Rotate3DTransform?)RootGrid.RenderTransform;
    private TranslateTransform? ParallaxTransform => (TranslateTransform?)ContentPresenter.RenderTransform;

    public ParallaxTiltControl()
    {
        InitializeComponent();
    }

    public bool IsParallaxEnabled
    {
        get => GetValue(IsParallaxEnabledProperty);
        set => SetValue(IsParallaxEnabledProperty, value);
    }

    public ParallaxTiltEffect? ParallaxContext { get; set; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsParallaxEnabledProperty)
        {
            UpdateRenderingSubscription();

            if (!IsParallaxEnabled)
            {
                ResetTilt();
            }
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        UpdateRenderingSubscription();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        UpdateRenderingSubscription();
    }

    private void UpdateRenderingSubscription()
    {
        var shouldSubscribe = _isLoaded && IsParallaxEnabled;

        if (shouldSubscribe && _renderTimer == null)
        {
            _renderTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(16),
                DispatcherPriority.Render,
                OnRendering);
            _renderTimer.Start();
        }
        else if (!shouldSubscribe && _renderTimer != null)
        {
            _renderTimer.Stop();
            _renderTimer = null;
        }
    }

    private void ResetTilt()
    {
        if (TiltProjection != null && ParallaxTransform != null)
        {
            TiltProjection.AngleX = 0;
            TiltProjection.AngleY = 0;

            ParallaxTransform.X = 0;
            ParallaxTransform.Y = 0;
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (ParallaxContext == null) return;

        if (TiltProjection == null || ParallaxTransform == null) return;

        TiltProjection.AngleX = ParallaxContext.CurrentRotationX;
        TiltProjection.AngleY = ParallaxContext.CurrentRotationY;
        ParallaxTransform.X = ParallaxContext.CurrentTranslateX;
        ParallaxTransform.Y = ParallaxContext.CurrentTranslateY;
    }
}