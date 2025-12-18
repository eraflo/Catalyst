using System;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// A timer that repeats a specified number of times or indefinitely.
    /// Fires OnTick each time the interval completes.
    /// </summary>
    public class RepeatingTimer : Timer
    {
        private readonly float _interval;
        private int _repeatCount;
        private int _currentRepeat;
        private readonly bool _infinite;

        /// <summary>
        /// Fired each time the timer completes an interval.
        /// </summary>
        public event Action OnTick;

        /// <summary>
        /// Fired when all repeats are complete (not fired for infinite timers).
        /// </summary>
        public event Action OnComplete;

        /// <summary>
        /// The interval between ticks in seconds.
        /// </summary>
        public float Interval => _interval;

        /// <summary>
        /// The total number of repeats (0 = infinite).
        /// </summary>
        public int RepeatCount => _repeatCount;

        /// <summary>
        /// The current repeat number (1-based).
        /// </summary>
        public int CurrentRepeat => _currentRepeat;

        /// <summary>
        /// How many repeats are remaining (0 for infinite timers).
        /// </summary>
        public int RemainingRepeats => _infinite ? int.MaxValue : _repeatCount - _currentRepeat;

        /// <summary>
        /// Whether this timer repeats indefinitely.
        /// </summary>
        public bool IsInfinite => _infinite;

        /// <summary>
        /// Returns true when all repeats are complete (never true for infinite timers).
        /// </summary>
        public override bool IsFinished => !_infinite && _currentRepeat >= _repeatCount;

        /// <summary>
        /// Creates a repeating timer.
        /// </summary>
        /// <param name="interval">Time in seconds between each tick.</param>
        /// <param name="repeatCount">Number of times to repeat. Use 0 for infinite.</param>
        public RepeatingTimer(float interval, int repeatCount = 0) : base(interval)
        {
            _interval = interval;
            _repeatCount = repeatCount;
            _infinite = repeatCount <= 0;
            _currentRepeat = 0;
        }

        /// <summary>
        /// Updates the timer and fires OnTick when interval completes.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            if (IsFinished) return;

            CurrentTime -= deltaTime;

            while (CurrentTime <= 0f && !IsFinished)
            {
                _currentRepeat++;
                
                try
                {
                    OnTick?.Invoke();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }

                if (IsFinished)
                {
                    try
                    {
                        OnComplete?.Invoke();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
                else
                {
                    // Reset for next interval (carry over remaining time)
                    CurrentTime += _interval;
                }
            }
        }

        /// <summary>
        /// Resets the timer to start from the beginning.
        /// </summary>
        public override void Reset()
        {
            CurrentTime = _interval;
            _currentRepeat = 0;
        }

        /// <summary>
        /// Resets with a new interval.
        /// </summary>
        /// <param name="newInterval">New interval in seconds.</param>
        public override void Reset(float newInterval)
        {
            initialTime = newInterval;
            CurrentTime = newInterval;
            _currentRepeat = 0;
        }

        /// <summary>
        /// Changes the number of repeats.
        /// </summary>
        /// <param name="count">New repeat count (0 = infinite).</param>
        public void SetRepeatCount(int count)
        {
            _repeatCount = count;
        }
    }
}
