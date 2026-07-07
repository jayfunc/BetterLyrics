using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Threading;
using System;
using BetterLyrics.Core.Effects;

namespace BetterLyrics.Avalonia.Controls;

public partial class ParallaxTiltControl : UserControl
{
    public static readonly StyledProperty<Control?> ChildProperty =
        AvaloniaProperty.Register<ParallaxTiltControl, Control?>(nameof(Child));

    public static readonly StyledProperty<bool> IsParallaxEnabledProperty =
        AvaloniaProperty.Register<ParallaxTiltControl, bool>(nameof(IsParallaxEnabled), false);

    private bool _isLoaded;
    private DispatcherTimer? _renderTimer;

    public ParallaxTiltControl()
    {
        InitializeComponent();
    }

    [Content] // Equivalent to WinUI's [ContentProperty(Name = "Child")]
    public Control? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public bool IsParallaxEnabled
    {
        get => GetValue(IsParallaxEnabledProperty);
        set => SetValue(IsParallaxEnabledProperty, value);
    }

    public ParallaxTiltEffect? ParallaxContext { get; set; }

    // Avalonia's native way to handle property changes instead of static callbacks
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
            // Simulate CompositionTarget.Rendering with a 60fps DispatcherTimer on the Render thread
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
        //if (TiltProjection != null && ParallaxTransform != null)
        //{
        //    // Properties on Rotate3DTransform use AngleX/Y instead of RotationX/Y
        //    TiltProjection.AngleX = 0;
        //    TiltProjection.AngleY = 0;

        //    // TranslateTransform uses X and Y directly
        //    ParallaxTransform.X = 0;
        //    ParallaxTransform.Y = 0;
        //}
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (ParallaxContext == null) return;

        // TODO

        //if (TiltProjection == null || ParallaxTransform == null) return;

        //TiltProjection.AngleX = ParallaxContext.CurrentRotationX;
        //TiltProjection.AngleY = ParallaxContext.CurrentRotationY;
        //ParallaxTransform.X = ParallaxContext.CurrentTranslateX;
        //ParallaxTransform.Y = ParallaxContext.CurrentTranslateY;
    }
}