using System;
using System.Collections.Generic;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Fluent API for chaining timer actions.
    /// Use Timer.Chain() to start a chain.
    /// </summary>
    public class TimerChain
    {
        private readonly List<ChainStep> _steps = new List<ChainStep>();
        private int _currentStep = 0;
        private TimerHandle _currentHandle;
        private bool _isRunning;
        private bool _isPaused;

        /// <summary>
        /// Creates a new timer chain.
        /// </summary>
        public static TimerChain Create() => new TimerChain();

        /// <summary>
        /// Adds a delay before the next action.
        /// </summary>
        /// <param name="delay">Delay in seconds.</param>
        public TimerChain Delay(float delay)
        {
            _steps.Add(new ChainStep { Type = StepType.Delay, Duration = delay });
            return this;
        }

        /// <summary>
        /// Adds an action to execute.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        public TimerChain Then(Action action)
        {
            _steps.Add(new ChainStep { Type = StepType.Action, Callback = action });
            return this;
        }

        /// <summary>
        /// Adds a delay then an action.
        /// </summary>
        public TimerChain ThenDelay(float delay, Action action)
        {
            Delay(delay);
            Then(action);
            return this;
        }

        /// <summary>
        /// Adds a loop that repeats N times.
        /// </summary>
        /// <param name="count">Number of repetitions.</param>
        /// <param name="interval">Interval between repetitions.</param>
        /// <param name="action">Action to execute each repetition.</param>
        public TimerChain Loop(int count, float interval, Action<int> action)
        {
            for (int i = 0; i < count; i++)
            {
                int index = i;
                _steps.Add(new ChainStep { Type = StepType.Delay, Duration = interval });
                _steps.Add(new ChainStep { Type = StepType.Action, Callback = () => action(index) });
            }
            return this;
        }

        /// <summary>
        /// Starts the timer chain.
        /// </summary>
        public TimerChain Start()
        {
            if (_isRunning || _steps.Count == 0) return this;
            _isRunning = true;
            _currentStep = 0;
            ExecuteNextStep();
            return this;
        }

        /// <summary>
        /// Pauses the chain.
        /// </summary>
        public void Pause()
        {
            if (!_isRunning) return;
            _isPaused = true;
            if (_currentHandle.IsValid)
            {
                Timer.Pause(_currentHandle);
            }
        }

        /// <summary>
        /// Resumes the chain.
        /// </summary>
        public void Resume()
        {
            if (!_isRunning || !_isPaused) return;
            _isPaused = false;
            if (_currentHandle.IsValid)
            {
                Timer.Resume(_currentHandle);
            }
        }

        /// <summary>
        /// Cancels the chain.
        /// </summary>
        public void Cancel()
        {
            if (_currentHandle.IsValid)
            {
                Timer.Cancel(_currentHandle);
            }
            _isRunning = false;
            _isPaused = false;
        }

        private void ExecuteNextStep()
        {
            if (_currentStep >= _steps.Count)
            {
                _isRunning = false;
                return;
            }

            var step = _steps[_currentStep];
            _currentStep++;

            switch (step.Type)
            {
                case StepType.Delay:
                    _currentHandle = Timer.Delay(step.Duration, ExecuteNextStep);
                    break;
                case StepType.Action:
                    try { step.Callback?.Invoke(); }
                    catch (Exception e) { UnityEngine.Debug.LogException(e); }
                    ExecuteNextStep();
                    break;
            }
        }

        private enum StepType { Delay, Action }

        private struct ChainStep
        {
            public StepType Type;
            public float Duration;
            public Action Callback;
        }
    }

    // Extension for Timer class
    public static partial class Timer
    {
        /// <summary>
        /// Creates a new timer chain.
        /// </summary>
        public static TimerChain Chain() => TimerChain.Create();
    }
}
