using Avalonia.Controls;
using Avalonia.Interactivity;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Avalonia.Controls;

public partial class PluginManagerControl : UserControl
{
    public PluginManagerControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PluginManagerControlViewModel>();
    }

    public PluginManagerControlViewModel ViewModel => (PluginManagerControlViewModel)DataContext!;

    private void UninstallPluginButton_Click(object? sender, RoutedEventArgs e)
    {
        // 获取事件触发者上下文
        if (sender is Control { DataContext: PluginInfo plugin })
        {
            ViewModel.UninstallPluginCommand.Execute(plugin.Plugin);
        }
    }
}