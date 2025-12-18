using System;
using System.Collections.Generic;
using System.Reflection;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Generic object pool for timers to reduce garbage collection.
    /// Uses TimerManager.ThreadMode for thread safety.
    /// </summary>
    public static class TimerPool
    {
        private static readonly Dictionary<Type, Queue<Timer>> _pools = new Dictionary<Type, Queue<Timer>>();
        private static readonly Dictionary<Type, ConstructorInfo> _constructorCache = new Dictionary<Type, ConstructorInfo>();
        private static readonly object _lockObject = new object();
        private static int _defaultCapacity = 10;
        private static int _maxCapacity = 50;

        private static bool IsThreadSafe => TimerManager.ThreadMode == TimerThreadMode.ThreadSafe;

        public static int DefaultCapacity
        {
            get => _defaultCapacity;
            set => _defaultCapacity = Math.Max(1, value);
        }

        public static int MaxCapacity
        {
            get => _maxCapacity;
            set => _maxCapacity = Math.Max(1, value);
        }

        /// <summary>
        /// Gets a timer of type T from the pool or creates a new one.
        /// </summary>
        public static T Get<T>(float initialTime = 1f) where T : Timer
        {
            var type = typeof(T);
            
            if (IsThreadSafe)
            {
                lock (_lockObject)
                {
                    return GetInternal<T>(type, initialTime);
                }
            }
            return GetInternal<T>(type, initialTime);
        }

        private static T GetInternal<T>(Type type, float initialTime) where T : Timer
        {
            if (_pools.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                var timer = (T)pool.Dequeue();
                timer.TimeScale = 1f;
                timer.UseUnscaledTime = false;
                timer.Reset(initialTime);
                TimerManager.RegisterTimer(timer);
                return timer;
            }
            return CreateTimer<T>(initialTime);
        }

        private static T CreateTimer<T>(float initialTime) where T : Timer
        {
            var type = typeof(T);
            
            if (!_constructorCache.TryGetValue(type, out var constructor))
            {
                constructor = type.GetConstructor(new[] { typeof(float) })
                    ?? type.GetConstructor(Type.EmptyTypes)
                    ?? type.GetConstructor(new[] { typeof(float), typeof(int) })
                    ?? type.GetConstructor(new[] { typeof(int) });
                
                if (constructor == null)
                    throw new ArgumentException($"Timer type {type.Name} has no suitable constructor.");
                
                _constructorCache[type] = constructor;
            }

            var parameters = constructor.GetParameters();
            object[] args = parameters.Length switch
            {
                0 => Array.Empty<object>(),
                1 when parameters[0].ParameterType == typeof(float) => new object[] { initialTime },
                1 when parameters[0].ParameterType == typeof(int) => new object[] { (int)initialTime },
                2 => new object[] { initialTime, 0 },
                _ => Array.Empty<object>()
            };

            return (T)constructor.Invoke(args);
        }

        /// <summary>
        /// Returns a timer to the pool for reuse.
        /// </summary>
        public static void Release(Timer timer)
        {
            if (timer == null) return;
            
            var type = timer.GetType();
            timer.Pause();
            TimerManager.UnregisterTimer(timer);
            
            if (IsThreadSafe)
            {
                lock (_lockObject) ReleaseInternal(timer, type);
            }
            else
            {
                ReleaseInternal(timer, type);
            }
        }

        private static void ReleaseInternal(Timer timer, Type type)
        {
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<Timer>(_defaultCapacity);
                _pools[type] = pool;
            }
            
            if (pool.Count < _maxCapacity)
                pool.Enqueue(timer);
        }

        public static void Clear()
        {
            if (IsThreadSafe)
                lock (_lockObject) _pools.Clear();
            else
                _pools.Clear();
        }

        public static void Prewarm<T>(int count) where T : Timer
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var timer = CreateTimer<T>(1f);
                    TimerManager.UnregisterTimer(timer);
                    Release(timer);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[TimerPool] Failed to prewarm {typeof(T).Name}: {e.Message}");
                    break;
                }
            }
        }

        public static void Prewarm(Type timerType, int count)
        {
            if (timerType == null || !typeof(Timer).IsAssignableFrom(timerType)) return;
            
            var method = typeof(TimerPool).GetMethod(nameof(Prewarm), new[] { typeof(int) });
            var genericMethod = method.MakeGenericMethod(timerType);
            genericMethod.Invoke(null, new object[] { count });
        }

        public static int GetPoolSize<T>() where T : Timer
        {
            if (IsThreadSafe)
            {
                lock (_lockObject)
                {
                    return _pools.TryGetValue(typeof(T), out var pool) ? pool.Count : 0;
                }
            }
            return _pools.TryGetValue(typeof(T), out var p) ? p.Count : 0;
        }

        public static int TotalPooledCount
        {
            get
            {
                int count = 0;
                if (IsThreadSafe)
                {
                    lock (_lockObject)
                    {
                        foreach (var pool in _pools.Values) count += pool.Count;
                    }
                }
                else
                {
                    foreach (var pool in _pools.Values) count += pool.Count;
                }
                return count;
            }
        }
    }
}
