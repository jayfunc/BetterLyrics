// 2025/6/23 by Zhe Fang

using System;
using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Helper
{
    public class AnimationHelper
    {
        public const int DebounceDefaultDuration = 200;
        public const int StackedNotificationsShowingDuration = 3900;
        public const int StoryboardDefaultDuration = 200;
    }

    public class ValueTransition<T>
        where T : struct
    {
        private T _currentValue;
        private float _durationSeconds;
        private readonly EasingType? _easingType;
        private Func<T, T, float, T> _interpolator;
        private bool _isTransitioning;
        private float _progress;
        private T _startValue;
        private T _targetValue;

        public bool IsTransitioning => _isTransitioning;
        public T Value => _currentValue;

        public ValueTransition(T initialValue, float durationSeconds, Func<T, T, float, T>? interpolator = null, EasingType? easingType = null)
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
                _interpolator = GetInterpolatorByEasingType(easingType.Value);
            }
            else
            {
                _interpolator = GetInterpolatorByEasingType(EasingType.Linear);
                _easingType = EasingType.Linear;
            }
        }

        public void JumpTo(T value)
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

        public void StartTransition(T targetValue)
        {
            if (!targetValue.Equals(_currentValue))
            {
                _startValue = _currentValue;
                _targetValue = targetValue;
                _progress = 0f;
                _isTransitioning = true;
            }
        }

        public void Update(TimeSpan elapsedTime)
        {
            if (!_isTransitioning) return;

            _progress += (float)elapsedTime.TotalSeconds / _durationSeconds;
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

        private Func<T, T, float, T> GetInterpolatorByEasingType(EasingType type)
        {
            if (typeof(T) == typeof(float))
            {
                return (start, end, progress) =>
                {
                    float s = (float)(object)start;
                    float e = (float)(object)end;
                    float t = progress;
                    switch (type)
                    {
                        case EasingType.EaseInOutExpo:
                            t = EasingHelper.EaseInOutExpo(t);
                            break;
                        case EasingType.EaseInOutQuad:
                            t = EasingHelper.EaseInOutQuad(t);
                            break;
                        case EasingType.EaseInQuad:
                            t = EasingHelper.EaseInQuad(t);
                            break;
                        case EasingType.EaseOutQuad:
                            t = EasingHelper.EaseOutQuad(t);
                            break;
                        case EasingType.Linear:
                            t = EasingHelper.Linear(t);
                            break;
                        case EasingType.SmoothStep:
                            t = EasingHelper.SmoothStep(t);
                            break;
                        case EasingType.SmootherStep:
                            t = EasingHelper.SmootherStep(t);
                            break;
                        default:
                            break;
                    }
                    return (T)(object)(s + (e - s) * t);
                };
            }
            throw new NotSupportedException($"Easing type {type} is not supported for type {typeof(T)}.");
        }
    }
}
