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
    public sealed partial class LyricsEffectSettingsControl : UserControl
    {
        public LyricsEffectSettingsControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LyricsEffectSettingsProperty =
            DependencyProperty.Register(nameof(LyricsEffectSettings), typeof(LyricsEffectSettings), typeof(LyricsEffectSettingsControl), new PropertyMetadata(default));

        public LyricsEffectSettings LyricsEffectSettings
        {
            get => (LyricsEffectSettings)GetValue(LyricsEffectSettingsProperty);
            set => SetValue(LyricsEffectSettingsProperty, value);
        }

    }
}
