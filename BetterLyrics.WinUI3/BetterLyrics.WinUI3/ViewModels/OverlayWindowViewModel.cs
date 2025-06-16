using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class OverlayWindowViewModel(ISettingsService settingsService)
        : BaseViewModel(settingsService)
    {
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color ActivatedWindowAccentColor { get; set; }

        public void UpdateAccentColor(nint hwnd)
        {
            ActivatedWindowAccentColor = WindowColorHelper
                .GetDominantColorBelow(hwnd)
                .ToWindowsUIColor();
        }
    }
}
