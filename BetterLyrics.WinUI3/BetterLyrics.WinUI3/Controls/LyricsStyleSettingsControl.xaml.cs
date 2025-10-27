using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsStyleSettingsControl : UserControl
    {
        public LyricsStyleSettingsControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LyricsStyleSettingsProperty =
            DependencyProperty.Register(nameof(LyricsStyleSettings), typeof(LyricsStyleSettings), typeof(LyricsStyleSettingsControl), new PropertyMetadata(default));

        public LyricsStyleSettings LyricsStyleSettings
        {
            get => (LyricsStyleSettings)GetValue(LyricsStyleSettingsProperty);
            set => SetValue(LyricsStyleSettingsProperty, value);
        }
    }
}
