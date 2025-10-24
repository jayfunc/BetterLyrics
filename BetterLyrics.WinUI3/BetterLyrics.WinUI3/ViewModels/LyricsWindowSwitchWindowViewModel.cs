using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsWindowSwitchWindowViewModel : BaseViewModel
    {
        [ObservableProperty] public partial float RootGridOpacity { get; set; } = 1;
    }
}
