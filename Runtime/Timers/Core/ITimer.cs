namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Base interface for all timer types.
    /// Implement this to create custom timer types that work with Timer.Create&lt;T&gt;().
    /// </summary>
    public interface ITimer
    {
        /// <summary>Current time value of the timer.</summary>
        float CurrentTime { get; set; }
        
        /// <summary>Initial time value (for progress calculation).</summary>
        float InitialTime { get; }
        
        /// <summary>Whether the timer is currently running.</summary>
        bool IsRunning { get; set; }
        
        /// <summary>Whether the timer has finished.</summary>
        bool IsFinished { get; set; }
        
        /// <summary>Whether to use unscaled time.</summary>
        bool UseUnscaledTime { get; }
        
        /// <summary>Time scale multiplier for this timer.</summary>
        float TimeScale { get; set; }
        
        /// <summary>Updates the timer. Called each frame by the system.</summary>
        /// <param name="deltaTime">Time elapsed since last tick.</param>
        void Tick(float deltaTime);
        
        /// <summary>Resets the timer to its initial state.</summary>
        void Reset();

        /// <summary>
        /// Collects callbacks to invoke this frame.
        /// Override this in custom timers to trigger custom callbacks.
        /// </summary>
        /// <param name="collector">Event collector to add callbacks to.</param>
        void CollectCallbacks(ICallbackCollector collector) { }
    }

    /// <summary>
    /// Interface for collecting callback events during a tick.
    /// Passed to ITimer.CollectCallbacks() for custom callback invocation.
    /// </summary>
    public interface ICallbackCollector
    {
        /// <summary>Triggers a callback with no parameters.</summary>
        void Trigger<TCallback>() where TCallback : struct, ITimerCallback;
        
        /// <summary>Triggers a callback with any parameter type.</summary>
        void Trigger<TCallback, TArg>(TArg value) where TCallback : struct, ITimerCallback;
    }

    /// <summary>
    /// Configuration for creating a timer.
    /// </summary>
    public struct TimerConfig
    {
        /// <summary>Duration or initial time value.</summary>
        public float Duration;
        
        /// <summary>Time scale multiplier (default 1).</summary>
        public float TimeScale;
        
        /// <summary>Whether to use unscaled time.</summary>
        public bool UseUnscaledTime;
        
        /// <summary>For repeating timers - number of repeats (0 = infinite).</summary>
        public int RepeatCount;
        
        /// <summary>For frequency timers - ticks per second.</summary>
        public float TicksPerSecond;

        /// <summary>Creates a simple duration config.</summary>
        public static TimerConfig FromDuration(float duration) => new TimerConfig { Duration = duration, TimeScale = 1f };
        
        /// <summary>Creates config with all options.</summary>
        public static TimerConfig Create(float duration, float timeScale = 1f, bool useUnscaledTime = false)
        {
            return new TimerConfig
            {
                Duration = duration,
                TimeScale = timeScale,
                UseUnscaledTime = useUnscaledTime
            };
        }
    }
}
