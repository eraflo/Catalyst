using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Central manager for all timers. Handles registration, unregistration,
    /// and updating of all active timers. Updated via the Player Loop system.
    /// </summary>
    public static class TimerManager
    {
        private static readonly List<Timer> _timers = new List<Timer>();
        private static readonly List<Timer> _timersToAdd = new List<Timer>();
        private static readonly List<Timer> _timersToRemove = new List<Timer>();
        private static bool _isUpdating;

        /// <summary>
        /// The number of currently registered timers.
        /// </summary>
        public static int TimerCount => _timers.Count;

        /// <summary>
        /// Registers a timer to be updated each frame.
        /// </summary>
        /// <param name="timer">The timer to register.</param>
        public static void RegisterTimer(Timer timer)
        {
            if (timer == null) return;
            
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

        /// <summary>
        /// Unregisters a timer so it is no longer updated.
        /// </summary>
        /// <param name="timer">The timer to unregister.</param>
        public static void UnregisterTimer(Timer timer)
        {
            if (timer == null) return;
            
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

        /// <summary>
        /// Clears all registered timers.
        /// </summary>
        public static void Clear()
        {
            _timers.Clear();
            _timersToAdd.Clear();
            _timersToRemove.Clear();
        }

        /// <summary>
        /// Updates all registered timers. Called automatically by the Player Loop.
        /// </summary>
        internal static void UpdateTimers()
        {
            if (_timers.Count == 0 && _timersToAdd.Count == 0) return;

            _isUpdating = true;

            foreach (var timer in _timers)
            {
                if (timer == null) continue;
                
                if (timer.IsRunning)
                {
                    float deltaTime = timer.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    timer.Tick(deltaTime);
                    
                    // Auto-stop finished timers
                    if (timer.IsFinished)
                    {
                        timer.Stop();
                    }
                }
            }

            _isUpdating = false;

            // Process pending additions and removals
            if (_timersToAdd.Count > 0)
            {
                foreach (var timer in _timersToAdd)
                {
                    if (!_timers.Contains(timer))
                        _timers.Add(timer);
                }
                _timersToAdd.Clear();
            }

            if (_timersToRemove.Count > 0)
            {
                foreach (var timer in _timersToRemove)
                {
                    _timers.Remove(timer);
                }
                _timersToRemove.Clear();
            }
        }
    }
}
