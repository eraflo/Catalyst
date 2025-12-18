using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Eraflo.UnityImportPackage.Utilities;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Serializable data for a single timer.
    /// </summary>
    [Serializable]
    public class TimerSaveData
    {
        public string TypeName;
        public string AssemblyQualifiedTypeName;
        public float Duration;
        public float CurrentTime;
        public float TimeScale;
        public bool IsRunning;
        public bool UseUnscaledTime;
        public List<SerializableCallback> Callbacks = new List<SerializableCallback>();
    }

    /// <summary>
    /// Serializable data for all timers.
    /// </summary>
    [Serializable]
    public class TimerPersistenceData
    {
        public List<TimerSaveData> Timers = new List<TimerSaveData>();
    }

    /// <summary>
    /// Handles saving and loading of timer state with Newtonsoft JSON.
    /// Callbacks are restored via reflection if they reference named methods on Unity Objects.
    /// Supports custom timer types via dynamic type resolution.
    /// </summary>
    public static class TimerPersistence
    {
        private static readonly Dictionary<uint, List<SerializableCallback>> _callbackRegistry = new Dictionary<uint, List<SerializableCallback>>();
        private static readonly Dictionary<string, Type> _timerTypeCache = new Dictionary<string, Type>();

        /// <summary>
        /// Registers callback metadata for a timer.
        /// </summary>
        internal static void RegisterCallback(uint timerId, SerializableCallback callback)
        {
            if (callback == null) return;
            
            if (!_callbackRegistry.TryGetValue(timerId, out var list))
            {
                list = new List<SerializableCallback>();
                _callbackRegistry[timerId] = list;
            }
            list.Add(callback);
        }

        /// <summary>
        /// Removes all callback metadata for a timer.
        /// </summary>
        internal static void UnregisterCallbacks(uint timerId)
        {
            _callbackRegistry.Remove(timerId);
        }

        /// <summary>
        /// Gets callback metadata for a timer.
        /// </summary>
        internal static List<SerializableCallback> GetCallbacks(uint timerId)
        {
            return _callbackRegistry.TryGetValue(timerId, out var list) ? list : new List<SerializableCallback>();
        }

        /// <summary>
        /// Saves all active timers to JSON.
        /// </summary>
        public static string SaveAll()
        {
            var data = new TimerPersistenceData();
            var activeTimers = Timer.GetActiveTimers();

            foreach (var timerInfo in activeTimers)
            {
                var timerType = GetTimerType(timerInfo.TypeName);
                
                var saveData = new TimerSaveData
                {
                    TypeName = timerInfo.TypeName,
                    AssemblyQualifiedTypeName = timerType?.AssemblyQualifiedName ?? "",
                    Duration = timerInfo.InitialTime,
                    CurrentTime = timerInfo.CurrentTime,
                    TimeScale = timerInfo.TimeScale,
                    IsRunning = timerInfo.IsRunning,
                    UseUnscaledTime = false,
                    Callbacks = GetCallbacks(timerInfo.Id)
                };
                data.Timers.Add(saveData);
            }

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        /// <summary>
        /// Loads timers from JSON and restores callbacks.
        /// </summary>
        public static List<TimerHandle> LoadAll(string json)
        {
            var handles = new List<TimerHandle>();
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[TimerPersistence] Empty JSON provided");
                return handles;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<TimerPersistenceData>(json);
                if (data?.Timers == null) return handles;

                foreach (var timerData in data.Timers)
                {
                    var handle = RestoreTimer(timerData);
                    if (handle.IsValid)
                    {
                        handles.Add(handle);
                        RestoreCallbacks(handle, timerData.Callbacks);
                    }
                }

                Debug.Log($"[TimerPersistence] Restored {handles.Count} timers");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TimerPersistence] Failed to load: {ex.Message}");
            }

            return handles;
        }

        private static TimerHandle RestoreTimer(TimerSaveData data)
        {
            Type timerType = null;
            
            // Try assembly-qualified name first
            if (!string.IsNullOrEmpty(data.AssemblyQualifiedTypeName))
            {
                timerType = Type.GetType(data.AssemblyQualifiedTypeName);
            }
            
            // Fallback to searching by simple name
            if (timerType == null)
            {
                timerType = GetTimerType(data.TypeName);
            }

            if (timerType == null)
            {
                Debug.LogWarning($"[TimerPersistence] Could not find timer type: {data.TypeName}");
                return TimerHandle.None;
            }

            if (!typeof(ITimer).IsAssignableFrom(timerType))
            {
                Debug.LogWarning($"[TimerPersistence] Type {data.TypeName} does not implement ITimer");
                return TimerHandle.None;
            }

            var config = new TimerConfig
            {
                Duration = data.Duration,
                TimeScale = data.TimeScale,
                UseUnscaledTime = data.UseUnscaledTime
            };

            try
            {
                var method = typeof(Timer).GetMethod("Create", new[] { typeof(TimerConfig) });
                var generic = method.MakeGenericMethod(timerType);
                var handle = (TimerHandle)generic.Invoke(null, new object[] { config });

                if (!data.IsRunning)
                {
                    Timer.Pause(handle);
                }

                return handle;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TimerPersistence] Failed to create timer {data.TypeName}: {ex.Message}");
                return TimerHandle.None;
            }
        }

        /// <summary>
        /// Finds a timer type by name, searching all loaded assemblies.
        /// </summary>
        private static Type GetTimerType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            if (_timerTypeCache.TryGetValue(typeName, out var cached))
                return cached;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == typeName && typeof(ITimer).IsAssignableFrom(type))
                        {
                            _timerTypeCache[typeName] = type;
                            return type;
                        }
                    }
                }
                catch { /* Some assemblies may not allow reflection */ }
            }

            return null;
        }

        private static void RestoreCallbacks(TimerHandle handle, List<SerializableCallback> callbacks)
        {
            if (callbacks == null) return;

            foreach (var callback in callbacks)
            {
                var del = callback.ToDelegate();
                if (del == null) continue;

                var callbackType = Type.GetType(callback.CallbackTypeName);
                if (callbackType == null)
                {
                    Debug.LogWarning($"[TimerPersistence] Unknown callback type: {callback.CallbackTypeName}");
                    continue;
                }

                try
                {
                    if (callback.HasParameter)
                    {
                        var paramType = Type.GetType(callback.ParameterTypeName);
                        var actionType = typeof(Action<>).MakeGenericType(paramType);
                        var registerMethod = typeof(TimerCallbacks).GetMethod("Register", new[] { typeof(TimerHandle), actionType });
                        if (registerMethod != null)
                        {
                            var genericRegister = registerMethod.MakeGenericMethod(callbackType, paramType);
                            genericRegister.Invoke(null, new object[] { handle, del });
                        }
                    }
                    else
                    {
                        var registerMethod = typeof(TimerCallbacks).GetMethod("Register", new[] { typeof(TimerHandle), typeof(Action) });
                        if (registerMethod != null)
                        {
                            var genericRegister = registerMethod.MakeGenericMethod(callbackType);
                            genericRegister.Invoke(null, new object[] { handle, del });
                        }
                    }

                    RegisterCallback(handle.Id, callback);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TimerPersistence] Failed to restore callback: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clears all callback registry and type cache.
        /// </summary>
        public static void Clear()
        {
            _callbackRegistry.Clear();
            _timerTypeCache.Clear();
        }
    }
}
