using BetterLyrics.Core.Models.Settings;

using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
