using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Models
{
    public partial class WindowBoundsRecord : ObservableObject
    {
        [ObservableProperty] public partial string MonitorDeviceName { get; set; } = string.Empty;
        [ObservableProperty] public partial Rect WindowBounds { get; set; }
        [ObservableProperty] public partial Rect DemoWindowBounds { get; set; }
        [ObservableProperty] public partial Rect MonitorBounds { get; set; }
        [ObservableProperty] public partial Rect DemoMonitorBounds { get; set; }
    }
}
