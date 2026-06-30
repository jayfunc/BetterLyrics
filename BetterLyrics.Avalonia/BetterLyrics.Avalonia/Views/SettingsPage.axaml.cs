using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BetterLyrics.Avalonia.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Views
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
        }
    }
}