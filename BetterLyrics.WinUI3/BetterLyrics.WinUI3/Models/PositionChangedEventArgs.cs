using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public class PositionChangedEventArgs(TimeSpan position) : EventArgs()
    {
        public TimeSpan Position { get; set; } = position;
    }
}
