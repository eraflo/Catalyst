namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Stopwatch timer - counts up from 0 indefinitely.
    /// </summary>
    public struct StopwatchTimer : ITimer, ISupportsIndefiniteCallbacks
    {
        private float _currentTime;
        private float _timeScale;
        private bool _isRunning;
        private bool _isFinished;
        private bool _useUnscaledTime;

        public float CurrentTime { get => _currentTime; set => _currentTime = value; }
        public float InitialTime => 0f;
        public bool IsRunning { get => _isRunning; set => _isRunning = value; }
        public bool IsFinished { get => _isFinished; set => _isFinished = value; }
        public bool UseUnscaledTime => _useUnscaledTime;
        public float TimeScale { get => _timeScale; set => _timeScale = value; }

        public void Tick(float deltaTime)
        {
            _currentTime += deltaTime;
        }

        public void Reset()
        {
            _currentTime = 0f;
            _isRunning = true;
        }

        public void CollectCallbacks(ICallbackCollector collector)
        {
            // Stopwatch has no automatic callbacks - it runs indefinitely
        }
    }
}
