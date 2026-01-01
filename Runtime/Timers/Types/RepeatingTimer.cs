namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Repeating timer - fires OnRepeat every interval, for a count or infinitely.
    /// </summary>
    public struct RepeatingTimer : ITimer, ISupportsRepeatingCallbacks
    {
        private float _currentTime;
        private float _interval;
        private float _timeScale;
        private int _repeatCount;
        private int _currentRepeat;
        private int _lastReportedRepeat;
        private bool _isRunning;
        private bool _isFinished;
        private bool _useUnscaledTime;
        private bool _isInfinite;
        private bool _wasFinishedLastFrame;

        public float CurrentTime { get => _currentTime; set { _interval = _interval == 0 ? value : _interval; _currentTime = value; } }
        public float InitialTime => _interval;
        public bool IsRunning { get => _isRunning; set => _isRunning = value; }
        public bool IsFinished { get => _isFinished; set => _isFinished = value; }
        public bool UseUnscaledTime => _useUnscaledTime;
        public float TimeScale { get => _timeScale; set => _timeScale = value; }

        /// <summary>Current repeat number (1-based).</summary>
        public int CurrentRepeat => _currentRepeat;

        /// <summary>Total repeat count (0 = infinite).</summary>
        public int RepeatCount { get => _repeatCount; set { _repeatCount = value; _isInfinite = value <= 0; } }

        public void Tick(float deltaTime)
        {
            _wasFinishedLastFrame = _isFinished;
            _currentTime -= deltaTime;
            
            if (_currentTime <= 0f)
            {
                _currentRepeat++;
                
                if (!_isInfinite && _currentRepeat >= _repeatCount)
                {
                    _isFinished = true;
                }
                else
                {
                    _currentTime += _interval;
                }
            }
        }

        public void Reset()
        {
            _currentTime = _interval;
            _currentRepeat = 0;
            _lastReportedRepeat = 0;
            _isFinished = false;
            _wasFinishedLastFrame = false;
            _isRunning = true;
        }

        public void CollectCallbacks(ICallbackCollector collector)
        {
            // Fire OnRepeat for each new repeat (with repeat count as int parameter)
            if (_currentRepeat > _lastReportedRepeat)
            {
                collector.Trigger<OnRepeat, int>(_currentRepeat);
                _lastReportedRepeat = _currentRepeat;
            }

            // Fire OnComplete when finished
            if (_isFinished && !_wasFinishedLastFrame)
            {
                collector.Trigger<OnComplete>();
            }
        }
    }
}
