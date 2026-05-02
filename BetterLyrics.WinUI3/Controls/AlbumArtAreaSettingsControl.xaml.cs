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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class AlbumArtAreaSettingsControl : UserControl
    {
        public static readonly DependencyProperty LyricsWindowStatusProperty =
            DependencyProperty.Register(nameof(LyricsWindowStatus), typeof(LyricsWindowStatus), typeof(AlbumArtAreaSettingsControl), new PropertyMetadata(null));

        public LyricsWindowStatus LyricsWindowStatus
        {
            get => (LyricsWindowStatus)GetValue(LyricsWindowStatusProperty);
            set => SetValue(LyricsWindowStatusProperty, value);
        }

        public AlbumArtAreaSettingsControl()
        {
            InitializeComponent();
        }
    }
}
