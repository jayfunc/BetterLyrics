using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class StatsDashboardWindow : Window,
    IRecipient<AppThemeChangedMessage>
{
    public StatsDashboardWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.Init("SettingsPageStats");
        this.SyncTheme();
    }

    public void Receive(AppThemeChangedMessage message)
    {
        this.SyncTheme();
    }
}