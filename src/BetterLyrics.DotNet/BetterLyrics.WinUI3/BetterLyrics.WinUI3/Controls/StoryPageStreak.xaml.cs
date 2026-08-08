using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StoryPageStreak : UserControl
{
    public StoryPageStreak()
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

    public static readonly DependencyProperty MaxCountProp = DependencyProperty.Register(
        "MaxCount", typeof(double), typeof(StoryPageStreak), new PropertyMetadata(0.0, OnMaxCountChanged));

    public double MaxCount
    {
        get => (double)GetValue(MaxCountProp);
        set => SetValue(MaxCountProp, value);
    }

    private static void OnMaxCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StoryPageStreak page)
        {
            page.MaxCountTextBlock.Text = ((double)e.NewValue).ToString("0");
        }
    }

    public static readonly DependencyProperty StreakCountProp = DependencyProperty.Register(
        "StreakCount", typeof(double), typeof(StoryPageStreak), new PropertyMetadata(0.0, OnStreakCountChanged));

    public double StreakCount
    {
        get => (double)GetValue(StreakCountProp);
        set => SetValue(StreakCountProp, value);
    }

    private static void OnStreakCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StoryPageStreak page)
        {
            page.StreakTextBlock.Text = ((double)e.NewValue).ToString("0");
        }
    }

    public void PlayAnimation(DateTime? maxDay, int maxCount, int streak)
    {
        MaxDateTextBlock.Text = maxDay?.ToString("M") ?? "--";
        MaxCount = 0;
        StreakCount = 0;
        
        Panel1.Opacity = 0;
        Panel2.Opacity = 0;
        Panel3.Opacity = 0;

        AnimateEntrance(Panel1, 0.2);
        AnimateEntrance(Panel2, 1.2);
        AnimateEntrance(Panel3, 2.2);

        var storyboard = new Storyboard();
        
        var anim1 = new DoubleAnimation 
        { 
            From = 0, 
            To = maxCount, 
            Duration = new Duration(TimeSpan.FromSeconds(2.0)), 
            BeginTime = TimeSpan.FromSeconds(1.2),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim1, this);
        Storyboard.SetTargetProperty(anim1, "MaxCount");
        storyboard.Children.Add(anim1);

        var anim2 = new DoubleAnimation 
        { 
            From = 0, 
            To = streak, 
            Duration = new Duration(TimeSpan.FromSeconds(2.0)),
            BeginTime = TimeSpan.FromSeconds(2.2),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim2, this);
        Storyboard.SetTargetProperty(anim2, "StreakCount");
        storyboard.Children.Add(anim2);

        storyboard.Begin();
    }
}
