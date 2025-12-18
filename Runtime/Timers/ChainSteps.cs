using System;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Interface for chain steps. Implement this to create custom chain steps.
    /// </summary>
    public interface IChainStep
    {
        /// <summary>
        /// Estimated duration of this step (for progress calculation).
        /// </summary>
        float Duration { get; }

        /// <summary>
        /// Executes the step and calls onComplete when done.
        /// </summary>
        void Execute(Action onComplete);

        /// <summary>
        /// Pauses the step.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes the step.
        /// </summary>
        void Resume();

        /// <summary>
        /// Disposes the step.
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Executes an action immediately.
    /// </summary>
    public class ActionStep : IChainStep
    {
        private readonly Action _action;

        public float Duration => 0f;

        public ActionStep(Action action) => _action = action;

        public void Execute(Action onComplete)
        {
            try { _action?.Invoke(); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
            onComplete?.Invoke();
        }

        public void Pause() { }
        public void Resume() { }
        public void Dispose() { }
    }

    /// <summary>
    /// Waits for a timer to complete.
    /// </summary>
    public class TimerStep : IChainStep
    {
        private readonly Timer _timer;
        private readonly float _duration;
        private Action _onComplete;

        public float Duration => _duration;

        public TimerStep(Timer timer, float duration = 0f)
        {
            _timer = timer;
            _duration = duration > 0 ? duration : timer.CurrentTime;
            
            // Don't start yet
            _timer.Pause();
            TimerManager.UnregisterTimer(_timer);
        }

        public void Execute(Action onComplete)
        {
            _onComplete = onComplete;
            TimerManager.RegisterTimer(_timer);
            _timer.OnTimerStop += OnTimerComplete;
            _timer.Start();
        }

        private void OnTimerComplete()
        {
            _timer.OnTimerStop -= OnTimerComplete;
            _onComplete?.Invoke();
        }

        public void Pause() => _timer?.Pause();
        public void Resume() => _timer?.Resume();
        public void Dispose() => _timer?.Dispose();
    }

    /// <summary>
    /// Simple delay step using CountdownTimer.
    /// </summary>
    public class DelayStep : TimerStep
    {
        public DelayStep(float delay) : base(new CountdownTimer(delay), delay) { }
    }

    /// <summary>
    /// Repeating timer step.
    /// </summary>
    public class RepeatStep : IChainStep
    {
        private readonly RepeatingTimer _timer;
        private readonly float _duration;
        private Action _onComplete;

        public float Duration => _duration;

        public RepeatStep(float interval, int repeatCount, Action onTick)
        {
            _timer = new RepeatingTimer(interval, repeatCount);
            _timer.OnTick += onTick;
            _duration = interval * repeatCount;
            
            _timer.Pause();
            TimerManager.UnregisterTimer(_timer);
        }

        public void Execute(Action onComplete)
        {
            _onComplete = onComplete;
            TimerManager.RegisterTimer(_timer);
            _timer.OnComplete += OnComplete;
            _timer.Start();
        }

        private void OnComplete()
        {
            _timer.OnComplete -= OnComplete;
            _onComplete?.Invoke();
        }

        public void Pause() => _timer?.Pause();
        public void Resume() => _timer?.Resume();
        public void Dispose() => _timer?.Dispose();
    }

    /// <summary>
    /// Waits until a condition is true.
    /// </summary>
    public class WaitUntilStep : IChainStep
    {
        private readonly Func<bool> _condition;
        private FrequencyTimer _checkTimer;
        private Action _onComplete;

        public float Duration => 0f; // Unknown duration

        public WaitUntilStep(Func<bool> condition) => _condition = condition;

        public void Execute(Action onComplete)
        {
            _onComplete = onComplete;

            // Check immediately
            if (_condition())
            {
                onComplete?.Invoke();
                return;
            }

            // Check periodically
            _checkTimer = new FrequencyTimer(30);
            _checkTimer.OnTick += CheckCondition;
            _checkTimer.Start();
        }

        private void CheckCondition()
        {
            if (_condition())
            {
                _checkTimer.Dispose();
                _checkTimer = null;
                _onComplete?.Invoke();
            }
        }

        public void Pause() => _checkTimer?.Pause();
        public void Resume() => _checkTimer?.Resume();
        public void Dispose() => _checkTimer?.Dispose();
    }

    /// <summary>
    /// Waits while a condition is true.
    /// </summary>
    public class WaitWhileStep : WaitUntilStep
    {
        public WaitWhileStep(Func<bool> condition) : base(() => !condition()) { }
    }

    /// <summary>
    /// Executes multiple steps in parallel, completes when all are done.
    /// </summary>
    public class ParallelStep : IChainStep
    {
        private readonly IChainStep[] _steps;
        private int _completedCount;
        private Action _onComplete;

        public float Duration { get; }

        public ParallelStep(params IChainStep[] steps)
        {
            _steps = steps;
            
            // Duration is the max of all steps
            float maxDuration = 0f;
            foreach (var step in steps)
                if (step.Duration > maxDuration)
                    maxDuration = step.Duration;
            Duration = maxDuration;
        }

        public void Execute(Action onComplete)
        {
            _onComplete = onComplete;
            _completedCount = 0;

            foreach (var step in _steps)
            {
                step.Execute(OnStepComplete);
            }
        }

        private void OnStepComplete()
        {
            _completedCount++;
            if (_completedCount >= _steps.Length)
            {
                _onComplete?.Invoke();
            }
        }

        public void Pause()
        {
            foreach (var step in _steps) step.Pause();
        }

        public void Resume()
        {
            foreach (var step in _steps) step.Resume();
        }

        public void Dispose()
        {
            foreach (var step in _steps) step.Dispose();
        }
    }
}
