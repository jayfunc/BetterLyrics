namespace BetterLyrics.Core.Constants;

public static class Time
{
    public static readonly TimeSpan DebounceTimeout = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan LongDebounceTimeout = TimeSpan.FromMilliseconds(1000);
    public static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(350);
    public static readonly TimeSpan LongAnimationDuration = TimeSpan.FromMilliseconds(650);
    public static readonly TimeSpan WaitingDuration = TimeSpan.FromMilliseconds(300);
}