namespace BetterLyrics.Core.Events
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
