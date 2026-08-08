using System;
using System.Collections.Generic;
using BetterLyrics.Core.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Linq;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StoryViewerControl : UserControl
{
    public delegate void CloseRequestedHandler();
    public event CloseRequestedHandler CloseRequested;

    public StatsDashboardControlViewModel ViewModel { get; set; }
    
    private List<SceneConfiguration> _pageScenes;

    public StoryViewerControl()
    {
        this.InitializeComponent();
    }

    public void StartStory(StatsDashboardControlViewModel vm)
    {
        ViewModel = vm;
        GenerateScenes();
        BuildPages();
        StoryFlipView.SelectedIndex = 0;
        UpdateProgressBars();
    }

    private void GenerateScenes()
    {
        var rnd = new System.Random();
        _pageScenes = new List<SceneConfiguration>();

        // Hand-crafted preset combinations that are guaranteed to look good
        var curatedPresets = new List<SceneConfiguration>
        {
            new SceneConfiguration { Background = BackgroundStyle.WarmDark, Ambient = AmbientElement.GodRays, Overlay = OverlayEffect.DustParticles }, // Sunlight
            new SceneConfiguration { Background = BackgroundStyle.DeepGreen, Ambient = AmbientElement.BlurredBlobs, Overlay = OverlayEffect.None }, // TreeShade
            new SceneConfiguration { Background = BackgroundStyle.DeepBlue, Ambient = AmbientElement.TrainPole, Overlay = OverlayEffect.TrainStreaks }, // Train
            new SceneConfiguration { Background = BackgroundStyle.DeepSpace, Ambient = AmbientElement.None, Overlay = OverlayEffect.Stars }, // StarryNight
            new SceneConfiguration { Background = BackgroundStyle.Gloomy, Ambient = AmbientElement.None, Overlay = OverlayEffect.RainDrops }, // RainyWindow
            new SceneConfiguration { Background = BackgroundStyle.PitchBlack, Ambient = AmbientElement.StageSpotlights, Overlay = OverlayEffect.None }, // StageSpotlight
            new SceneConfiguration { Background = BackgroundStyle.GoldBlack, Ambient = AmbientElement.SweepingHighlight, Overlay = OverlayEffect.VinylRings }, // VinylGrooves
            new SceneConfiguration { Background = BackgroundStyle.NeonPurple, Ambient = AmbientElement.NeonGrid, Overlay = OverlayEffect.DataParticles }, // Cyberpunk
            new SceneConfiguration { Background = BackgroundStyle.FrostBlue, Ambient = AmbientElement.NorthernLights, Overlay = OverlayEffect.Snowflakes }, // Winter
            new SceneConfiguration { Background = BackgroundStyle.SunsetOrange, Ambient = AmbientElement.None, Overlay = OverlayEffect.FloatingEmbers } // Sunset
        };

        // We need 5 distinct scenes for the 5 pages
        for (int i = 0; i < 5; i++)
        {
            if (curatedPresets.Count == 0) break;
            
            int index = rnd.Next(curatedPresets.Count);
            _pageScenes.Add(curatedPresets[index]);
            curatedPresets.RemoveAt(index);
        }
    }

    private void BuildPages()
    {
        StoryFlipView.Items.Clear();
        
        // Page 1: Overview
        var page1 = new StoryPageOverview();
        StoryFlipView.Items.Add(page1);

        // Page 2: Top Song
        var page2 = new StoryPageTopSong();
        StoryFlipView.Items.Add(page2);

        // Page 3: Top Artist
        var page3 = new StoryPageTopArtist();
        StoryFlipView.Items.Add(page3);

        // Page 4: Time
        var page4 = new StoryPageTime();
        StoryFlipView.Items.Add(page4);

        // Page 5: Summary
        var page5 = new StoryPageSummary();
        StoryFlipView.Items.Add(page5);

        BuildProgressIndicators();
    }

    private void BuildProgressIndicators()
    {
        ProgressIndicatorList.Items.Clear();
        for (int i = 0; i < StoryFlipView.Items.Count; i++)
        {
            var bar = new Border
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Colors.White) { Opacity = 0.3 },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = 40
            };
            ProgressIndicatorList.Items.Add(bar);
        }
    }

    private void LeftHalf_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (StoryFlipView.SelectedIndex > 0)
        {
            StoryFlipView.SelectedIndex--;
        }
    }

    private void RightHalf_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (StoryFlipView.SelectedIndex < StoryFlipView.Items.Count - 1)
        {
            StoryFlipView.SelectedIndex++;
        }
        else
        {
            CloseRequested?.Invoke();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke();
    }

    private void StoryFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateProgressBars();

        if (ViewModel == null || StoryFlipView.Items.Count == 0 || _pageScenes == null) return;

        BackgroundControl.SetScene(_pageScenes[StoryFlipView.SelectedIndex]);

        var currentPage = StoryFlipView.Items[StoryFlipView.SelectedIndex];
        
        if (currentPage is StoryPageOverview overviewPage)
        {
            overviewPage.PlayAnimation(ViewModel.TotalTracksPlayed, ViewModel.TotalDuration.TotalHours);
        }
        else if (currentPage is StoryPageTopSong topSongPage)
        {
            topSongPage.PlayAnimation(ViewModel.TopSongs?.FirstOrDefault());
        }
        else if (currentPage is StoryPageTopArtist topArtistPage)
        {
            topArtistPage.PlayAnimation(ViewModel.TopArtists?.FirstOrDefault());
        }
        else if (currentPage is StoryPageTime timePage)
        {
            timePage.PlayAnimation(ViewModel.PeakHourText, ViewModel.QuietHourText);
        }
        else if (currentPage is StoryPageSummary summaryPage)
        {
            summaryPage.PlayAnimation(ViewModel);
        }
    }

    private void UpdateProgressBars()
    {
        for (int i = 0; i < ProgressIndicatorList.Items.Count; i++)
        {
            var bar = (Border)ProgressIndicatorList.Items[i];
            if (i < StoryFlipView.SelectedIndex)
            {
                bar.Background = new SolidColorBrush(Colors.White);
            }
            else if (i == StoryFlipView.SelectedIndex)
            {
                bar.Background = new SolidColorBrush(Colors.White); // Can animate this later
            }
            else
            {
                bar.Background = new SolidColorBrush(Colors.White) { Opacity = 0.3 };
            }
        }
    }
}
