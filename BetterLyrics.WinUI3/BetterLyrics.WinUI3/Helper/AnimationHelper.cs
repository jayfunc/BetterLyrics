// 2025/6/23 by Zhe Fang

using System;

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
            Func<T, T, float, T> interpolator
        )
        {
            _currentValue = initialValue;
            _startValue = initialValue;
            _targetValue = initialValue;
            _durationSeconds = durationSeconds;
            _progress = 1f;
            _isTransitioning = false;
            _interpolator = interpolator;
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
