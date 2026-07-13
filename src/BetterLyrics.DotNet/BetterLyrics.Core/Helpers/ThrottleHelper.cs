namespace BetterLyrics.Core.Helpers;

public class ThrottleHelper
{
    private readonly TimeSpan _interval;
    private DateTime _lastTriggerTime = DateTime.MinValue;

    public ThrottleHelper(TimeSpan interval)
    {
        _interval = interval;
    }

    /// <summary>
    ///     判断是否可以触发（距离上次触发已超过设定间隔），如果可以则更新时间戳并返回 true，否则返回 false。
    /// </summary>
    public bool CanTrigger()
    {
        var now = DateTime.Now;
        if (now - _lastTriggerTime >= _interval)
        {
            _lastTriggerTime = now;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     重置触发时间
    /// </summary>
    public void Reset()
    {
        _lastTriggerTime = DateTime.MinValue;
    }
}