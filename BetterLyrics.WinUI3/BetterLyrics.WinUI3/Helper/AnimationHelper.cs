using System;

namespace BetterLyrics.WinUI3.Helper
{
    public class AnimationHelper
    {
        public const int StackedNotificationsShowingDuration = 3900;
        public const int StoryboardDefaultDuration = 200;
        public const int DebounceDefaultDuration = 200;
    }

    public class ValueTransition<T>
        where T : struct
    {
        private T _currentValue;
        private T _startValue;
        private T _targetValue;
        private float _progress;
        private float _durationSeconds;
        private bool _isTransitioning;
        private Func<T, T, float, T> _interpolator;

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

        public T Value => _currentValue;
        public bool IsTransitioning => _isTransitioning;

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

        public void Reset(T value)
        {
            _currentValue = value;
            _startValue = value;
            _targetValue = value;
            _progress = 0f;
            _isTransitioning = false;
        }
    }
}
