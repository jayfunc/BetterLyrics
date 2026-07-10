using System.Threading.Tasks;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Views;

public partial class LyricsWindowSwitchWindow : Window
{
    private readonly IWindowManagerProvider _windowManagerProvider;

    public LyricsWindowSwitchWindow()
    {
        InitializeComponent();
        _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();
        DataContext = Ioc.Default.GetRequiredService<LyricsWindowSwitchControlViewModel>();
    }
    
    public LyricsWindowSwitchControlViewModel ViewModel => (LyricsWindowSwitchControlViewModel)DataContext;

    private void Grid_Tapped(object? sender, TappedEventArgs e)
    {
        HideWindow();
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        HideWindow();
    }

    private void HideWindow()
    {
        _windowManagerProvider.HideWindow(this);
    }

    private void SettingsHypelinkButton_Click(object? sender, RoutedEventArgs e)
    {
        HideWindow();
        _windowManagerProvider.OpenOrShowWindow<SettingsWindow>();
        var settingsPageViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
        settingsPageViewModel.NavigateToSection(SettingsSection.LyricsWindowMgr);
    }
}