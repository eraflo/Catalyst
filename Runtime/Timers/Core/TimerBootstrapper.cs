using System;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Bootstraps the timer system by injecting a custom update into Unity's Player Loop.
    /// This allows timers to run without requiring a MonoBehaviour.
    /// </summary>
    public static class TimerBootstrapper
    {
        private static bool _initialized;

        /// <summary>
        /// Marker struct for the Timer update in the Player Loop.
        /// </summary>
        private struct TimerUpdate { }

        /// <summary>
        /// Initializes the timer system by injecting into the Player Loop.
        /// Called automatically at runtime.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            if (_initialized) return;

            // Capture main thread ID
            TimerManager.MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            var currentLoop = PlayerLoop.GetCurrentPlayerLoop();
            
            if (!InsertTimerUpdate(ref currentLoop))
            {
                Debug.LogError("[TimerBootstrapper] Failed to insert timer update into Player Loop.");
                return;
            }

            PlayerLoop.SetPlayerLoop(currentLoop);
            _initialized = true;

            // Initialize timer pool from settings
            InitializeTimerPool();

            // Clean up on application quit
            Application.quitting += OnApplicationQuit;

#if UNITY_EDITOR
            // Reset when exiting play mode in editor
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        /// <summary>
        /// Initializes the timer pool using PackageSettings configuration.
        /// Uses reflection to discover all Timer types.
        /// </summary>
        private static void InitializeTimerPool()
        {
            try
            {
                var settings = PackageSettings.Instance;
                
                TimerPool.DefaultCapacity = settings.TimerPoolDefaultCapacity;
                TimerPool.MaxCapacity = settings.TimerPoolMaxCapacity;

                if (settings.EnableTimerPooling && settings.TimerPoolPrewarmCount > 0)
                {
                    PrewarmAllTimerTypes(settings.TimerPoolPrewarmCount, settings.EnableTimerDebugLogs);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TimerBootstrapper] Failed to initialize timer pool: {e.Message}");
            }
        }

        /// <summary>
        /// Discovers all non-abstract Timer subclasses and prewarms them.
        /// </summary>
        private static void PrewarmAllTimerTypes(int count, bool debugLog)
        {
            var timerBaseType = typeof(Timer);
            var prewarmedTypes = new System.Collections.Generic.List<string>();

            // Search all loaded assemblies for Timer subclasses
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        // Skip abstract classes, interfaces, and the base Timer class
                        if (type.IsAbstract || type.IsInterface || type == timerBaseType)
                            continue;

                        // Check if it's a Timer subclass
                        if (!timerBaseType.IsAssignableFrom(type))
                            continue;

                        // Try to prewarm this timer type
                        try
                        {
                            TimerPool.Prewarm(type, count);
                            prewarmedTypes.Add(type.Name);
                        }
                        catch
                        {
                            // Skip types that can't be instantiated
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be reflected
                }
            }

            if (debugLog && prewarmedTypes.Count > 0)
            {
                Debug.Log($"[TimerPool] Prewarmed {count} timers for: {string.Join(", ", prewarmedTypes)}");
            }
        }

        /// <summary>
        /// Inserts the timer update system after Unity's Update phase.
        /// </summary>
        private static bool InsertTimerUpdate(ref PlayerLoopSystem loop)
        {
            var timerSystem = new PlayerLoopSystem
            {
                type = typeof(TimerUpdate),
                updateDelegate = TimerManager.UpdateTimers
            };

            // Find and modify the Update subsystem
            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type == typeof(Update))
                {
                    var updateSystem = loop.subSystemList[i];
                    var subsystems = updateSystem.subSystemList;
                    
                    // Create new array with space for timer update
                    var newSubsystems = new PlayerLoopSystem[subsystems.Length + 1];
                    
                    // Find ScriptRunBehaviourUpdate and insert after it
                    int insertIndex = -1;
                    for (int j = 0; j < subsystems.Length; j++)
                    {
                        newSubsystems[j] = subsystems[j];
                        if (subsystems[j].type == typeof(Update.ScriptRunBehaviourUpdate))
                        {
                            insertIndex = j + 1;
                        }
                    }

                    if (insertIndex == -1)
                    {
                        // Fallback: add at the end
                        insertIndex = subsystems.Length;
                    }

                    // Shift elements and insert timer system
                    for (int j = newSubsystems.Length - 1; j > insertIndex; j--)
                    {
                        newSubsystems[j] = newSubsystems[j - 1];
                    }
                    newSubsystems[insertIndex] = timerSystem;

                    updateSystem.subSystemList = newSubsystems;
                    loop.subSystemList[i] = updateSystem;
                    return true;
                }
            }

            return false;
        }

        private static void OnApplicationQuit()
        {
            TimerManager.Clear();
            _initialized = false;
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                TimerManager.Clear();
                _initialized = false;
            }
        }
#endif
    }
}
