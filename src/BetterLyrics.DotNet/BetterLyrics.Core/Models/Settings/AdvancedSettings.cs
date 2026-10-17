using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class AdvancedSettings : ObservableObject
{
    [ObservableProperty]
    public partial int FPS { get; set; } = 60;
}