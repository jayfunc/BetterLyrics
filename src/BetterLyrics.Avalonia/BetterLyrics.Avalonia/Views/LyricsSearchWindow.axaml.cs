using global::Avalonia.Controls;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Avalonia.Views;

public partial class LyricsSearchWindow : Window, IRecipient<PropertyChangedMessage<AppTheme>>
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public LyricsSearchWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.RegisterAll(this);

        this.Init("LyricsSearchPageTitle");
        this.SyncTheme();

        Closing += LyricsSearchWindow_Closing;
    }

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender is GeneralSettings && message.PropertyName == nameof(GeneralSettings.AppTheme))
        {
            this.SyncTheme();
        }
    }

    private void LyricsSearchWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        _windowManagerProvider.CloseWindow(this);
    }
}