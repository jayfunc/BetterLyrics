using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LyricsShareWindow : Window,
    IRecipient<PropertyChangedMessage<AppTheme>>
{
    public LyricsShareWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.Init("LyricsSharePageTitle");
        this.SyncTheme();
    }

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender is GeneralSettings)
            if (message.PropertyName == nameof(GeneralSettings.AppTheme))
                this.SyncTheme();
    }
}