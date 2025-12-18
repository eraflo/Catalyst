using System;
using System.Collections.Generic;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Chains multiple steps for sequential execution.
    /// Uses IChainStep interface for extensibility.
    /// </summary>
    public class TimerChain : IDisposable
    {
        private readonly Queue<IChainStep> _steps = new Queue<IChainStep>();
        private IChainStep _currentStep;
        private bool _isRunning;
        private bool _isDisposed;
        private Action _onComplete;
        private Action<float> _onProgress;
        private float _totalDuration;
        private float _elapsedDuration;

        public bool IsRunning => _isRunning;
        public float Progress => _totalDuration > 0 ? _elapsedDuration / _totalDuration : 0f;

        #region Static Constructors

        public static TimerChain Start(float delay) => new TimerChain().Then(delay);
        public static TimerChain Start(Action action) => new TimerChain().Then(action);
        public static TimerChain Start(Timer timer, float duration = 0f) => new TimerChain().Then(timer, duration);
        public static TimerChain Start(IChainStep step) => new TimerChain().Then(step);

        #endregion

        #region Fluent API - Add Steps

        public TimerChain Then(float delay) => Then(new DelayStep(delay));
        public TimerChain Then(Action action) => Then(new ActionStep(action));
        public TimerChain Then(Timer timer, float duration = 0f) => Then(new TimerStep(timer, duration));
        public TimerChain Then(float delay, Action action) => Then(delay).Then(action);

        public TimerChain ThenRepeat(float interval, int count, Action onTick) 
            => Then(new RepeatStep(interval, count, onTick));

        public TimerChain WaitUntil(Func<bool> condition) 
            => Then(new WaitUntilStep(condition));

        public TimerChain WaitWhile(Func<bool> condition) 
            => Then(new WaitWhileStep(condition));

        public TimerChain Parallel(params IChainStep[] steps) 
            => Then(new ParallelStep(steps));

        /// <summary>
        /// Adds any IChainStep to the chain.
        /// </summary>
        public TimerChain Then(IChainStep step)
        {
            _steps.Enqueue(step);
            _totalDuration += step.Duration;
            return this;
        }

        #endregion

        #region Callbacks

        public TimerChain OnComplete(Action callback)
        {
            _onComplete = callback;
            return this;
        }

        public TimerChain OnProgress(Action<float> callback)
        {
            _onProgress = callback;
            return this;
        }

        #endregion

        #region Control

        public TimerChain Run()
        {
            if (_isRunning || _isDisposed) return this;
            _isRunning = true;
            _elapsedDuration = 0f;
            ExecuteNextStep();
            return this;
        }

        public void Pause() => _currentStep?.Pause();
        public void Resume() => _currentStep?.Resume();

        public void Stop()
        {
            _isRunning = false;
            _currentStep?.Dispose();
            _currentStep = null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Stop();
            _steps.Clear();
            _onComplete = null;
            _onProgress = null;
        }

        #endregion

        #region Execution

        private void ExecuteNextStep()
        {
            if (_isDisposed || !_isRunning) return;

            if (_steps.Count == 0)
            {
                CompleteChain();
                return;
            }

            _currentStep = _steps.Dequeue();
            var stepDuration = _currentStep.Duration;
            var startElapsed = _elapsedDuration;

            _currentStep.Execute(() =>
            {
                _elapsedDuration = startElapsed + stepDuration;
                _onProgress?.Invoke(Progress);
                ExecuteNextStep();
            });
        }

        private void CompleteChain()
        {
            _isRunning = false;
            _currentStep = null;

            try { _onComplete?.Invoke(); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

        #endregion
    }
}
