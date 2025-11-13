using System;

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
