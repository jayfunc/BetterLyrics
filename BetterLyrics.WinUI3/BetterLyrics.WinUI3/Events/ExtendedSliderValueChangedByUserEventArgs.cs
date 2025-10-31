using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Events
{
    public class ExtendedSliderValueChangedByUserEventArgs : EventArgs
    {
        public double Value { get; set; }

        public ExtendedSliderValueChangedByUserEventArgs(double value)
        {
            Value = value;
        }
    }
}
