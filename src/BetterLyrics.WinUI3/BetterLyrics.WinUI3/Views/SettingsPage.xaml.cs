// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
    }

    public SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;
}