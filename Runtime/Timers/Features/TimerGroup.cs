using System.Collections.Generic;

namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Groups timers together for batch operations.
    /// Use Timer.CreateGroup() to create a new group.
    /// </summary>
    public class TimerGroup
    {
        private readonly List<TimerHandle> _handles = new List<TimerHandle>();
        private readonly string _name;

        private Timer _cleanupTimer;
        private bool CleanupPredicate(TimerHandle h) => _cleanupTimer.IsFinished(h);

        /// <summary>
        /// Name of this timer group.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Number of timers in the group.
        /// </summary>
        public int Count => _handles.Count;

        /// <summary>
        /// Creates a new timer group.
        /// </summary>
        /// <param name="name">Optional name for debugging.</param>
        public TimerGroup(string name = null)
        {
            _name = name ?? $"Group_{GetHashCode()}";
        }

        /// <summary>
        /// Adds a timer to the group.
        /// </summary>
        public void Add(TimerHandle handle)
        {
            if (handle.IsValid && !_handles.Contains(handle))
            {
                _handles.Add(handle);
            }
        }

        /// <summary>
        /// Removes a timer from the group.
        /// </summary>
        public void Remove(TimerHandle handle)
        {
            _handles.Remove(handle);
        }

        /// <summary>
        /// Creates a timer and adds it to the group.
        /// </summary>
        public TimerHandle Create<T>(float duration) where T : struct, ITimer
        {
            var handle = App.Get<Timer>().CreateTimer<T>(duration);
            Add(handle);
            return handle;
        }

        /// <summary>
        /// Creates a delay and adds it to the group.
        /// </summary>
        public TimerHandle Delay(float delay, System.Action onComplete)
        {
            var handle = App.Get<Timer>().CreateDelay(delay, onComplete);
            Add(handle);
            return handle;
        }

        /// <summary>
        /// Pauses all timers in the group.
        /// </summary>
        public void PauseAll()
        {
            var timer = App.Get<Timer>();
            foreach (var handle in _handles)
            {
                timer.Pause(handle);
            }
        }

        /// <summary>
        /// Resumes all timers in the group.
        /// </summary>
        public void ResumeAll()
        {
            var timer = App.Get<Timer>();
            foreach (var handle in _handles)
            {
                timer.Resume(handle);
            }
        }

        /// <summary>
        /// Cancels all timers in the group.
        /// </summary>
        public void CancelAll()
        {
            var timer = App.Get<Timer>();
            foreach (var handle in _handles)
            {
                timer.CancelTimer(handle);
            }
            _handles.Clear();
        }

        /// <summary>
        /// Resets all timers in the group.
        /// </summary>
        public void ResetAll()
        {
            var timer = App.Get<Timer>();
            foreach (var handle in _handles)
            {
                timer.ResetTimer(handle);
            }
        }

        /// <summary>
        /// Sets time scale for all timers in the group.
        /// </summary>
        public void SetTimeScaleAll(float scale)
        {
            var timer = App.Get<Timer>();
            foreach (var handle in _handles)
            {
                timer.SetTimeScale(handle, scale);
            }
        }

        /// <summary>
        /// Removes finished timers from the group.
        /// </summary>
        public void CleanupFinished()
        {
            _cleanupTimer = App.Get<Timer>();
            _handles.RemoveAll(CleanupPredicate);
        }
    }

    // Extension for Timer class
    public partial class Timer
    {
        /// <summary>
        /// Creates a new timer group.
        /// </summary>
        /// <param name="name">Optional name for debugging.</param>
        public TimerGroup CreateGroup(string name = null) => new TimerGroup(name);
    }
}
