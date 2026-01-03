using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Custom PlayerLoop systems for ProceduralAnimation.
    /// Integrates animation updates into Unity's player loop for precise timing control.
    /// </summary>
    public static class ProceduralAnimationLoop
    {
        /// <summary>
        /// Marker struct for the ProceduralAnimation update phase.
        /// Runs after FixedUpdate but before Update.
        /// </summary>
        public struct ProceduralAnimationUpdate { }
        
        /// <summary>
        /// Marker struct for the ProceduralAnimation late update phase.
        /// Runs after LateUpdate for final adjustments.
        /// </summary>
        public struct ProceduralAnimationLateUpdate { }
        
        /// <summary>
        /// Marker struct for the ProceduralAnimation fixed update phase.
        /// Runs during FixedUpdate for physics-based animation.
        /// </summary>
        public struct ProceduralAnimationFixedUpdate { }
        
        private static bool _isInitialized;
        
        private static readonly List<Action<float>> _updateCallbacks = new List<Action<float>>();
        private static readonly List<Action<float>> _lateUpdateCallbacks = new List<Action<float>>();
        private static readonly List<Action<float>> _fixedUpdateCallbacks = new List<Action<float>>();
        
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Initializes the custom player loop systems.
        /// Called automatically on domain reload.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (_isInitialized) return;
            
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            
            // Insert our custom update phases
            InsertSystem<FixedUpdate>(ref playerLoop, 
                new PlayerLoopSystem
                {
                    type = typeof(ProceduralAnimationFixedUpdate),
                    updateDelegate = OnFixedUpdate
                });
            
            InsertSystem<Update>(ref playerLoop,
                new PlayerLoopSystem
                {
                    type = typeof(ProceduralAnimationUpdate),
                    updateDelegate = OnUpdate
                });
            
            InsertSystem<PostLateUpdate>(ref playerLoop,
                new PlayerLoopSystem
                {
                    type = typeof(ProceduralAnimationLateUpdate),
                    updateDelegate = OnLateUpdate
                });
            
            PlayerLoop.SetPlayerLoop(playerLoop);
            _isInitialized = true;
            
            Debug.Log("[ProceduralAnimation] Player loop systems initialized.");
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    Shutdown();
                }
            };
        }
#endif
        
        /// <summary>
        /// Shuts down the custom player loop systems.
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                _updateCallbacks.Clear();
                _lateUpdateCallbacks.Clear();
                _fixedUpdateCallbacks.Clear();
            }
            _isInitialized = false;
        }
        
        /// <summary>
        /// Registers a callback to be called during the ProceduralAnimation Update phase.
        /// </summary>
        /// <param name="callback">Callback receiving deltaTime.</param>
        public static void RegisterUpdate(Action<float> callback)
        {
            if (callback == null) return;
            lock (_lock)
            {
                if (!_updateCallbacks.Contains(callback))
                    _updateCallbacks.Add(callback);
            }
        }
        
        /// <summary>
        /// Unregisters an Update callback.
        /// </summary>
        public static void UnregisterUpdate(Action<float> callback)
        {
            if (callback == null) return;
            lock (_lock)
            {
                _updateCallbacks.Remove(callback);
            }
        }
        
        /// <summary>
        /// Registers a callback to be called during the ProceduralAnimation LateUpdate phase.
        /// </summary>
        public static void RegisterLateUpdate(Action<float> callback)
        {
            if (callback == null) return;
            lock (_lock)
            {
                if (!_lateUpdateCallbacks.Contains(callback))
                    _lateUpdateCallbacks.Add(callback);
            }
        }
        
        /// <summary>
        /// Unregisters a LateUpdate callback.
        /// </summary>
        public static void UnregisterLateUpdate(Action<float> callback)
        {
            if (callback == null) return;
            lock (_lock)
            {
                _lateUpdateCallbacks.Remove(callback);
            }
        }
        
        /// <summary>
        /// Registers a callback to be called during the ProceduralAnimation FixedUpdate phase.
        /// </summary>
        public static void RegisterFixedUpdate(Action<float> callback)
        {
            if (callback == null) return;
            lock (_lock)
            {
                if (!_fixedUpdateCallbacks.Contains(callback))
                    _fixedUpdateCallbacks.Add(callback);
            }
        }
        
        /// <summary>
        /// Unregisters a FixedUpdate callback.
        /// </summary>
        public static void UnregisterFixedUpdate(Action<float> callback)
        {
            if (callback == null) return;
            lock (_lock)
            {
                _fixedUpdateCallbacks.Remove(callback);
            }
        }
        
        private static void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            
            Action<float>[] snapshot;
            lock (_lock)
            {
                snapshot = _updateCallbacks.ToArray();
            }
            
            foreach (var callback in snapshot)
            {
                try
                {
                    callback?.Invoke(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        
        private static void OnLateUpdate()
        {
            float deltaTime = Time.deltaTime;
            
            Action<float>[] snapshot;
            lock (_lock)
            {
                snapshot = _lateUpdateCallbacks.ToArray();
            }
            
            foreach (var callback in snapshot)
            {
                try
                {
                    callback?.Invoke(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        
        private static void OnFixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            
            Action<float>[] snapshot;
            lock (_lock)
            {
                snapshot = _fixedUpdateCallbacks.ToArray();
            }
            
            foreach (var callback in snapshot)
            {
                try
                {
                    callback?.Invoke(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        
        private static bool InsertSystem<TBefore>(ref PlayerLoopSystem loop, PlayerLoopSystem systemToInsert)
        {
            if (loop.subSystemList == null)
                return false;
            
            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type == typeof(TBefore))
                {
                    // Insert at the end of the target system's subsystems
                    var targetSystem = loop.subSystemList[i];
                    var subSystems = targetSystem.subSystemList ?? Array.Empty<PlayerLoopSystem>();
                    var newSubSystems = new PlayerLoopSystem[subSystems.Length + 1];
                    Array.Copy(subSystems, newSubSystems, subSystems.Length);
                    newSubSystems[subSystems.Length] = systemToInsert;
                    targetSystem.subSystemList = newSubSystems;
                    loop.subSystemList[i] = targetSystem;
                    return true;
                }
                
                if (InsertSystem<TBefore>(ref loop.subSystemList[i], systemToInsert))
                    return true;
            }
            
            return false;
        }
    }
}
