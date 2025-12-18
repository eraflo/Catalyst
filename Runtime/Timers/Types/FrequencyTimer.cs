using System;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// A timer that fires a tick event at a specified frequency (N times per second).
    /// </summary>
    public class FrequencyTimer : Timer
    {
        private float _tickInterval;
        private float _timeSinceLastTick;

        /// <summary>
        /// The number of ticks per second.
        /// </summary>
        public int TicksPerSecond { get; private set; }

        /// <summary>
        /// The total number of ticks that have occurred since the timer started.
        /// </summary>
        public int TickCount { get; private set; }

        /// <summary>
        /// Fired each time the timer ticks at the specified frequency.
        /// </summary>
        public event Action OnTick;

        /// <summary>
        /// Creates a new frequency timer that ticks at the specified rate.
        /// </summary>
        /// <param name="ticksPerSecond">How many times per second to tick.</param>
        public FrequencyTimer(int ticksPerSecond) : base(0f)
        {
            SetTicksPerSecond(ticksPerSecond);
        }

        /// <summary>
        /// Frequency timers never finish automatically.
        /// </summary>
        public override bool IsFinished => false;

        /// <summary>
        /// Changes the tick frequency.
        /// </summary>
        /// <param name="ticksPerSecond">New ticks per second value.</param>
        public void SetTicksPerSecond(int ticksPerSecond)
        {
            TicksPerSecond = ticksPerSecond > 0 ? ticksPerSecond : 1;
            _tickInterval = 1f / TicksPerSecond;
        }

        /// <summary>
        /// Accumulates time and fires OnTick at the specified frequency.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            CurrentTime += deltaTime;
            _timeSinceLastTick += deltaTime;

            // Fire ticks for accumulated time
            while (_timeSinceLastTick >= _tickInterval)
            {
                _timeSinceLastTick -= _tickInterval;
                TickCount++;
                OnTick?.Invoke();
            }
        }

        /// <summary>
        /// Resets the timer and tick count.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            _timeSinceLastTick = 0f;
            TickCount = 0;
        }
    }
}
