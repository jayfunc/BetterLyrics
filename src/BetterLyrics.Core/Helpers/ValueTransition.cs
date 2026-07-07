using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models;
using static BetterLyrics.Core.Helpers.EasingHelper;

namespace BetterLyrics.Core.Helpers;

public class ValueTransition<T> where T : struct
{
    // 核心队列
    private readonly Queue<Keyframe<T>> _keyframeQueue = new();

    private double _configuredDelaySeconds; // 配置的延迟时长

    // 状态变量

    // 动画状态
    private T _startValue;

    // 时间控制
    private double _stepDuration; // 当前这一段的时长 (动态变化)

    public ValueTransition(T initialValue, Func<T, T, double, T>? interpolator, double defaultTotalDuration = 0.3)
    {
        Value = initialValue;
        _startValue = initialValue;
        TargetValue = initialValue;
        DurationSeconds = defaultTotalDuration;

        if (interpolator != null) Interpolator = interpolator;
    }

    // 公开属性
    public T Value { get; private set; }

    public bool IsTransitioning { get; private set; }

    public T TargetValue { get; private set; }

    public double DurationSeconds { get; private set; }

    public double Progress { get; private set; }

    public Func<T, T, double, T> Interpolator { get; private set; }

    #region Interpolators

    public static Func<T, T, double, T> GetInterpolatorByEasingType(EasingType? type, EaseMode easingMode)
    {
        if (typeof(T) == typeof(double))
            return (start, end, progress) =>
            {
                var s = (double)(object)start;
                var e = (double)(object)end;

                Func<double, double> easeInFunc = type switch
                {
                    EasingType.Sine => EaseInSine,
                    EasingType.Quad => EaseInQuad,
                    EasingType.Cubic => EaseInCubic,
                    EasingType.Quart => EaseInQuart,
                    EasingType.Quint => EaseInQuint,
                    EasingType.Expo => EaseInExpo,
                    EasingType.Circle => EaseInCircle,
                    EasingType.Back => EaseInBack,
                    EasingType.Elastic => EaseInElastic,
                    EasingType.Bounce => EaseInBounce,
                    EasingType.SmoothStep => SmoothStep,
                    EasingType.Linear => Linear,
                    _ => EaseInQuad
                };
                var t = Ease(progress, easingMode, easeInFunc);

                return (T)(object)(s + (e - s) * t);
            };

        throw new NotSupportedException($"Type {typeof(T)} is not supported.");
    }

    #endregion

    #region Configuration

    public void SetDuration(double seconds)
    {
        if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        DurationSeconds = seconds;
    }

    public void SetDurationMs(double millionSeconds)
    {
        SetDuration(millionSeconds / 1000.0);
    }

    /// <summary>
    ///     设置启动延迟。
    ///     原理：在动画队列最前方插入一个“数值不变”的关键帧。
    /// </summary>
    public void SetDelay(double seconds)
    {
        _configuredDelaySeconds = seconds;
    }

    public void SetInterpolator(Func<T, T, double, T> interpolator)
    {
        Interpolator = interpolator;
    }

    #endregion

    #region Control Methods

    /// <summary>
    ///     立即跳转到指定值（停止动画）
    /// </summary>
    public void JumpTo(T value)
    {
        _keyframeQueue.Clear();
        Value = value;
        _startValue = value;
        TargetValue = value;
        IsTransitioning = false;
        Progress = 0;
    }

    /// <summary>
    ///     模式 A: 精确控制模式
    ///     显式指定每一段的目标值和时长。
    /// </summary>
    public void Start(params Keyframe<T>[] keyframes)
    {
        if (keyframes == null || keyframes.Length == 0) return;

        PrepareStart();

        // 1. 处理延迟 (插入静止帧)
        if (_configuredDelaySeconds > 0) _keyframeQueue.Enqueue(new Keyframe<T>(Value, _configuredDelaySeconds));

        // 2. 入队用户帧
        foreach (var kf in keyframes) _keyframeQueue.Enqueue(kf);

        MoveToNextSegment(true);
    }

    /// <summary>
    ///     模式 B: 自动均分模式 (兼容旧写法)
    ///     指定一串目标值，系统根据 SetDuration 的总时长平均分配。
    /// </summary>
    public void Start(params T[] values)
    {
        if (values == null || values.Length == 0) return;

        // 如果目标就是当前值且只有1帧，直接跳过以省性能
        if (values.Length == 1 && values[0].Equals(Value) && _configuredDelaySeconds <= 0) return;

        PrepareStart();

        // 1. 处理延迟
        if (_configuredDelaySeconds > 0) _keyframeQueue.Enqueue(new Keyframe<T>(Value, _configuredDelaySeconds));

        // 2. 计算均分时长
        var autoStepDuration = DurationSeconds / values.Length;

        // 3. 入队生成帧
        foreach (var val in values) _keyframeQueue.Enqueue(new Keyframe<T>(val, autoStepDuration));

        MoveToNextSegment(true);
    }

    #endregion

    #region Core Logic

    private void PrepareStart()
    {
        _keyframeQueue.Clear();
        IsTransitioning = true;
    }

    private void MoveToNextSegment(bool firstStart = false)
    {
        if (_keyframeQueue.Count > 0)
        {
            var kf = _keyframeQueue.Dequeue();

            // 起点逻辑：如果是刚开始，起点是当前值；如果是中间切换，起点是上一段的终点
            _startValue = firstStart ? Value : TargetValue;
            TargetValue = kf.Value;
            _stepDuration = kf.Duration;

            if (firstStart) Progress = 0f;
            // 注意：非 firstStart 时不重置 _progress，保留溢出值以平滑过渡
        }
        else
        {
            // 队列耗尽，动画结束
            Value = TargetValue;
            IsTransitioning = false;
            Progress = 1f;
        }
    }

    public void Update(TimeSpan elapsedTime)
    {
        if (!IsTransitioning) return;

        var timeStep = elapsedTime.TotalSeconds;

        // 使用 while 处理单帧时间过长跨越多段的情况
        while (timeStep > 0 && IsTransitioning)
        {
            // 计算当前帧的步进比例
            // 极小值保护，防止除以0
            var progressDelta = _stepDuration > 0.000001 ? timeStep / _stepDuration : 1.0;

            if (Progress + progressDelta >= 1.0)
            {
                // === 当前段结束 ===

                // 1. 计算这一段实际消耗的时间
                var timeConsumed = (1.0 - Progress) * _stepDuration;

                // 2. 剩余时间留给下一段
                timeStep -= timeConsumed;

                // 3. 修正当前值到目标值
                Progress = 1.0;
                Value = TargetValue;

                // 4. 切换到下一段
                MoveToNextSegment();

                // 5. 如果还有下一段，进度归零
                if (IsTransitioning) Progress = 0f;
            }
            else
            {
                // === 当前段进行中 ===
                Progress += progressDelta;
                timeStep = 0; // 时间耗尽

                // 插值计算
                Value = Interpolator(_startValue, TargetValue, Progress);
            }
        }
    }

    #endregion
}