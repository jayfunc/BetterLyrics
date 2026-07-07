using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

public partial class LyricsWindowSwitchWindowViewModel : BaseViewModel
{
    [ObservableProperty] public partial float RootGridOpacity { get; set; } = 1;
}