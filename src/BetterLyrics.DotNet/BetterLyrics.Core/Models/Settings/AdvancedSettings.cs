using BetterLyrics.Core.Messages.AppSettings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.Core.Models.Settings;

public partial class AdvancedSettings : ObservableObject
{
    [ObservableProperty]
    public partial int FPS { get; set; } = 60;

    [ObservableProperty]
    public partial bool WriteDebugLogs { get; set; } = false;

    partial void OnWriteDebugLogsChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new WriteDebugLogsChangedMessage(value));
}