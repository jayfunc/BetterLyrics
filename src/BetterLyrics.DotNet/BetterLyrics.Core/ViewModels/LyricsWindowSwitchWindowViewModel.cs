using BetterLyrics.Core.Interfaces.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class LyricsWindowSwitchWindowViewModel : ObservableObject, ITransientViewModel
{
    [ObservableProperty] public partial float RootGridOpacity { get; set; } = 1;
}