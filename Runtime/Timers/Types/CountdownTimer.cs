namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Countdown timer - counts down from duration to 0.
    /// </summary>
    public struct CountdownTimer : ITimer, ISupportsStandardCallbacks
    {
        private float _currentTime;
        private float _initialTime;
        private float _timeScale;
        private bool _isRunning;
        private bool _isFinished;
        private bool _useUnscaledTime;
        private bool _wasFinishedLastFrame;

        public float CurrentTime { get => _currentTime; set { _initialTime = _initialTime == 0 ? value : _initialTime; _currentTime = value; } }
        public float InitialTime => _initialTime;
        public bool IsRunning { get => _isRunning; set => _isRunning = value; }
        public bool IsFinished { get => _isFinished; set => _isFinished = value; }
        public bool UseUnscaledTime => _useUnscaledTime;
        public float TimeScale { get => _timeScale; set => _timeScale = value; }

        /// <summary>Progress from 1 (start) to 0 (finished).</summary>
        public float Progress => _initialTime > 0 ? _currentTime / _initialTime : 0f;

        public void Tick(float deltaTime)
        {
            _wasFinishedLastFrame = _isFinished;
            _currentTime -= deltaTime;
            if (_currentTime <= 0f)
            {
                _currentTime = 0f;
                _isFinished = true;
            }
        }

        public void Reset()
        {
            _currentTime = _initialTime;
            _isFinished = false;
            _wasFinishedLastFrame = false;
            _isRunning = true;
        }

        public void CollectCallbacks(ICallbackCollector collector)
        {
            if (_isFinished && !_wasFinishedLastFrame)
            {
                collector.Trigger<OnComplete>();
            }
        }
    }
}
