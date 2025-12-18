namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Backend interface for timer implementations.
    /// Allows switching between Standard (class) and Burst (Jobs) implementations.
    /// </summary>
    public interface ITimerBackend
    {
        /// <summary>
        /// Creates a timer of the specified type.
        /// </summary>
        /// <typeparam name="T">Timer type implementing ITimer.</typeparam>
        /// <param name="config">Timer configuration.</param>
        /// <returns>Handle to the created timer.</returns>
        TimerHandle Create<T>(TimerConfig config) where T : struct, ITimer;

        /// <summary>
        /// Gets the current time of a timer.
        /// </summary>
        float GetCurrentTime(TimerHandle handle);

        /// <summary>
        /// Gets the progress (0-1) of a timer.
        /// </summary>
        float GetProgress(TimerHandle handle);

        /// <summary>
        /// Checks if a timer has finished.
        /// </summary>
        bool IsFinished(TimerHandle handle);

        /// <summary>
        /// Checks if a timer is running.
        /// </summary>
        bool IsRunning(TimerHandle handle);

        /// <summary>
        /// Pauses a timer.
        /// </summary>
        void Pause(TimerHandle handle);

        /// <summary>
        /// Resumes a paused timer.
        /// </summary>
        void Resume(TimerHandle handle);

        /// <summary>
        /// Cancels and removes a timer.
        /// </summary>
        void Cancel(TimerHandle handle);

        /// <summary>
        /// Resets a timer to its initial state.
        /// </summary>
        void Reset(TimerHandle handle);

        /// <summary>
        /// Sets the time scale of a timer.
        /// </summary>
        void SetTimeScale(TimerHandle handle, float scale);

        /// <summary>
        /// Updates all timers. Called by the system each frame.
        /// </summary>
        void Update(float deltaTime, float unscaledDeltaTime);

        /// <summary>
        /// Clears all timers.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the number of active timers.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Disposes the backend and releases resources.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Gets all active timer handles for debugging.
        /// </summary>
        System.Collections.Generic.List<TimerDebugInfo> GetActiveTimers();
    }

    /// <summary>
    /// Debug info for a timer.
    /// </summary>
    public struct TimerDebugInfo
    {
        public uint Id;
        public string TypeName;
        public float CurrentTime;
        public float InitialTime;
        public float Progress;
        public bool IsRunning;
        public bool IsFinished;
        public float TimeScale;
    }
}
