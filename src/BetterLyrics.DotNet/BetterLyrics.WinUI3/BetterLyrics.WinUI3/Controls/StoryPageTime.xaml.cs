using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StoryPageTime : UserControl
{
    public StoryPageTime()
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

    public void PlayAnimation(string peakHour, string quietHour)
    {
        PeakHourTextBlock.Text = peakHour;
        QuietHourTextBlock.Text = quietHour;
        
        Text1.Opacity = 0;
        Panel1.Opacity = 0;
        Text2.Opacity = 0;
        Text3.Opacity = 0;
        Panel2.Opacity = 0;

        AnimateEntrance(Text1, 0.2);
        AnimateEntrance(Panel1, 0.7);
        AnimateEntrance(Text2, 1.2);
        AnimateEntrance(Text3, 1.7);
        AnimateEntrance(Panel2, 2.2);
    }
}
