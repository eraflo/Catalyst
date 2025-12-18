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

            var currentLoop = PlayerLoop.GetCurrentPlayerLoop();
            
            if (!InsertTimerUpdate(ref currentLoop))
            {
                Debug.LogError("[TimerBootstrapper] Failed to insert timer update into Player Loop.");
                return;
            }

            PlayerLoop.SetPlayerLoop(currentLoop);
            _initialized = true;

            // Initialize the Timer system
            Timer.Initialize();

            // Clean up on application quit
            Application.quitting += OnApplicationQuit;

#if UNITY_EDITOR
            // Reset when exiting play mode in editor
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        /// <summary>
        /// Inserts the timer update into the Player Loop after the Update phase.
        /// </summary>
        private static bool InsertTimerUpdate(ref PlayerLoopSystem loop)
        {
            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type == typeof(Update))
                {
                    var updateSystem = loop.subSystemList[i];
                    var subsystems = updateSystem.subSystemList ?? Array.Empty<PlayerLoopSystem>();

                    // Check if already inserted
                    foreach (var sub in subsystems)
                    {
                        if (sub.type == typeof(TimerUpdate)) return true;
                    }

                    // Insert our update
                    var newSubsystems = new PlayerLoopSystem[subsystems.Length + 1];
                    Array.Copy(subsystems, newSubsystems, subsystems.Length);
                    newSubsystems[subsystems.Length] = new PlayerLoopSystem
                    {
                        type = typeof(TimerUpdate),
                        updateDelegate = OnTimerUpdate
                    };

                    updateSystem.subSystemList = newSubsystems;
                    loop.subSystemList[i] = updateSystem;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Called every frame by the Player Loop.
        /// </summary>
        private static void OnTimerUpdate()
        {
            Timer.Update();
        }

        /// <summary>
        /// Cleans up when the application quits.
        /// </summary>
        private static void OnApplicationQuit()
        {
            Timer.Shutdown();
            _initialized = false;
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Timer.Shutdown();
                _initialized = false;
            }
        }
#endif
    }
}
