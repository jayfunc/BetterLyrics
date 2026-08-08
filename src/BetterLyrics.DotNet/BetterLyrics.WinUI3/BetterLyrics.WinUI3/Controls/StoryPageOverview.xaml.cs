using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StoryPageOverview : UserControl
{
    public static readonly DependencyProperty TracksCountProperty = DependencyProperty.Register(
        "TracksCount", typeof(double), typeof(StoryPageOverview), new PropertyMetadata(0.0, OnTracksCountChanged));

    public double TracksCount
    {
        get => (double)GetValue(TracksCountProperty);
        set => SetValue(TracksCountProperty, value);
    }

    private static void OnTracksCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StoryPageOverview page)
        {
            page.TracksTextBlock.Text = ((double)e.NewValue).ToString("N0");
        }
    }

    public static readonly DependencyProperty HoursCountProperty = DependencyProperty.Register(
        "HoursCount", typeof(double), typeof(StoryPageOverview), new PropertyMetadata(0.0, OnHoursCountChanged));

    public double HoursCount
    {
        get => (double)GetValue(HoursCountProperty);
        set => SetValue(HoursCountProperty, value);
    }

    private static void OnHoursCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StoryPageOverview page)
        {
            page.HoursTextBlock.Text = ((double)e.NewValue).ToString("0.0");
        }
    }

    public StoryPageOverview()
    {
        this.InitializeComponent();
    }
    
    private void AnimateEntrance(UIElement el, double delaySec)
    {
        var transform = new Microsoft.UI.Xaml.Media.TranslateTransform { Y = 30 };
        el.RenderTransform = transform;
        
        var sb = new Storyboard();
        
        var opAnim = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.8), BeginTime = TimeSpan.FromSeconds(delaySec), EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(opAnim, el);
        Storyboard.SetTargetProperty(opAnim, "Opacity");
        
        var transAnim = new DoubleAnimation { From = 30, To = 0, Duration = TimeSpan.FromSeconds(0.8), BeginTime = TimeSpan.FromSeconds(delaySec), EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 } };
        Storyboard.SetTarget(transAnim, transform);
        Storyboard.SetTargetProperty(transAnim, "Y");
        
        sb.Children.Add(opAnim);
        sb.Children.Add(transAnim);
        sb.Begin();
    }

    public void PlayAnimation(int targetTracks, double targetHours)
    {
        TracksCount = 0;
        HoursCount = 0;
        Text1.Opacity = 0;
        Text2.Opacity = 0;
        Panel1.Opacity = 0;
        Text3.Opacity = 0;
        Panel2.Opacity = 0;

        AnimateEntrance(Text1, 0.2);
        AnimateEntrance(Text2, 0.7);
        AnimateEntrance(Panel1, 1.2);
        AnimateEntrance(Text3, 1.8);
        AnimateEntrance(Panel2, 2.3);

        var storyboard = new Storyboard();
        
        var anim1 = new DoubleAnimation 
        { 
            From = 0, 
            To = targetTracks, 
            Duration = new Duration(TimeSpan.FromSeconds(2.0)), 
            BeginTime = TimeSpan.FromSeconds(1.2),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim1, this);
        Storyboard.SetTargetProperty(anim1, "TracksCount");
        storyboard.Children.Add(anim1);

        var anim2 = new DoubleAnimation 
        { 
            From = 0, 
            To = targetHours, 
            Duration = new Duration(TimeSpan.FromSeconds(2.0)),
            BeginTime = TimeSpan.FromSeconds(2.3),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim2, this);
        Storyboard.SetTargetProperty(anim2, "HoursCount");
        storyboard.Children.Add(anim2);

        storyboard.Begin();
    }
}
