namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Frequency timer - ticks N times per second.
    /// </summary>
    public struct FrequencyTimer : ITimer, ISupportsIndefiniteCallbacks
    {
        private float _accumulator;
        private float _tickInterval;
        private float _timeScale;
        private bool _isRunning;
        private bool _isFinished;
        private bool _useUnscaledTime;
        private int _ticksThisFrame;

        public float CurrentTime { get => _accumulator; set { if (_tickInterval == 0f && value > 0f) _tickInterval = 1f / value; _accumulator = 0f; } }
        public float InitialTime => _tickInterval;
        public bool IsRunning { get => _isRunning; set => _isRunning = value; }
        public bool IsFinished { get => _isFinished; set => _isFinished = value; }
        public bool UseUnscaledTime => _useUnscaledTime;
        public float TimeScale { get => _timeScale; set => _timeScale = value; }

        /// <summary>Number of ticks that occurred this frame.</summary>
        public int TicksThisFrame => _ticksThisFrame;

        /// <summary>Ticks per second.</summary>
        public float TicksPerSecond => _tickInterval > 0 ? 1f / _tickInterval : 0f;

        public void Tick(float deltaTime)
        {
            _ticksThisFrame = 0;
            _accumulator += deltaTime;
            
            while (_accumulator >= _tickInterval && _tickInterval > 0)
            {
                _accumulator -= _tickInterval;
                _ticksThisFrame++;
            }
        }

        public void Reset()
        {
            _accumulator = 0f;
            _ticksThisFrame = 0;
            _isRunning = true;
        }

        public void CollectCallbacks(ICallbackCollector collector)
        {
            // Fire OnTick for each frequency tick (with interval as float parameter)
            for (int i = 0; i < _ticksThisFrame; i++)
            {
                collector.Trigger<OnTick, float>(_tickInterval);
            }
        }
    }
}
