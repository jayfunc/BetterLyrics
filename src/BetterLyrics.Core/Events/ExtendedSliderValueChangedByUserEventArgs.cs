namespace BetterLyrics.Core.Events;

public class ExtendedSliderValueChangedByUserEventArgs : EventArgs
{
    public ExtendedSliderValueChangedByUserEventArgs(double value)
    {
        Value = value;
    }

    public double Value { get; set; }
}