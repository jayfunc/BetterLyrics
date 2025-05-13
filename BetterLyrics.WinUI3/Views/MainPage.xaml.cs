using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Control;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using DevWinUI;
using Windows.UI.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using BetterLyrics.WinUI3.ViewModels;
using Microsoft.UI.Xaml.Hosting;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// Please note that this page was implemented by traditional XAML + C# code-behind, not MVVM.
    /// I tried before but there was a difficulty in accessing UI thread on non-UI threads via MVVM.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel => (MainViewModel)this.DataContext;

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetService<MainViewModel>();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.LyricsLineIndex))
            {
                ScrollToCurrentLyricsLine();
            }
        }

        private void LyricsArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScrollToCurrentLyricsLine();
        }

        private void LyricsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LyricsListView.SelectedIndex = -1;
        }

        private void ScrollToCurrentLyricsLine()
        {
            // Always leave half of the LyricsArea height at the head and tail of the whole lyrics
            double offset = LyricsArea.ActualHeight / 2;

            for (int i = 0; i <= ViewModel.LyricsLineIndex; i++)
            {
                var container = LyricsListView.ContainerFromIndex(i);
                if (container == null) return;

                var containerHeight = (container as ListViewItem).ActualHeight;

                if (i == ViewModel.LyricsLineIndex)
                {
                    // Make sure the current lyrics line is esactly at the center of the lyrics area
                    offset -= containerHeight / 2;
                }
                else
                {
                    offset -= containerHeight;
                }
            }

            if (ViewModel.LyricsLineIndex >= ViewModel.LyricsLines.Count) return;

            int lyricsLineDurationMs = ViewModel.LyricsLines[ViewModel.LyricsLineIndex].DurationMs;

            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                To = offset,
                // Make sure the duration of animation always <= lyrics line duration
                Duration = TimeSpan.FromMilliseconds(Math.Min(650, lyricsLineDurationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(animation, LyricsStackPanelTranslateTransform);
            Storyboard.SetTargetProperty(animation, "Y");

            storyboard.Children.Add(animation);

            storyboard.Begin();
        }

    }
}
