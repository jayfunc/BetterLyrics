using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;

namespace BetterLyrics.WinUI3.Models.Settings
{
    public partial class StandardModeSettings : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial Rect WindowBounds { get; set; } = new Rect(100, 100, 1000, 600);
    
        public StandardModeSettings() { }
    }
}
