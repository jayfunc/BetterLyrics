using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class AdvancedSettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsFixedTimeStep { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int FPS { get; set; } = 60;
    }
}
