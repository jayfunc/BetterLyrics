using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.SettingsNavigation;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls;

public sealed partial class AppSettingsControl : BaseUserControl
{
    public AppSettingsControlViewModel ViewModel => (AppSettingsControlViewModel)DataContext;

    public AppSettingsControl()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<AppSettingsControlViewModel>();
    }

    protected override void Cleanup()
    {
        this.Bindings.StopTracking();
    }
}
