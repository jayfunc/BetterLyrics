using CommunityToolkit.Mvvm.ComponentModel;
using BetterLyrics.Core.Interfaces.ViewModels;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class LyricsWindowSwitchWindowViewModel : ObservableObject, ITransientViewModel
{
    [ObservableProperty] public partial float RootGridOpacity { get; set; } = 1;
}