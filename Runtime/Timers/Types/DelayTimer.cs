using System;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// A simple timer that executes an action after a delay and auto-disposes.
    /// Use via TimerManager.Delay() for one-liner delayed actions.
    /// </summary>
    public class DelayTimer : Timer
    {
        private readonly Action _onComplete;
        private bool _hasCompleted;

        /// <summary>
        /// Creates a new delay timer.
        /// </summary>
        /// <param name="delay">Delay in seconds before executing the action.</param>
        /// <param name="onComplete">Action to execute when the delay completes.</param>
        /// <param name="useUnscaledTime">If true, ignores Time.timeScale.</param>
        public DelayTimer(float delay, Action onComplete, bool useUnscaledTime = false) 
            : base(delay)
        {
            _onComplete = onComplete;
            UseUnscaledTime = useUnscaledTime;
            
            // Auto-dispose and execute callback when timer stops
            OnTimerStop += HandleComplete;
        }

        /// <summary>
        /// Returns true when the delay has elapsed.
        /// </summary>
        public override bool IsFinished => CurrentTime <= 0f;

        /// <summary>
        /// Decrements the remaining time.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            if (IsFinished) return;
            
            CurrentTime -= deltaTime;
            
            if (CurrentTime < 0f)
                CurrentTime = 0f;
        }

        private void HandleComplete()
        {
            if (_hasCompleted) return;
            _hasCompleted = true;
            
            try
            {
                _onComplete?.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                // Auto-dispose after completion
                Dispose();
            }
        }

        /// <summary>
        /// Cancels the delay without executing the callback.
        /// </summary>
        public void Cancel()
        {
            _hasCompleted = true; // Prevent callback execution
            Dispose();
        }
    }
}
