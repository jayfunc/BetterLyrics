// 2025/6/23 by Zhe Fang

using System;
using BetterLyrics.WinUI3.Enums;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="AnimationHelper" />
    /// </summary>
    public class AnimationHelper
    {
        #region Constants

        /// <summary>
        /// Defines the DebounceDefaultDuration
        /// </summary>
        public const int DebounceDefaultDuration = 200;

        /// <summary>
        /// Defines the StackedNotificationsShowingDuration
        /// </summary>
        public const int StackedNotificationsShowingDuration = 3900;

        /// <summary>
        /// Defines the StoryboardDefaultDuration
        /// </summary>
        public const int StoryboardDefaultDuration = 200;

        #endregion
    }

    /// <summary>
    /// Defines the <see cref="ValueTransition{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueTransition<T>
        where T : struct
    {
        #region Fields

        /// <summary>
        /// Defines the _currentValue
        /// </summary>
        private T _currentValue;

        /// <summary>
        /// Defines the _durationSeconds
        /// </summary>
        private float _durationSeconds;

        /// <summary>
        /// Defines the _interpolator
        /// </summary>
        private Func<T, T, float, T> _interpolator;

        /// <summary>
        /// Defines the _isTransitioning
        /// </summary>
        private bool _isTransitioning;

        /// <summary>
        /// Defines the _progress
        /// </summary>
        private float _progress;

        /// <summary>
        /// Defines the _startValue
        /// </summary>
        private T _startValue;

        /// <summary>
        /// Defines the _targetValue
        /// </summary>
        private T _targetValue;

        private EasingType? _easingType;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTransition{T}"/> class.
        /// </summary>
        /// <param name="initialValue">The initialValue<see cref="T"/></param>
        /// <param name="durationSeconds">The durationSeconds<see cref="float"/></param>
        /// <param name="interpolator">The interpolator<see cref="Func{T, T, float, T}"/></param>
        public ValueTransition(
            T initialValue,
            float durationSeconds,
            Func<T, T, float, T>? interpolator = null,
            EasingType? easingType = null
        )
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

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether IsTransitioning
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Gets the Value
        /// </summary>
        public T Value => _currentValue;

        #endregion

        #region Methods

        private Func<T, T, float, T> GetInterpolatorByEasingType(EasingType type)
        {
            // 这里只以float为例，实际可根据T类型扩展
            if (typeof(T) == typeof(float))
            {
                return (start, end, progress) =>
                {
                    float s = (float)(object)start;
                    float e = (float)(object)end;
                    float t = progress;
                    switch (type)
                    {
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
                        case EasingType.SmootherStep:
                            t = EasingHelper.SmootherStep(t);
                            break;
                        default:
                            break;
                    }
                    return (T)(object)(s + (e - s) * t);
                };
            }
            throw new NotSupportedException("当前类型未实现默认缓动插值");
        }

        /// <summary>
        /// The Reset
        /// </summary>
        /// <param name="value">The value<see cref="T"/></param>
        public void Reset(T value)
        {
            _currentValue = value;
            _startValue = value;
            _targetValue = value;
            _progress = 0f;
            _isTransitioning = false;
        }

        /// <summary>
        /// The StartTransition
        /// </summary>
        /// <param name="targetValue">The targetValue<see cref="T"/></param>
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

        /// <summary>
        /// 立即跳转到指定值，无动画
        /// </summary>
        /// <param name="value">目标值</param>
        public void JumpTo(T value)
        {
            _currentValue = value;
            _startValue = value;
            _targetValue = value;
            _progress = 1f;
            _isTransitioning = false;
        }

        /// <summary>
        /// The Update
        /// </summary>
        /// <param name="elapsedTime">The elapsedTime<see cref="TimeSpan"/></param>
        public void Update(TimeSpan elapsedTime)
        {
            if (!_isTransitioning)
                return;

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

        #endregion
    }
}
