using BetterLyrics.Core.Interfaces;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class PluginManagerControl : UserControl
    {
        public PluginManagerControlViewModel ViewModel => (PluginManagerControlViewModel)DataContext;

        public PluginManagerControl()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<PluginManagerControlViewModel>();
        }

        private void UninstallPluginButton_Click(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            var plugin = (PluginInfo)element.DataContext;
            ViewModel.UninstallPluginCommand.Execute(plugin.Plugin);
        }
    }
}
