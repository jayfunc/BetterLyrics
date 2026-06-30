using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsWindowSwitchWindowViewModel : BaseViewModel
    {
        [ObservableProperty] public partial float RootGridOpacity { get; set; } = 1;
    }
}
