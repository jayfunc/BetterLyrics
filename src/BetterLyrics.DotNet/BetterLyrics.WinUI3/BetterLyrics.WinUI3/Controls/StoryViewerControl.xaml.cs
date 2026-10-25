using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class StoryViewerControl : BaseUserControl
{
    public delegate void CloseRequestedHandler();
    public event CloseRequestedHandler CloseRequested;

    public StatsDashboardControlViewModel ViewModel { get; set; }

    private List<AmbientSceneConfiguration> _pageScenes;

    public StoryViewerControl()
    {
        this.InitializeComponent();
    }

    override protected void BindGeneratedControls()
    {
    }

    override protected void UnbindGeneratedControls()
    {
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
        _pageScenes = new List<AmbientSceneConfiguration>();

        // Hand-crafted preset combinations that are guaranteed to look good
        var curatedPresets = new List<AmbientSceneConfiguration>
        {
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.WarmDark, Ambient = AmbientElement.GodRays, Overlay = AmbientOverlayEffect.DustParticles }, // Sunlight
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.DeepGreen, Ambient = AmbientElement.BlurredBlobs, Overlay = AmbientOverlayEffect.None }, // TreeShade
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.DeepBlue, Ambient = AmbientElement.TrainPole, Overlay = AmbientOverlayEffect.TrainStreaks }, // Train
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.DeepSpace, Ambient = AmbientElement.None, Overlay = AmbientOverlayEffect.Stars }, // StarryNight
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.Gloomy, Ambient = AmbientElement.None, Overlay = AmbientOverlayEffect.RainDrops }, // RainyWindow
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.PitchBlack, Ambient = AmbientElement.StageSpotlights, Overlay = AmbientOverlayEffect.None }, // StageSpotlight
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.GoldBlack, Ambient = AmbientElement.SweepingHighlight, Overlay = AmbientOverlayEffect.VinylRings }, // VinylGrooves
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.NeonPurple, Ambient = AmbientElement.NeonGrid, Overlay = AmbientOverlayEffect.DataParticles }, // Cyberpunk
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.FrostBlue, Ambient = AmbientElement.NorthernLights, Overlay = AmbientOverlayEffect.Snowflakes }, // Winter
            new AmbientSceneConfiguration { Background = AmbientBackgroundStyle.SunsetOrange, Ambient = AmbientElement.None, Overlay = AmbientOverlayEffect.FloatingEmbers } // Sunset
        };

        // We need 8 distinct scenes for the 8 pages
        for (int i = 0; i < 8; i++)
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

        // Page 2: Streak
        var pageStreak = new StoryPageStreak();
        StoryFlipView.Items.Add(pageStreak);

        // Page 3: Special Moment
        var pageSpecial = new StoryPageSpecialMoment();
        StoryFlipView.Items.Add(pageSpecial);

        // Page 4: Top Song
        var page2 = new StoryPageTopSong();
        StoryFlipView.Items.Add(page2);

        // Page 5: Top Artist
        var page3 = new StoryPageTopArtist();
        StoryFlipView.Items.Add(page3);

        // Page 6: Time
        var page4 = new StoryPageTime();
        StoryFlipView.Items.Add(page4);

        // Page 7: Persona
        var pagePersona = new StoryPagePersona();
        StoryFlipView.Items.Add(pagePersona);

        // Page 8: Summary
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
        else if (currentPage is StoryPageStreak streakPage)
        {
            streakPage.PlayAnimation(ViewModel.MaxListeningDay, ViewModel.MaxListeningDayCount, ViewModel.LongestStreakDays);
        }
        else if (currentPage is StoryPageSpecialMoment specialPage)
        {
            specialPage.PlayAnimation(ViewModel.FirstSongName, ViewModel.LateNightSongName, ViewModel.LateNightSongDate);
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
        else if (currentPage is StoryPagePersona personaPage)
        {
            personaPage.PlayAnimation(ViewModel.PersonaTitle, ViewModel.PersonaDescription);
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
