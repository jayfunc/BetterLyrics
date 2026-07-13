namespace BetterLyrics.Core.Models;

public struct Keyframe<T>
{
    public T Value { get; }
    public double Duration { get; }

    public Keyframe(T value, double durationSeconds)
    {
        Value = value;
        Duration = durationSeconds;
    }
}