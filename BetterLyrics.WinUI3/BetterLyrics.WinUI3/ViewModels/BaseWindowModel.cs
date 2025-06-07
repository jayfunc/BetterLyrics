using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseWindowModel : ObservableObject
    {
        [ObservableProperty]
        private int _titleBarFontSize = 11;
    }
}
