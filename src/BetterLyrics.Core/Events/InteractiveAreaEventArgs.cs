namespace BetterLyrics.Core.Events;

public class InteractiveAreaEventArgs : EventArgs
{
    public InteractiveAreaEventArgs(IList<object> elements)
    {
        Elements = elements;
    }

    public IList<object> Elements { get; set; }
}