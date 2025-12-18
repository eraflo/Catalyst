using System;
using UnityEngine;
using Eraflo.UnityImportPackage.EasingSystem;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Abstract base class for all timers. Timers automatically register themselves
    /// with the TimerManager and are updated via the Player Loop system.
    /// </summary>
    public abstract class Timer : IDisposable
    {
        /// <summary>
        /// The initial time value used when the timer was created or last reset.
        /// </summary>
        protected float initialTime;
        
        /// <summary>
        /// The current time value of the timer.
        /// </summary>
        public float CurrentTime { get; protected set; }
        
        /// <summary>
        /// Whether the timer is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }
        
        /// <summary>
        /// Whether the timer has finished (implementation-specific).
        /// </summary>
        public abstract bool IsFinished { get; }
        
        /// <summary>
        /// Whether to use unscaled time (ignores Time.timeScale).
        /// </summary>
        public bool UseUnscaledTime { get; set; }
        
        /// <summary>
        /// Individual time scale multiplier for this timer.
        /// Default is 1.0. Values > 1 speed up, values < 1 slow down.
        /// Set to 0 to pause, negative values reverse the timer.
        /// </summary>
        public float TimeScale { get; set; } = 1f;
        
        /// <summary>
        /// Progress of the timer from 0 to 1 (based on initial time).
        /// For CountdownTimer, this goes from 1 to 0.
        /// </summary>
        public float Progress => initialTime > 0 ? Mathf.Clamp01(CurrentTime / initialTime) : 0f;

        /// <summary>
        /// Gets the completion progress (0 to 1) with the specified easing function applied.
        /// Useful for animating values based on the timer's progress.
        /// </summary>
        /// <param name="easing">The easing curve to apply.</param>
        /// <returns>A value between 0 and 1 (or outside for Elastic/Back).</returns>
        public float GetProgress(EasingType easing)
        {
            float t = 1f - Progress;
            return Easing.Evaluate(t, easing);
        }

        /// <summary>
        /// Fired when the timer starts.
        /// </summary>
        public event Action OnTimerStart;
        
        /// <summary>
        /// Fired when the timer stops (either completed or manually stopped).
        /// </summary>
        public event Action OnTimerStop;

        /// <summary>
        /// Creates a new timer with the specified initial time.
        /// </summary>
        /// <param name="initialTime">The starting time value for the timer.</param>
        protected Timer(float initialTime)
        {
            this.initialTime = initialTime;
            CurrentTime = initialTime;
            TimerManager.RegisterTimer(this);
        }

        /// <summary>
        /// Starts or resumes the timer.
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;
            
            IsRunning = true;
            OnTimerStart?.Invoke();
        }

        /// <summary>
        /// Stops the timer completely.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;
            
            IsRunning = false;
            OnTimerStop?.Invoke();
        }

        /// <summary>
        /// Pauses the timer (can be resumed with Start).
        /// </summary>
        public void Pause()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Resumes a paused timer.
        /// </summary>
        public void Resume() => Start();

        /// <summary>
        /// Resets the timer to its initial time.
        /// </summary>
        public virtual void Reset()
        {
            CurrentTime = initialTime;
        }

        /// <summary>
        /// Resets the timer with a new initial time.
        /// </summary>
        /// <param name="newTime">The new initial time value.</param>
        public virtual void Reset(float newTime)
        {
            initialTime = newTime;
            Reset();
        }

        /// <summary>
        /// Updates the timer. Called automatically by the TimerManager each frame.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last tick.</param>
        public abstract void Tick(float deltaTime);

        /// <summary>
        /// Disposes of the timer, unregistering it from the TimerManager.
        /// Always call Dispose when you're done with a timer to prevent memory leaks.
        /// </summary>
        public void Dispose()
        {
            TimerManager.UnregisterTimer(this);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Destructor to ensure timer is unregistered if Dispose wasn't called.
        /// </summary>
        ~Timer()
        {
            Dispose();
        }
    }
}
