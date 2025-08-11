using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
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
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class MediaSettingsControl : UserControl
    {
        public MediaSettingsControlViewModel ViewModel => (MediaSettingsControlViewModel)DataContext;
        public MediaSettingsControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<MediaSettingsControlViewModel>();
        }

        private void SettingsPageRemovePathButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.RemoveFolderAsync((LocalMediaFolder)(sender as HyperlinkButton)!.Tag);
        }

        private async void LocalFolderHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button && button.Tag is string uriStr)
            {
                if (Uri.TryCreate(uriStr, UriKind.Absolute, out var uri))
                {
                    await Launcher.LaunchUriAsync(uri);
                }
            }
        }
    }
}
