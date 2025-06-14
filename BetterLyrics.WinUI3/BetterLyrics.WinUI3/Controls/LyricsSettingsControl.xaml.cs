using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.Lyrics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsSettingsControl : UserControl
    {
        public LyricsSettingsControl()
        {
            InitializeComponent();
        }

        public ILyricsViewModel ViewModel
        {
            get => (ILyricsViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ILyricsViewModel),
            typeof(LyricsSettingsControl),
            new PropertyMetadata(null)
        );

        public GlobalViewModel GlobalViewModel
        {
            get => (GlobalViewModel)GetValue(GlobalViewModelProperty);
            set => SetValue(GlobalViewModelProperty, value);
        }

        public static readonly DependencyProperty GlobalViewModelProperty =
            DependencyProperty.Register(
                nameof(GlobalViewModel),
                typeof(GlobalViewModel),
                typeof(LyricsSettingsControl),
                new PropertyMetadata(null)
            );
    }
}
