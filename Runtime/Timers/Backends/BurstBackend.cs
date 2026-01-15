using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Eraflo.Catalyst.Timers.Backends
{
    /// <summary>
    /// High-performance backend using Unity Burst and Jobs for timer updates.
    /// Uses NativeContainers for Burst-compiled parallel updates.
    /// </summary>
    public class BurstBackend : ITimerBackend
    {
        // Native containers for Burst-compiled updates
        private NativeList<TimerData> _timerData;
        private NativeList<bool> _activeFlags;
        private NativeParallelHashMap<uint, int> _handleToIndex;

        // Managed wrappers for callback collection (can't be in Burst jobs)
        private readonly List<TimerWrapper> _wrappers = new List<TimerWrapper>();
        private readonly Stack<int> _freeIndices = new Stack<int>();
        private readonly List<uint> _toRemove = new List<uint>();
        private readonly object _lockObject = new object();

        private uint _nextId = 1;
        private byte _generation = 0;
        private bool _isDisposed = false;

        // Channel management
        private readonly List<string> _channelNames = new List<string>();
        private readonly Dictionary<string, int> _channelToIndex = new Dictionary<string, int>();
        private NativeList<float> _channelScales;

        private static readonly Dictionary<Type, ushort> _typeIds = new Dictionary<Type, ushort>();
        private static ushort _nextTypeId = 1;

        public BurstBackend()
        {
            _timerData = new NativeList<TimerData>(64, Allocator.Persistent);
            _activeFlags = new NativeList<bool>(64, Allocator.Persistent);
            _handleToIndex = new NativeParallelHashMap<uint, int>(64, Allocator.Persistent);
            _channelScales = new NativeList<float>(8, Allocator.Persistent);
        }

        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _handleToIndex.Count();
                }
            }
        }

        public TimerHandle Create<T>(TimerConfig config) where T : struct, ITimer
        {
            lock (_lockObject)
            {
                var channel = string.IsNullOrEmpty(config.Channel) ? "World" : config.Channel;
                int channelIndex = GetOrRegisterChannel(channel);
                var timer = new T();

                timer.CurrentTime = config.Duration;
                timer.TimeScale = config.TimeScale > 0 ? config.TimeScale : 1f;
                timer.Channel = channel;
                timer.IsRunning = true;

                var data = new TimerData
                {
                    CurrentTime = config.Duration,
                    InitialTime = config.Duration,
                    TimeScale = config.TimeScale > 0 ? config.TimeScale : 1f,
                    ChannelIndex = channelIndex,
                    IsRunning = true,
                    IsFinished = false,
                    UseUnscaledTime = config.UseUnscaledTime,
                    WasFinishedLastFrame = false
                };

                var wrapper = new TimerWrapper { Timer = timer };
                int index;

                if (_freeIndices.Count > 0)
                {
                    index = _freeIndices.Pop();
                    _timerData[index] = data;
                    _activeFlags[index] = true;
                    _wrappers[index] = wrapper;
                }
                else
                {
                    index = _wrappers.Count;
                    _timerData.Add(data);
                    _activeFlags.Add(true);
                    _wrappers.Add(wrapper);
                }

                var id = _nextId++;
                var typeId = GetOrRegisterTypeId<T>();
                var handle = new TimerHandle(id, _generation, typeId);
                _handleToIndex.Add(id, index);

                return handle;
            }
        }

        private int GetOrRegisterChannel(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "World";
            
            if (!_channelToIndex.TryGetValue(name, out var index))
            {
                index = _channelNames.Count;
                _channelNames.Add(name);
                _channelToIndex.Add(name, index);
                _channelScales.Add(1f);
            }
            return index;
        }

        private static ushort GetOrRegisterTypeId<T>()
        {
            var type = typeof(T);
            if (!_typeIds.TryGetValue(type, out var id))
            {
                id = _nextTypeId++;
                _typeIds[type] = id;
            }
            return id;
        }

        private bool TryGetIndex(uint id, out int index)
        {
            return _handleToIndex.TryGetValue(id, out index);
        }

        public float GetCurrentTime(TimerHandle handle)
        {
            lock (_lockObject)
            {
                if (TryGetIndex(handle.Id, out var i)) return _timerData[i].CurrentTime;
                return 0f;
            }
        }

        public float GetProgress(TimerHandle handle)
        {
            lock (_lockObject)
            {
                if (!TryGetIndex(handle.Id, out var i)) return 0f;
                var d = _timerData[i];
                return d.InitialTime > 0 ? Mathf.Clamp01(d.CurrentTime / d.InitialTime) : 0f;
            }
        }

        public bool IsFinished(TimerHandle handle)
        {
            lock (_lockObject)
            {
                // Invalid/not found handles are considered finished
                if (!TryGetIndex(handle.Id, out var i))
                    return true;
                return _timerData[i].IsFinished;
            }
        }

        public bool IsRunning(TimerHandle handle)
        {
            lock (_lockObject)
            {
                return TryGetIndex(handle.Id, out var i) && _timerData[i].IsRunning;
            }
        }

        public void Pause(TimerHandle handle)
        {
            lock (_lockObject)
            {
                if (TryGetIndex(handle.Id, out var i))
                {
                    var d = _timerData[i];
                    d.IsRunning = false;
                    _timerData[i] = d;

                    var w = _wrappers[i];
                    var t = w.Timer;
                    t.IsRunning = false;
                    w.Timer = t;

                    TimerCallbacks.Invoke<OnPause>(handle.Id);
                }
            }
        }

        public void Resume(TimerHandle handle)
        {
            lock (_lockObject)
            {
                if (TryGetIndex(handle.Id, out var i))
                {
                    var d = _timerData[i];
                    d.IsRunning = true;
                    _timerData[i] = d;

                    var w = _wrappers[i];
                    var t = w.Timer;
                    t.IsRunning = true;
                    w.Timer = t;

                    TimerCallbacks.Invoke<OnResume>(handle.Id);
                }
            }
        }

        public void Start(TimerHandle handle) => Resume(handle);

        public void Cancel(TimerHandle handle)
        {
            lock (_lockObject)
            {
                if (TryGetIndex(handle.Id, out var i))
                {
                    TimerCallbacks.Invoke<OnCancel>(handle.Id);
                    _handleToIndex.Remove(handle.Id);
                    _activeFlags[i] = false;
                    _freeIndices.Push(i);
                }
            }
            TimerCallbacks.Remove(handle.Id);
        }

        public void Reset(TimerHandle handle)
        {
            lock (_lockObject)
            {
                if (TryGetIndex(handle.Id, out var i))
                {
                    var d = _timerData[i];
                    d.CurrentTime = d.InitialTime;
                    d.IsFinished = false;
                    d.WasFinishedLastFrame = false;
                    d.IsRunning = true;
                    _timerData[i] = d;

                    var w = _wrappers[i];
                    var t = w.Timer;
                    t.Reset();
                    w.Timer = t;

                    TimerCallbacks.Invoke<OnReset>(handle.Id);
                }
            }
        }

        public void SetTimeScale(TimerHandle handle, float scale)
        {
            lock (_lockObject)
            {
                if (TryGetIndex(handle.Id, out var i))
                {
                    var d = _timerData[i];
                    d.TimeScale = scale;
                    _timerData[i] = d;

                    var w = _wrappers[i];
                    var t = w.Timer;
                    t.TimeScale = scale;
                    w.Timer = t;
                }
            }
        }

        public void SetChannel(TimerHandle handle, string channel)
        {
            lock (_lockObject)
            {
                if (TryGetIndex(handle.Id, out var i))
                {
                    var w = _wrappers[i];
                    var t = w.Timer;
                    t.Channel = channel;
                    w.Timer = t;

                    var d = _timerData[i];
                    d.ChannelIndex = GetOrRegisterChannel(channel);
                    _timerData[i] = d;
                }
            }
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            // Sync channel scales
            var chronos = App.Get<Eraflo.Catalyst.Core.Chronos.ChronosManager>();
            for (int i = 0; i < _channelNames.Count; i++)
            {
                _channelScales[i] = chronos != null ? chronos.GetChannelScale(_channelNames[i]) : 1f;
            }

            // Schedule Burst job for timer updates
            var job = new TimerUpdateJob
            {
                TimerData = _timerData.AsArray(),
                ActiveFlags = _activeFlags.AsArray(),
                ChannelScales = _channelScales.AsArray(),
                DeltaTime = deltaTime,
                UnscaledDeltaTime = unscaledDeltaTime
            };

            var jobHandle = job.Schedule(_timerData.Length, 64);
            jobHandle.Complete();

            // Process callbacks on main thread (can't be in Burst job)
            ProcessCallbacksAndCleanup();
        }

        [BurstCompile]
        private struct TimerUpdateJob : IJobParallelFor
        {
            public NativeArray<TimerData> TimerData;
            [ReadOnly] public NativeArray<bool> ActiveFlags;
            [ReadOnly] public NativeArray<float> ChannelScales;
            public float DeltaTime;
            public float UnscaledDeltaTime;

            public void Execute(int index)
            {
                if (!ActiveFlags[index]) return;

                var data = TimerData[index];
                if (!data.IsRunning || data.IsFinished) return;

                data.WasFinishedLastFrame = data.IsFinished;

                float dt = data.UseUnscaledTime ? UnscaledDeltaTime : DeltaTime;
                dt *= data.TimeScale;

                if (data.ChannelIndex >= 0 && data.ChannelIndex < ChannelScales.Length)
                {
                    dt *= ChannelScales[data.ChannelIndex];
                }

                data.CurrentTime -= dt;
                if (data.CurrentTime <= 0f)
                {
                    data.CurrentTime = 0f;
                    data.IsFinished = true;
                }

                TimerData[index] = data;
            }
        }

        private void ProcessCallbacksAndCleanup()
        {
            _toRemove.Clear();

            lock (_lockObject)
            {
                var keys = _handleToIndex.GetKeyArray(Allocator.Temp);
                for (int k = 0; k < keys.Length; k++)
                {
                    var id = keys[k];
                    if (!_handleToIndex.TryGetValue(id, out var index)) continue;
                    if (!_activeFlags[index]) continue;

                    var data = _timerData[index];
                    var wrapper = _wrappers[index];

                    // Sync data back to wrapper
                    var timer = wrapper.Timer;
                    timer.CurrentTime = data.CurrentTime;
                    timer.IsFinished = data.IsFinished;

                    // Calculate delta for OnTick
                    float dt = data.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    dt *= data.TimeScale;

                    // Invoke OnTick
                    if (data.IsRunning && !data.WasFinishedLastFrame)
                    {
                        TimerCallbacks.Invoke<OnTick, float>(id, dt);
                    }

                    // Let timer collect custom callbacks
                    var collector = new CallbackCollector(id);
                    timer.CollectCallbacks(collector);
                    wrapper.Timer = timer;

                    // Check if timer should be auto-removed (finished and NOT reset by callback)
                    // Re-read data after callbacks in case Reset was called
                    data = _timerData[index];
                    if (data.IsFinished && !data.IsRunning)
                    {
                        _toRemove.Add(id);
                    }
                }
                keys.Dispose();

                foreach (var id in _toRemove)
                {
                    if (_handleToIndex.TryGetValue(id, out var index))
                    {
                        _handleToIndex.Remove(id);
                        _activeFlags[index] = false;
                        _freeIndices.Push(index);
                    }
                    TimerCallbacks.Remove(id);
                }
            }
        }

        public void Clear()
        {
            lock (_lockObject)
            {
                _timerData.Clear();
                _activeFlags.Clear();
                _handleToIndex.Clear();
                _wrappers.Clear();
                _freeIndices.Clear();
                _generation++;
            }
            TimerCallbacks.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Clear();
            
            if (_timerData.IsCreated) _timerData.Dispose();
            if (_activeFlags.IsCreated) _activeFlags.Dispose();
            if (_handleToIndex.IsCreated) _handleToIndex.Dispose();
            if (_channelScales.IsCreated) _channelScales.Dispose();
        }

        /// <summary>Timer data for Burst-compatible updates.</summary>
        private struct TimerData
        {
            public float CurrentTime;
            public float InitialTime;
            public float TimeScale;
            public int ChannelIndex;
            public bool IsRunning;
            public bool IsFinished;
            public bool UseUnscaledTime;
            public bool WasFinishedLastFrame;
        }

        /// <summary>Wrapper to hold ITimer for callback collection.</summary>
        private class TimerWrapper
        {
            public ITimer Timer;
        }

        public List<TimerDebugInfo> GetActiveTimers()
        {
            var result = new List<TimerDebugInfo>();
            lock (_lockObject)
            {
                var keys = _handleToIndex.GetKeyArray(Unity.Collections.Allocator.Temp);
                for (int k = 0; k < keys.Length; k++)
                {
                    var id = keys[k];
                    if (!_handleToIndex.TryGetValue(id, out var index)) continue;
                    if (!_activeFlags[index]) continue;

                    var data = _timerData[index];
                    var wrapper = _wrappers[index];
                    
                    result.Add(new TimerDebugInfo
                    {
                        Id = id,
                        TypeName = wrapper.Timer.GetType().Name,
                        CurrentTime = data.CurrentTime,
                        InitialTime = data.InitialTime,
                        Progress = data.InitialTime > 0 ? data.CurrentTime / data.InitialTime : 0f,
                        IsRunning = data.IsRunning,
                        IsFinished = data.IsFinished,
                        TimeScale = data.TimeScale,
                        Channel = wrapper.Timer.Channel
                    });
                }
                keys.Dispose();
            }
            return result;
        }
    }
}
