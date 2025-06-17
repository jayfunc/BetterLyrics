using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels;
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

        //public ILyricsSettingsControlViewModel ViewModel
        //{
        //    get => (ILyricsSettingsControlViewModel)GetValue(ViewModelProperty);
        //    set => SetValue(ViewModelProperty, value);
        //}

        //public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        //    nameof(ViewModel),
        //    typeof(ILyricsSettingsControlViewModel),
        //    typeof(LyricsSettingsControl),
        //    new PropertyMetadata(null)
        //);

        public LyricsSettingsControlViewModel ViewModel
        {
            get => (LyricsSettingsControlViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            nameof(ViewModel),
            typeof(LyricsSettingsControlViewModel),
            typeof(LyricsSettingsControl),
            new PropertyMetadata(null)
        );
    }
}
