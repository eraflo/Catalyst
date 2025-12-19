using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Timers.Backends
{
    /// <summary>
    /// Standard backend implementation using managed objects.
    /// Thread-safe, async-safe, and editor-safe.
    /// Works with any ITimer struct by boxing to a wrapper class.
    /// </summary>
    public class StandardBackend : ITimerBackend
    {
        private readonly Dictionary<uint, TimerWrapper> _timers = new Dictionary<uint, TimerWrapper>();
        private readonly List<uint> _toRemove = new List<uint>();
        
        // Thread-safe pending operations
        private readonly ConcurrentQueue<PendingOperation> _pendingOperations = new ConcurrentQueue<PendingOperation>();
        private readonly object _lockObject = new object();
        
        private int _nextId = 1;
        private byte _generation = 0;
        private int _mainThreadId = -1;

        // Type ID registry (static, thread-safe)
        private static readonly ConcurrentDictionary<Type, ushort> _typeIds = new ConcurrentDictionary<Type, ushort>();
        private static int _nextTypeId = 1;

        public int Count
        {
            get
            {
                lock (_lockObject) { return _timers.Count; }
            }
        }

        public StandardBackend()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        public TimerHandle Create<T>(TimerConfig config) where T : struct, ITimer
        {
            var timer = new T();
            InitializeTimer(ref timer, config);

            var id = (uint)Interlocked.Increment(ref _nextId);
            var typeId = GetOrRegisterTypeId<T>();
            var handle = new TimerHandle(id, _generation, typeId);
            var wrapper = new TimerWrapper(timer, handle);

            if (IsMainThread)
            {
                lock (_lockObject)
                {
                    _timers[id] = wrapper;
                }
            }
            else
            {
                _pendingOperations.Enqueue(new PendingOperation(OperationType.Add, id, wrapper));
            }

            return handle;
        }

        private void InitializeTimer<T>(ref T timer, TimerConfig config) where T : struct, ITimer
        {
            timer.CurrentTime = config.Duration;
            timer.TimeScale = config.TimeScale > 0 ? config.TimeScale : 1f;
            timer.IsRunning = true;
            timer.IsFinished = false;
        }

        private static ushort GetOrRegisterTypeId<T>()
        {
            var type = typeof(T);
            return _typeIds.GetOrAdd(type, _ => (ushort)Interlocked.Increment(ref _nextTypeId));
        }

        public float GetCurrentTime(TimerHandle handle)
        {
            lock (_lockObject)
            {
                return _timers.TryGetValue(handle.Id, out var wrapper) ? wrapper.Timer.CurrentTime : 0f;
            }
        }

        public float GetProgress(TimerHandle handle)
        {
            lock (_lockObject)
            {
                if (!_timers.TryGetValue(handle.Id, out var wrapper)) return 0f;
                var t = wrapper.Timer;
                return t.InitialTime > 0 ? Mathf.Clamp01(t.CurrentTime / t.InitialTime) : 0f;
            }
        }

        public bool IsFinished(TimerHandle handle)
        {
            lock (_lockObject)
            {
                // Invalid/not found handles are considered finished
                if (!_timers.TryGetValue(handle.Id, out var wrapper))
                    return true;
                return wrapper.Timer.IsFinished;
            }
        }

        public bool IsRunning(TimerHandle handle)
        {
            lock (_lockObject)
            {
                return _timers.TryGetValue(handle.Id, out var wrapper) && wrapper.Timer.IsRunning;
            }
        }

        public void Pause(TimerHandle handle)
        {
            if (IsMainThread)
            {
                lock (_lockObject)
                {
                    if (_timers.TryGetValue(handle.Id, out var wrapper))
                    {
                        var timer = wrapper.Timer;
                        timer.IsRunning = false;
                        wrapper.Timer = timer;
                    }
                }
            }
            else
            {
                _pendingOperations.Enqueue(new PendingOperation(OperationType.Pause, handle.Id, null));
            }
        }

        public void Resume(TimerHandle handle)
        {
            if (IsMainThread)
            {
                lock (_lockObject)
                {
                    if (_timers.TryGetValue(handle.Id, out var wrapper))
                    {
                        var timer = wrapper.Timer;
                        timer.IsRunning = true;
                        wrapper.Timer = timer;
                    }
                }
            }
            else
            {
                _pendingOperations.Enqueue(new PendingOperation(OperationType.Resume, handle.Id, null));
            }
        }

        public void Start(TimerHandle handle) => Resume(handle);

        public void Cancel(TimerHandle handle)
        {
            if (IsMainThread)
            {
                lock (_lockObject)
                {
                    _timers.Remove(handle.Id);
                }
                TimerCallbacks.Remove(handle.Id);
            }
            else
            {
                _pendingOperations.Enqueue(new PendingOperation(OperationType.Remove, handle.Id, null));
            }
        }

        public void Reset(TimerHandle handle)
        {
            lock (_lockObject)
            {
                if (_timers.TryGetValue(handle.Id, out var wrapper))
                {
                    var timer = wrapper.Timer;
                    timer.Reset();
                    wrapper.Timer = timer;
                }
            }
        }

        public void SetTimeScale(TimerHandle handle, float scale)
        {
            lock (_lockObject)
            {
                if (_timers.TryGetValue(handle.Id, out var wrapper))
                {
                    var timer = wrapper.Timer;
                    timer.TimeScale = scale;
                    wrapper.Timer = timer;
                }
            }
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            // Process pending operations from other threads
            ProcessPendingOperations();

            _toRemove.Clear();

            lock (_lockObject)
            {
                foreach (var kvp in _timers)
                {
                    var wrapper = kvp.Value;
                    var timer = wrapper.Timer;

                    if (!timer.IsRunning || timer.IsFinished) continue;

                    float dt = timer.UseUnscaledTime ? unscaledDeltaTime : deltaTime;
                    dt *= timer.TimeScale;

                    // Invoke OnTick for each frame (with deltaTime as float parameter)
                    TimerCallbacks.Invoke<OnTick, float>(kvp.Key, dt);

                    timer.Tick(dt);

                    // Let the timer collect its own callbacks
                    var collector = new CallbackCollector(kvp.Key);
                    timer.CollectCallbacks(collector);

                    wrapper.Timer = timer;

                    // Only auto-remove if finished AND not reset by callback (IsRunning would be true if reset)
                    if (timer.IsFinished && !timer.IsRunning)
                    {
                        _toRemove.Add(kvp.Key);
                    }
                }

                foreach (var id in _toRemove)
                {
                    _timers.Remove(id);
                }
            }
        }

        private void ProcessPendingOperations()
        {
            while (_pendingOperations.TryDequeue(out var op))
            {
                lock (_lockObject)
                {
                    switch (op.Type)
                    {
                        case OperationType.Add:
                            _timers[op.Id] = op.Wrapper;
                            break;
                        case OperationType.Remove:
                            _timers.Remove(op.Id);
                            TimerCallbacks.Remove(op.Id);
                            break;
                        case OperationType.Pause:
                            if (_timers.TryGetValue(op.Id, out var wPause))
                            {
                                var t = wPause.Timer;
                                t.IsRunning = false;
                                wPause.Timer = t;
                            }
                            break;
                        case OperationType.Resume:
                            if (_timers.TryGetValue(op.Id, out var wResume))
                            {
                                var t = wResume.Timer;
                                t.IsRunning = true;
                                wResume.Timer = t;
                            }
                            break;
                    }
                }
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _timers.Clear();
                _generation++;
            }
            TimerCallbacks.Clear();
            while (_pendingOperations.TryDequeue(out _)) { }
        }

        public void Dispose()
        {
            Clear();
        }

        #region Internal Types

        private enum OperationType { Add, Remove, Pause, Resume }

        private readonly struct PendingOperation
        {
            public readonly OperationType Type;
            public readonly uint Id;
            public readonly TimerWrapper Wrapper;

            public PendingOperation(OperationType type, uint id, TimerWrapper wrapper)
            {
                Type = type;
                Id = id;
                Wrapper = wrapper;
            }
        }

        private class TimerWrapper
        {
            public ITimer Timer;
            public TimerHandle Handle;

            public TimerWrapper(ITimer timer, TimerHandle handle)
            {
                Timer = timer;
                Handle = handle;
            }
        }

        #endregion

        public List<TimerDebugInfo> GetActiveTimers()
        {
            var result = new List<TimerDebugInfo>();
            lock (_lockObject)
            {
                foreach (var kvp in _timers)
                {
                    var wrapper = kvp.Value;
                    var timer = wrapper.Timer;
                    result.Add(new TimerDebugInfo
                    {
                        Id = kvp.Key,
                        TypeName = timer.GetType().Name,
                        CurrentTime = timer.CurrentTime,
                        InitialTime = timer.InitialTime,
                        Progress = timer.InitialTime > 0 ? timer.CurrentTime / timer.InitialTime : 0f,
                        IsRunning = timer.IsRunning,
                        IsFinished = timer.IsFinished,
                        TimeScale = timer.TimeScale
                    });
                }
            }
            return result;
        }
    }
}
