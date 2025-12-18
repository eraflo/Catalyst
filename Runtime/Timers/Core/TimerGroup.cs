using System;
using System.Collections.Generic;
using System.Linq;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Groups multiple timers for collective control.
    /// Uses TimerManager.ThreadMode for thread safety.
    /// </summary>
    public class TimerGroup : IDisposable
    {
        private readonly HashSet<Timer> _timers = new HashSet<Timer>();
        private readonly object _lock = new object();
        private readonly string _name;
        private bool _isPaused;
        private bool _isDisposed;

        private bool IsThreadSafe => TimerManager.ThreadMode == TimerThreadMode.ThreadSafe;

        public string Name => _name;
        public bool IsPaused => _isPaused;

        public int Count
        {
            get
            {
                if (IsThreadSafe) lock (_lock) return _timers.Count;
                return _timers.Count;
            }
        }

        public TimerGroup(string name = null)
        {
            _name = name ?? $"Group_{GetHashCode()}";
            TimerGroups.Register(this);
        }

        public TimerGroup Add(Timer timer)
        {
            if (timer == null || _isDisposed) return this;
            
            if (IsThreadSafe)
                lock (_lock) _timers.Add(timer);
            else
                _timers.Add(timer);
            return this;
        }

        public TimerGroup Add(params Timer[] timers)
        {
            if (_isDisposed) return this;
            
            if (IsThreadSafe)
            {
                lock (_lock)
                    foreach (var timer in timers)
                        if (timer != null) _timers.Add(timer);
            }
            else
            {
                foreach (var timer in timers)
                    if (timer != null) _timers.Add(timer);
            }
            return this;
        }

        public TimerGroup Remove(Timer timer)
        {
            if (IsThreadSafe)
                lock (_lock) _timers.Remove(timer);
            else
                _timers.Remove(timer);
            return this;
        }

        public void Clear()
        {
            if (IsThreadSafe)
                lock (_lock) _timers.Clear();
            else
                _timers.Clear();
        }

        public void StartAll() { _isPaused = false; ForEach(t => t.Start()); }
        public void PauseAll() { _isPaused = true; ForEach(t => t.Pause()); }
        public void ResumeAll() { _isPaused = false; ForEach(t => t.Resume()); }
        public void StopAll() { _isPaused = false; ForEach(t => t.Stop()); }
        public void ResetAll() { ForEach(t => t.Reset()); }
        public void SetTimeScale(float timeScale) { ForEach(t => t.TimeScale = timeScale); }

        public bool Contains(Timer timer)
        {
            if (IsThreadSafe) lock (_lock) return _timers.Contains(timer);
            return _timers.Contains(timer);
        }

        private void ForEach(Action<Timer> action)
        {
            Timer[] snapshot;
            if (IsThreadSafe)
                lock (_lock) snapshot = _timers.ToArray();
            else
                snapshot = _timers.ToArray();
            
            foreach (var timer in snapshot)
            {
                try { action(timer); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
            }
        }

        public void Dispose(bool disposeTimers = false)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (disposeTimers) ForEach(t => t.Dispose());
            
            if (IsThreadSafe)
                lock (_lock) _timers.Clear();
            else
                _timers.Clear();
            
            TimerGroups.Unregister(this);
        }

        public void Dispose() => Dispose(false);
    }

    /// <summary>
    /// Global registry for timer groups.
    /// Uses TimerManager.ThreadMode for thread safety.
    /// </summary>
    public static class TimerGroups
    {
        private static readonly Dictionary<string, TimerGroup> _groups = new Dictionary<string, TimerGroup>();
        private static readonly object _lock = new object();

        private static bool IsThreadSafe => TimerManager.ThreadMode == TimerThreadMode.ThreadSafe;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                    Clear();
            };
        }
#endif

        internal static void Register(TimerGroup group)
        {
            if (group == null || string.IsNullOrEmpty(group.Name)) return;
            if (IsThreadSafe)
                lock (_lock) _groups[group.Name] = group;
            else
                _groups[group.Name] = group;
        }

        internal static void Unregister(TimerGroup group)
        {
            if (group == null) return;
            if (IsThreadSafe)
                lock (_lock) _groups.Remove(group.Name);
            else
                _groups.Remove(group.Name);
        }

        public static TimerGroup Get(string name)
        {
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    _groups.TryGetValue(name, out var group);
                    return group;
                }
            }
            _groups.TryGetValue(name, out var g);
            return g;
        }

        public static TimerGroup GetOrCreate(string name)
        {
            if (IsThreadSafe)
            {
                lock (_lock)
                {
                    if (!_groups.TryGetValue(name, out var group))
                        group = new TimerGroup(name);
                    return group;
                }
            }
            if (!_groups.TryGetValue(name, out var g))
                g = new TimerGroup(name);
            return g;
        }

        public static void Pause(string groupName) => Get(groupName)?.PauseAll();
        public static void Resume(string groupName) => Get(groupName)?.ResumeAll();
        public static void Stop(string groupName) => Get(groupName)?.StopAll();

        public static void PauseAll()
        {
            TimerGroup[] snapshot;
            if (IsThreadSafe)
                lock (_lock) snapshot = _groups.Values.ToArray();
            else
                snapshot = _groups.Values.ToArray();
            foreach (var group in snapshot) group.PauseAll();
        }

        public static void ResumeAll()
        {
            TimerGroup[] snapshot;
            if (IsThreadSafe)
                lock (_lock) snapshot = _groups.Values.ToArray();
            else
                snapshot = _groups.Values.ToArray();
            foreach (var group in snapshot) group.ResumeAll();
        }

        public static void Clear()
        {
            if (IsThreadSafe)
                lock (_lock) _groups.Clear();
            else
                _groups.Clear();
        }

        public static int GroupCount
        {
            get
            {
                if (IsThreadSafe) lock (_lock) return _groups.Count;
                return _groups.Count;
            }
        }
    }
}
