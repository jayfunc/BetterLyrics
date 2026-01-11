using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using static BetterLyrics.WinUI3.Helper.EasingHelper;

namespace BetterLyrics.WinUI3.Helper
{
    public class ValueTransition<T> where T : struct
    {
        // 状态变量
        private T _currentValue;
        private T _startValue;
        private T _targetValue;

        // 核心队列
        private readonly Queue<Keyframe<T>> _keyframeQueue = new Queue<Keyframe<T>>();

        // 时间控制
        private double _stepDuration;             // 当前这一段的时长 (动态变化)
        private double _totalDurationForAutoSplit; // 自动均分模式的总时长
        private double _configuredDelaySeconds;    // 配置的延迟时长

        // 动画状态
        private Func<T, T, double, T> _interpolator;
        private bool _isTransitioning;
        private double _progress; // 当前段的进度 (0.0 ~ 1.0)

        // 公开属性
        public T Value => _currentValue;
        public bool IsTransitioning => _isTransitioning;
        public T TargetValue => _targetValue; // 获取当前段的目标值
        public double DurationSeconds => _totalDurationForAutoSplit;

        public Func<T, T, double, T> Interpolator => _interpolator;

        public ValueTransition(T initialValue, Func<T, T, double, T>? interpolator, double defaultTotalDuration = 0.3)
        {
            _currentValue = initialValue;
            _startValue = initialValue;
            _targetValue = initialValue;
            _totalDurationForAutoSplit = defaultTotalDuration;

            if (interpolator != null)
            {
                _interpolator = interpolator;
            }
        }

        #region Configuration

        public void SetDuration(double seconds)
        {
            if (seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            _totalDurationForAutoSplit = seconds;
        }

        public void SetDurationMs(double millionSeconds) => SetDuration(millionSeconds / 1000.0);

        /// <summary>
        /// 设置启动延迟。
        /// 原理：在动画队列最前方插入一个“数值不变”的关键帧。
        /// </summary>
        public void SetDelay(double seconds)
        {
            _configuredDelaySeconds = seconds;
        }

        public void SetInterpolator(Func<T, T, double, T> interpolator)
        {
            _interpolator = interpolator;
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// 立即跳转到指定值（停止动画）
        /// </summary>
        public void JumpTo(T value)
        {
            _keyframeQueue.Clear();
            _currentValue = value;
            _startValue = value;
            _targetValue = value;
            _isTransitioning = false;
            _progress = 0;
        }

        /// <summary>
        /// 模式 A: 精确控制模式
        /// 显式指定每一段的目标值和时长。
        /// </summary>
        public void Start(params Keyframe<T>[] keyframes)
        {
            if (keyframes == null || keyframes.Length == 0) return;

            PrepareStart();

            // 1. 处理延迟 (插入静止帧)
            if (_configuredDelaySeconds > 0)
            {
                _keyframeQueue.Enqueue(new Keyframe<T>(_currentValue, _configuredDelaySeconds));
            }

            // 2. 入队用户帧
            foreach (var kf in keyframes)
            {
                _keyframeQueue.Enqueue(kf);
            }

            MoveToNextSegment(firstStart: true);
        }

        /// <summary>
        /// 模式 B: 自动均分模式 (兼容旧写法)
        /// 指定一串目标值，系统根据 SetDuration 的总时长平均分配。
        /// </summary>
        public void Start(params T[] values)
        {
            if (values == null || values.Length == 0) return;

            // 如果目标就是当前值且只有1帧，直接跳过以省性能
            if (values.Length == 1 && values[0].Equals(_currentValue) && _configuredDelaySeconds <= 0) return;

            PrepareStart();

            // 1. 处理延迟
            if (_configuredDelaySeconds > 0)
            {
                _keyframeQueue.Enqueue(new Keyframe<T>(_currentValue, _configuredDelaySeconds));
            }

            // 2. 计算均分时长
            double autoStepDuration = _totalDurationForAutoSplit / values.Length;

            // 3. 入队生成帧
            foreach (var val in values)
            {
                _keyframeQueue.Enqueue(new Keyframe<T>(val, autoStepDuration));
            }

            MoveToNextSegment(firstStart: true);
        }

        #endregion

        #region Core Logic

        private void PrepareStart()
        {
            _keyframeQueue.Clear();
            _isTransitioning = true;
        }

        private void MoveToNextSegment(bool firstStart = false)
        {
            if (_keyframeQueue.Count > 0)
            {
                var kf = _keyframeQueue.Dequeue();

                // 起点逻辑：如果是刚开始，起点是当前值；如果是中间切换，起点是上一段的终点
                _startValue = firstStart ? _currentValue : _targetValue;
                _targetValue = kf.Value;
                _stepDuration = kf.Duration;

                if (firstStart) _progress = 0f;
                // 注意：非 firstStart 时不重置 _progress，保留溢出值以平滑过渡
            }
            else
            {
                // 队列耗尽，动画结束
                _currentValue = _targetValue;
                _isTransitioning = false;
                _progress = 1f;
            }
        }

        public void Update(TimeSpan elapsedTime)
        {
            if (!_isTransitioning) return;

            double timeStep = elapsedTime.TotalSeconds;

            // 使用 while 处理单帧时间过长跨越多段的情况
            while (timeStep > 0 && _isTransitioning)
            {
                // 计算当前帧的步进比例
                // 极小值保护，防止除以0
                double progressDelta = (_stepDuration > 0.000001) ? (timeStep / _stepDuration) : 1.0;

                if (_progress + progressDelta >= 1.0)
                {
                    // === 当前段结束 ===

                    // 1. 计算这一段实际消耗的时间
                    double timeConsumed = (1.0 - _progress) * _stepDuration;

                    // 2. 剩余时间留给下一段
                    timeStep -= timeConsumed;

                    // 3. 修正当前值到目标值
                    _progress = 1.0;
                    _currentValue = _targetValue;

                    // 4. 切换到下一段
                    MoveToNextSegment();

                    // 5. 如果还有下一段，进度归零
                    if (_isTransitioning) _progress = 0f;
                }
                else
                {
                    // === 当前段进行中 ===
                    _progress += progressDelta;
                    timeStep = 0; // 时间耗尽

                    // 插值计算
                    _currentValue = _interpolator(_startValue, _targetValue, _progress);
                }
            }
        }

        #endregion

        #region Interpolators

        public static Func<T, T, double, T> GetInterpolatorByEasingType(EasingType? type, EaseMode easingMode)
        {
            if (typeof(T) == typeof(double))
            {
                return (start, end, progress) =>
                {
                    double s = (double)(object)start;
                    double e = (double)(object)end;

                    Func<double, double> easeInFunc = type switch
                    {
                        Enums.EasingType.Sine => EaseInSine,
                        Enums.EasingType.Quad => EaseInQuad,
                        Enums.EasingType.Cubic => EaseInCubic,
                        Enums.EasingType.Quart => EaseInQuart,
                        Enums.EasingType.Quint => EaseInQuint,
                        Enums.EasingType.Expo => EaseInExpo,
                        Enums.EasingType.Circle => EaseInCircle,
                        Enums.EasingType.Back => EaseInBack,
                        Enums.EasingType.Elastic => EaseInElastic,
                        Enums.EasingType.Bounce => EaseInBounce,
                        Enums.EasingType.SmoothStep => SmoothStep,
                        Enums.EasingType.Linear => Linear,
                        _ => EaseInQuad,
                    };
                    double t = Ease(progress, easingMode, easeInFunc);

                    return (T)(object)(s + (e - s) * t);
                };
            }

            throw new NotSupportedException($"Type {typeof(T)} is not supported.");
        }

        #endregion
    }
}