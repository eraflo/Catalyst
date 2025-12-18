using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Thread safety mode for the TimerManager.
    /// </summary>
    public enum TimerThreadMode
    {
        /// <summary>
        /// Fast mode - optimized for single-threaded main thread access only.
        /// </summary>
        SingleThread,
        
        /// <summary>
        /// Thread-safe mode - allows timer operations from any thread.
        /// </summary>
        ThreadSafe
    }

    /// <summary>
    /// Central manager for all timers. Uses PackageRuntime.IsThreadSafe for thread mode.
    /// </summary>
    public static class TimerManager
    {
        // Single-thread mode collections (faster)
        private static readonly List<Timer> _timers = new List<Timer>();
        private static readonly List<Timer> _timersToAdd = new List<Timer>();
        private static readonly List<Timer> _timersToRemove = new List<Timer>();
        
        // Thread-safe mode collections
        private static readonly ConcurrentQueue<Timer> _pendingAdditions = new ConcurrentQueue<Timer>();
        private static readonly ConcurrentQueue<Timer> _pendingRemovals = new ConcurrentQueue<Timer>();
        private static readonly object _lockObject = new object();
        
        private static bool _isUpdating;

        /// <summary>
        /// The current thread safety mode.
        /// Delegates to PackageRuntime.ThreadMode.
        /// </summary>
        public static TimerThreadMode ThreadMode
        {
            get => (TimerThreadMode)(int)PackageRuntime.ThreadMode;
            set => PackageRuntime.ThreadMode = (PackageThreadMode)(int)value;
        }

        /// <summary>
        /// Whether thread-safe mode is enabled.
        /// </summary>
        public static bool IsThreadSafe => PackageRuntime.IsThreadSafe;

        /// <summary>
        /// Gets a snapshot of all currently registered timers. 
        /// Useful for debugging or custom tools.
        /// Thread-safe in ThreadSafe mode.
        /// </summary>
        public static List<Timer> GetAllTimers()
        {
            if (IsThreadSafe)
            {
                lock (_lockObject)
                {
                    return new List<Timer>(_timers);
                }
            }
            return new List<Timer>(_timers);
        }

        /// <summary>
        /// The number of currently registered timers.
        /// </summary>
        public static int TimerCount
        {
            get
            {
                if (IsThreadSafe)
                {
                    lock (_lockObject)
                    {
                        return _timers.Count;
                    }
                }
                return _timers.Count;
            }
        }

        /// <summary>
        /// The ID of the main Unity thread (set during initialization).
        /// </summary>
        internal static int MainThreadId { get; set; } = -1;

        /// <summary>
        /// Returns true if currently executing on the main Unity thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        /// <summary>
        /// Registers a timer to be updated each frame.
        /// Thread-safe if ThreadMode is set to ThreadSafe.
        /// </summary>
        /// <param name="timer">The timer to register.</param>
        public static void RegisterTimer(Timer timer)
        {
            if (timer == null) return;

            if (IsThreadSafe)
            {
                RegisterTimerThreadSafe(timer);
            }
            else
            {
                RegisterTimerSingleThread(timer);
            }
        }

        private static void RegisterTimerSingleThread(Timer timer)
        {
            if (_isUpdating)
            {
                if (!_timersToAdd.Contains(timer))
                    _timersToAdd.Add(timer);
            }
            else
            {
                if (!_timers.Contains(timer))
                    _timers.Add(timer);
            }
        }

        private static void RegisterTimerThreadSafe(Timer timer)
        {
            if (IsMainThread && !_isUpdating)
            {
                lock (_lockObject)
                {
                    if (!_timers.Contains(timer))
                        _timers.Add(timer);
                }
            }
            else
            {
                _pendingAdditions.Enqueue(timer);
            }
        }

        /// <summary>
        /// Unregisters a timer so it is no longer updated.
        /// Thread-safe if ThreadMode is set to ThreadSafe.
        /// </summary>
        /// <param name="timer">The timer to unregister.</param>
        public static void UnregisterTimer(Timer timer)
        {
            if (timer == null) return;

            if (IsThreadSafe)
            {
                UnregisterTimerThreadSafe(timer);
            }
            else
            {
                UnregisterTimerSingleThread(timer);
            }
        }

        private static void UnregisterTimerSingleThread(Timer timer)
        {
            if (_isUpdating)
            {
                if (!_timersToRemove.Contains(timer))
                    _timersToRemove.Add(timer);
            }
            else
            {
                _timers.Remove(timer);
            }
        }

        private static void UnregisterTimerThreadSafe(Timer timer)
        {
            if (IsMainThread && !_isUpdating)
            {
                lock (_lockObject)
                {
                    _timers.Remove(timer);
                }
            }
            else
            {
                _pendingRemovals.Enqueue(timer);
            }
        }

        /// <summary>
        /// Clears all registered timers.
        /// </summary>
        public static void Clear()
        {
            if (IsThreadSafe)
            {
                lock (_lockObject)
                {
                    _timers.Clear();
                }
                
                // Drain the queues
                while (_pendingAdditions.TryDequeue(out _)) { }
                while (_pendingRemovals.TryDequeue(out _)) { }
            }
            else
            {
                _timers.Clear();
                _timersToAdd.Clear();
                _timersToRemove.Clear();
            }
        }

        #region Delay Helpers

        /// <summary>
        /// Executes an action after a specified delay.
        /// The timer auto-disposes after completion.
        /// </summary>
        /// <param name="delay">Delay in seconds.</param>
        /// <param name="onComplete">Action to execute after the delay.</param>
        /// <param name="useUnscaledTime">If true, ignores Time.timeScale.</param>
        /// <returns>The timer instance (can be used to cancel).</returns>
        public static DelayTimer Delay(float delay, Action onComplete, bool useUnscaledTime = false)
        {
            var timer = new DelayTimer(delay, onComplete, useUnscaledTime);
            timer.Start();
            return timer;
        }

        /// <summary>
        /// Cancels a delay timer if it's still running.
        /// </summary>
        /// <param name="timer">The timer to cancel.</param>
        public static void CancelDelay(DelayTimer timer)
        {
            timer?.Dispose();
        }

        #endregion

        /// <summary>
        /// Updates all registered timers. Called automatically by the Player Loop.
        /// </summary>
        internal static void UpdateTimers()
        {
            if (IsThreadSafe)
            {
                UpdateTimersThreadSafe();
            }
            else
            {
                UpdateTimersSingleThread();
            }
        }

        private static void UpdateTimersSingleThread()
        {
            if (_timers.Count == 0 && _timersToAdd.Count == 0) return;

            _isUpdating = true;

            foreach (var timer in _timers)
            {
                if (timer == null) continue;
                
                if (timer.IsRunning)
                {
                    float deltaTime = timer.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    deltaTime *= timer.TimeScale;
                    timer.Tick(deltaTime);
                    
                    if (timer.IsFinished)
                    {
                        timer.Stop();
                    }
                }
            }

            _isUpdating = false;

            // Process pending additions
            if (_timersToAdd.Count > 0)
            {
                foreach (var timer in _timersToAdd)
                {
                    if (!_timers.Contains(timer))
                        _timers.Add(timer);
                }
                _timersToAdd.Clear();
            }

            // Process pending removals
            if (_timersToRemove.Count > 0)
            {
                foreach (var timer in _timersToRemove)
                {
                    _timers.Remove(timer);
                }
                _timersToRemove.Clear();
            }
        }

        private static void UpdateTimersThreadSafe()
        {
            // Process pending additions from other threads
            while (_pendingAdditions.TryDequeue(out var timerToAdd))
            {
                lock (_lockObject)
                {
                    if (!_timers.Contains(timerToAdd))
                        _timers.Add(timerToAdd);
                }
            }

            // Process pending removals from other threads
            while (_pendingRemovals.TryDequeue(out var timerToRemove))
            {
                lock (_lockObject)
                {
                    _timers.Remove(timerToRemove);
                }
            }

            List<Timer> snapshot;
            lock (_lockObject)
            {
                if (_timers.Count == 0) return;
                snapshot = new List<Timer>(_timers);
            }

            _isUpdating = true;

            foreach (var timer in snapshot)
            {
                if (timer == null) continue;
                
                if (timer.IsRunning)
                {
                    float deltaTime = timer.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    deltaTime *= timer.TimeScale;
                    timer.Tick(deltaTime);
                    
                    if (timer.IsFinished)
                    {
                        timer.Stop();
                    }
                }
            }

            _isUpdating = false;
        }
    }
}
