using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using BetterLyrics.Core.Models.Stats;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StoryPageTopArtist : UserControl
{
    public static readonly DependencyProperty PlayCountProperty = DependencyProperty.Register(
        "PlayCount", typeof(double), typeof(StoryPageTopArtist), new PropertyMetadata(0.0, OnPlayCountChanged));

    public double PlayCount
    {
        get => (double)GetValue(PlayCountProperty);
        set => SetValue(PlayCountProperty, value);
    }

    private static void OnPlayCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StoryPageTopArtist page)
        {
            page.PlayCountTextBlock.Text = ((double)e.NewValue).ToString("N0");
        }
    }

    public StoryPageTopArtist()
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

    public void PlayAnimation(ArtistPlayCount topArtist)
    {
        if (topArtist == null) return;
        
        ArtistNameTextBlock.Text = topArtist.Artist;
        PlayCount = 0;
        
        Text1.Opacity = 0;
        Panel1.Opacity = 0;
        Text2.Opacity = 0;
        Panel2.Opacity = 0;

        AnimateEntrance(Text1, 0.2);
        AnimateEntrance(Panel1, 0.7);
        AnimateEntrance(Text2, 1.2);
        AnimateEntrance(Panel2, 1.7);

        var storyboard = new Storyboard();
        var anim1 = new DoubleAnimation 
        { 
            From = 0, 
            To = topArtist.PlayCount, 
            Duration = new Duration(TimeSpan.FromSeconds(2.0)), 
            BeginTime = TimeSpan.FromSeconds(1.7),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim1, this);
        Storyboard.SetTargetProperty(anim1, "PlayCount");
        storyboard.Children.Add(anim1);
        storyboard.Begin();
    }
}
