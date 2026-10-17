using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LyricsSearchWindow : Window,
    IRecipient<AppThemeChangedMessage>
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public LyricsSearchWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.RegisterAll(this);

        this.Init("LyricsSearchPageTitle");
        this.SyncTheme();

        AppWindow.Closing += AppWindow_Closing;
    }

    public void Receive(AppThemeChangedMessage message)
    {
        this.SyncTheme();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        _windowManagerProvider.CloseWindow(this);
    }
}