// 2025/6/23 by Zhe Fang

using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        public partial object NavViewSelectedItemTag { get; set; } = "App";

        public SettingsPageViewModel() { }
    }
}
