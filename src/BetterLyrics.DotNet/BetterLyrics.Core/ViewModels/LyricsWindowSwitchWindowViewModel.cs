using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Singleton ViewModel
/// </summary>
public partial class LyricsWindowSwitchWindowViewModel : ObservableObject
{
    [ObservableProperty] public partial float RootGridOpacity { get; set; } = 1;
}