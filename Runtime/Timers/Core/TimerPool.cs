using System;
using System.Collections.Generic;
using System.Reflection;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Generic object pool for timers to reduce garbage collection.
    /// Supports any Timer type via TimerPool.Get&lt;T&gt;().
    /// Uses reflection to support custom timer types.
    /// </summary>
    public static class TimerPool
    {
        private static readonly Dictionary<Type, Queue<Timer>> _pools = new Dictionary<Type, Queue<Timer>>();
        private static readonly Dictionary<Type, ConstructorInfo> _constructorCache = new Dictionary<Type, ConstructorInfo>();
        private static readonly object _lockObject = new object();
        private static int _defaultCapacity = 10;
        private static int _maxCapacity = 50;

        /// <summary>
        /// Default pool capacity per timer type.
        /// </summary>
        public static int DefaultCapacity
        {
            get => _defaultCapacity;
            set => _defaultCapacity = Math.Max(1, value);
        }

        /// <summary>
        /// Maximum pool capacity per timer type.
        /// </summary>
        public static int MaxCapacity
        {
            get => _maxCapacity;
            set => _maxCapacity = Math.Max(1, value);
        }

        /// <summary>
        /// Gets a timer of type T from the pool or creates a new one using reflection.
        /// Supports any Timer subclass with a parameterless or single float constructor.
        /// </summary>
        /// <typeparam name="T">Timer type to get.</typeparam>
        /// <param name="initialTime">Optional initial time for timers that require it.</param>
        /// <returns>A timer instance ready for use.</returns>
        public static T Get<T>(float initialTime = 1f) where T : Timer
        {
            var type = typeof(T);
            
            lock (_lockObject)
            {
                // Try to get from pool
                if (_pools.TryGetValue(type, out var pool) && pool.Count > 0)
                {
                    var timer = (T)pool.Dequeue();
                    timer.TimeScale = 1f;
                    timer.UseUnscaledTime = false;
                    timer.Reset(initialTime);
                    
                    // Re-register with TimerManager
                    TimerManager.RegisterTimer(timer);
                    return timer;
                }
            }
            
            // Create new timer instance using reflection
            return CreateTimer<T>(initialTime);
        }

        /// <summary>
        /// Creates a new timer instance using reflection.
        /// Caches constructors for performance.
        /// </summary>
        private static T CreateTimer<T>(float initialTime) where T : Timer
        {
            var type = typeof(T);
            
            lock (_lockObject)
            {
                if (!_constructorCache.TryGetValue(type, out var constructor))
                {
                    // Try to find a suitable constructor
                    // Priority: (float), (), (float, int), (int)
                    constructor = type.GetConstructor(new[] { typeof(float) });
                    
                    if (constructor == null)
                        constructor = type.GetConstructor(Type.EmptyTypes);
                    
                    if (constructor == null)
                        constructor = type.GetConstructor(new[] { typeof(float), typeof(int) });
                    
                    if (constructor == null)
                        constructor = type.GetConstructor(new[] { typeof(int) });
                    
                    if (constructor == null)
                        throw new ArgumentException($"Timer type {type.Name} has no suitable constructor. " +
                            "Expected: (), (float), (int), or (float, int)");
                    
                    _constructorCache[type] = constructor;
                }

                // Invoke constructor with appropriate parameters
                var parameters = constructor.GetParameters();
                object[] args;
                
                if (parameters.Length == 0)
                {
                    args = Array.Empty<object>();
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float))
                {
                    args = new object[] { initialTime };
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
                {
                    args = new object[] { (int)initialTime };
                }
                else if (parameters.Length == 2)
                {
                    args = new object[] { initialTime, 0 };
                }
                else
                {
                    args = Array.Empty<object>();
                }

                return (T)constructor.Invoke(args);
            }
        }

        /// <summary>
        /// Returns a timer to the pool for reuse.
        /// </summary>
        /// <param name="timer">Timer to return to pool.</param>
        public static void Release(Timer timer)
        {
            if (timer == null) return;
            
            var type = timer.GetType();
            
            // Stop and unregister
            timer.Pause();
            TimerManager.UnregisterTimer(timer);
            
            lock (_lockObject)
            {
                if (!_pools.TryGetValue(type, out var pool))
                {
                    pool = new Queue<Timer>(_defaultCapacity);
                    _pools[type] = pool;
                }
                
                // Only add if under max capacity
                if (pool.Count < _maxCapacity)
                {
                    pool.Enqueue(timer);
                }
            }
        }

        /// <summary>
        /// Clears all pooled timers.
        /// </summary>
        public static void Clear()
        {
            lock (_lockObject)
            {
                _pools.Clear();
            }
        }

        /// <summary>
        /// Prewarms the pool with the specified number of timers.
        /// Uses reflection to support any Timer type.
        /// </summary>
        /// <typeparam name="T">Timer type to prewarm.</typeparam>
        /// <param name="count">Number of timers to create.</param>
        public static void Prewarm<T>(int count) where T : Timer
        {
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var timer = CreateTimer<T>(1f);
                    TimerManager.UnregisterTimer(timer); // Don't keep registered
                    Release(timer);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[TimerPool] Failed to prewarm {typeof(T).Name}: {e.Message}");
                    break;
                }
            }
        }

        /// <summary>
        /// Prewarms the pool for a timer type specified at runtime.
        /// </summary>
        /// <param name="timerType">Type of timer to prewarm.</param>
        /// <param name="count">Number of timers to create.</param>
        public static void Prewarm(Type timerType, int count)
        {
            if (timerType == null || !typeof(Timer).IsAssignableFrom(timerType)) return;
            
            var method = typeof(TimerPool).GetMethod(nameof(Prewarm), new[] { typeof(int) });
            var genericMethod = method.MakeGenericMethod(timerType);
            genericMethod.Invoke(null, new object[] { count });
        }

        /// <summary>
        /// Gets the current pool size for a timer type.
        /// </summary>
        public static int GetPoolSize<T>() where T : Timer
        {
            lock (_lockObject)
            {
                if (_pools.TryGetValue(typeof(T), out var pool))
                {
                    return pool.Count;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets total number of pooled timers across all types.
        /// </summary>
        public static int TotalPooledCount
        {
            get
            {
                int count = 0;
                lock (_lockObject)
                {
                    foreach (var pool in _pools.Values)
                    {
                        count += pool.Count;
                    }
                }
                return count;
            }
        }
    }
}
