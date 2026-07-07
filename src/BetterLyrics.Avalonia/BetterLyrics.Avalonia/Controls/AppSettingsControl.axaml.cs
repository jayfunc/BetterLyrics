using Avalonia.Controls;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Controls;

public partial class AppSettingsControl : UserControl
{
    public AppSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<AppSettingsControlViewModel>();
    }

    public AppSettingsControlViewModel ViewModel => (AppSettingsControlViewModel)DataContext!;
}