using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StoryPageSpecialMoment : UserControl
{
    public StoryPageSpecialMoment()
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

    public void PlayAnimation(string firstSong, string lateNightSong, DateTime? lateNightDate)
    {
        FirstSongTextBlock.Text = !string.IsNullOrEmpty(firstSong) ? $"《{firstSong}》" : "--";
        LateNightSongTextBlock.Text = !string.IsNullOrEmpty(lateNightSong) ? $"《{lateNightSong}》" : "--";
        
        LateNightDateTextBlock.Text = lateNightDate?.ToString("D") ?? "--";
        LateNightTimeTextBlock.Text = lateNightDate?.ToString("H:mm") ?? "--";

        Text1.Opacity = 0;
        Text2.Opacity = 0;
        Panel1.Opacity = 0;
        LateNightPanel1.Opacity = 0;
        LateNightPanel2.Opacity = 0;
        Panel2.Opacity = 0;

        AnimateEntrance(Text1, 0.2);
        AnimateEntrance(Text2, 0.7);
        AnimateEntrance(Panel1, 1.2);
        
        if (!string.IsNullOrEmpty(lateNightSong))
        {
            AnimateEntrance(LateNightPanel1, 2.2);
            AnimateEntrance(LateNightPanel2, 2.7);
            AnimateEntrance(Panel2, 3.2);
        }
    }
}
