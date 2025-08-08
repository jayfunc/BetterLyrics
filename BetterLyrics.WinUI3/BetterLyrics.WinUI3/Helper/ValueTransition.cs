// 2025/6/23 by Zhe Fang

using System;
using System.Diagnostics;
using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Helper
{
    public class ValueTransition<T>
        where T : struct
    {
        private T _currentValue;
        private double _durationSeconds;
        private EasingType? _easingType;
        private Func<T, T, double, T> _interpolator;
        private bool _isTransitioning;
        private double _progress;
        private T _startValue;
        private T _targetValue;

        public double DurationSeconds => _durationSeconds;

        public bool IsTransitioning => _isTransitioning;
        public T Value => _currentValue;
        public T TargetValue => _targetValue;

        public ValueTransition(T initialValue, double durationSeconds, Func<T, T, double, T>? interpolator = null, EasingType? easingType = null)
        {
            _currentValue = initialValue;
            _startValue = initialValue;
            _targetValue = initialValue;
            _durationSeconds = durationSeconds;
            _progress = 1f;
            _isTransitioning = false;

            if (interpolator != null)
            {
                _interpolator = interpolator;
                _easingType = null;
            }
            else if (easingType.HasValue)
            {
                _easingType = easingType;
                _interpolator = GetInterpolatorByEasingType(_easingType.Value);
            }
            else
            {
                _easingType = EasingType.Linear;
                _interpolator = GetInterpolatorByEasingType(_easingType.Value);
            }
        }

        public void SetDuration(double seconds)
        {
            if (seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds), "Duration must be positive.");
            _durationSeconds = seconds;
        }

        private void JumpTo(T value)
        {
            _currentValue = value;
            _startValue = value;
            _targetValue = value;
            _progress = 1f;
            _isTransitioning = false;
        }

        public void Reset(T value)
        {
            _currentValue = value;
            _startValue = value;
            _targetValue = value;
            _progress = 0f;
            _isTransitioning = false;
        }

        public void StartTransition(T targetValue, bool jumpTo = false)
        {
            if (jumpTo)
            {
                JumpTo(targetValue);
                return;
            }

            if (!targetValue.Equals(_currentValue))
            {
                _startValue = _currentValue;
                _targetValue = targetValue;
                _progress = 0f;
                _isTransitioning = true;
            }
        }

        public static bool Equals(double x, double y, double tolerance)
        {
            var diff = Math.Abs(x - y);
            return diff <= tolerance || diff <= Math.Max(Math.Abs(x), Math.Abs(y)) * tolerance;
        }

        public void Update(TimeSpan elapsedTime)
        {
            if (!_isTransitioning) return;

            _progress += (double)(elapsedTime / TimeSpan.FromSeconds(_durationSeconds));
            if (_progress >= 1f)
            {
                _progress = 1f;
                _currentValue = _targetValue;
                _isTransitioning = false;
            }
            else
            {
                _currentValue = _interpolator(_startValue, _targetValue, _progress);
            }
        }

        private Func<T, T, double, T> GetInterpolatorByEasingType(EasingType type)
        {
            if (typeof(T) == typeof(double))
            {
                return (start, end, progress) =>
                {
                    double s = (double)(object)start;
                    double e = (double)(object)end;
                    double t = progress;
                    switch (type)
                    {
                        case EasingType.EaseInOutSine:
                            t = EasingHelper.EaseInOutSine(t);
                            break;
                        case EasingType.EaseInOutQuad:
                            t = EasingHelper.EaseInOutQuad(t);
                            break;
                        case EasingType.EaseInOutCubic:
                            t = EasingHelper.EaseInOutCubic(t);
                            break;
                        case EasingType.EaseInOutQuart:
                            t = EasingHelper.EaseInOutQuart(t);
                            break;
                        case EasingType.EaseInOutQuint:
                            t = EasingHelper.EaseInOutQuint(t);
                            break;
                        case EasingType.EaseInOutExpo:
                            t = EasingHelper.EaseInOutExpo(t);
                            break;
                        case EasingType.EaseInOutCirc:
                            t = EasingHelper.EaseInOutCirc(t);
                            break;
                        case EasingType.EaseInOutBack:
                            t = EasingHelper.EaseInOutBack(t);
                            break;
                        case EasingType.EaseInOutElastic:
                            t = EasingHelper.EaseInOutElastic(t);
                            break;
                        case EasingType.EaseInOutBounce:
                            t = EasingHelper.EaseInOutBounce(t);
                            break;
                        case EasingType.SmoothStep:
                            t = EasingHelper.SmoothStep(t);
                            break;
                        case EasingType.Linear:
                            t = EasingHelper.Linear(t);
                            break;
                        default:
                            break;
                    }
                    return (T)(object)(s + (e - s) * t);
                };
            }
            throw new NotSupportedException($"Easing type {type} is not supported for type {typeof(T)}.");
        }

        public void SetEasingType(EasingType easingType)
        {
            _easingType = easingType;
            _interpolator = GetInterpolatorByEasingType(easingType);
        }
    }
}
