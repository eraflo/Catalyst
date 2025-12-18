using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Network synchronization layer for timers.
    /// Can be applied to ANY timer type (including custom ones).
    /// Use Timer.MakeNetworked() to wrap an existing timer.
    /// </summary>
    public static class NetworkTimerSync
    {
        private static readonly Dictionary<TimerHandle, NetworkTimerData> _networkData = new Dictionary<TimerHandle, NetworkTimerData>();
        private static readonly Dictionary<uint, TimerHandle> _networkIdToHandle = new Dictionary<uint, TimerHandle>();
        private static uint _nextNetworkId = 1;

        /// <summary>
        /// Callback invoked when a timer's network state needs to be sent.
        /// Implement this to send data over your network solution.
        /// </summary>
        public static Action<NetworkTimerSyncData> OnSendSyncData;

        /// <summary>
        /// Callback invoked when any networked timer ticks.
        /// </summary>
        public static Action<uint, float> OnNetworkTimerTick;

        /// <summary>
        /// Callback invoked when any networked timer completes.
        /// </summary>
        public static Action<uint> OnNetworkTimerComplete;

        /// <summary>
        /// Wraps an existing timer with network synchronization.
        /// </summary>
        /// <param name="handle">Timer handle to make networked.</param>
        /// <param name="isServerAuthoritative">If true, server controls this timer.</param>
        /// <param name="networkId">Optional custom network ID. Auto-generated if 0.</param>
        /// <returns>The network ID for this timer.</returns>
        public static uint MakeNetworked(TimerHandle handle, bool isServerAuthoritative = true, uint networkId = 0)
        {
            if (!handle.IsValid) return 0;

            if (networkId == 0)
            {
                networkId = _nextNetworkId++;
            }

            var data = new NetworkTimerData
            {
                NetworkId = networkId,
                IsServerAuthoritative = isServerAuthoritative,
                LastSyncTime = Time.realtimeSinceStartup
            };

            _networkData[handle] = data;
            _networkIdToHandle[networkId] = handle;

            // Register callbacks
            Timer.On<OnComplete>(handle, () => HandleNetworkComplete(handle, networkId));
            Timer.On<OnTick, float>(handle, (dt) => HandleNetworkTick(handle, networkId, dt));

            return networkId;
        }

        /// <summary>
        /// Removes network synchronization from a timer.
        /// </summary>
        public static void RemoveNetworked(TimerHandle handle)
        {
            if (_networkData.TryGetValue(handle, out var data))
            {
                _networkIdToHandle.Remove(data.NetworkId);
                _networkData.Remove(handle);
            }
        }

        /// <summary>
        /// Gets the network ID for a timer.
        /// </summary>
        public static uint GetNetworkId(TimerHandle handle)
        {
            return _networkData.TryGetValue(handle, out var data) ? data.NetworkId : 0;
        }

        /// <summary>
        /// Gets the timer handle for a network ID.
        /// </summary>
        public static TimerHandle GetHandle(uint networkId)
        {
            return _networkIdToHandle.TryGetValue(networkId, out var handle) ? handle : TimerHandle.None;
        }

        /// <summary>
        /// Checks if a timer is networked.
        /// </summary>
        public static bool IsNetworked(TimerHandle handle)
        {
            return _networkData.ContainsKey(handle);
        }

        /// <summary>
        /// Gets sync data for a timer (to send to clients).
        /// </summary>
        public static NetworkTimerSyncData GetSyncData(TimerHandle handle)
        {
            if (!_networkData.TryGetValue(handle, out var data))
            {
                return default;
            }

            return new NetworkTimerSyncData
            {
                NetworkId = data.NetworkId,
                RemainingTime = Timer.GetCurrentTime(handle),
                Progress = Timer.GetProgress(handle),
                IsRunning = Timer.IsRunning(handle),
                IsFinished = Timer.IsFinished(handle),
                IsServerAuthoritative = data.IsServerAuthoritative
            };
        }

        /// <summary>
        /// Applies sync data received from server to a timer.
        /// </summary>
        public static void ApplySyncData(NetworkTimerSyncData syncData)
        {
            if (!_networkIdToHandle.TryGetValue(syncData.NetworkId, out var handle))
            {
                Debug.LogWarning($"[NetworkTimerSync] No timer found for network ID {syncData.NetworkId}");
                return;
            }

            // Only apply if this is a client (server authoritative timer)
            if (_networkData.TryGetValue(handle, out var data) && data.IsServerAuthoritative)
            {
                // Note: We can't directly set CurrentTime on struct timers via the backend.
                // For full sync, we would need the backend to expose a SetCurrentTime method.
                // For now, we sync pause/resume state.
                
                if (syncData.IsRunning && !Timer.IsRunning(handle))
                {
                    Timer.Resume(handle);
                }
                else if (!syncData.IsRunning && Timer.IsRunning(handle))
                {
                    Timer.Pause(handle);
                }

                if (syncData.IsFinished)
                {
                    Timer.Cancel(handle);
                }
            }
        }

        /// <summary>
        /// Broadcast sync data for all server-authoritative timers.
        /// Call this periodically on the server.
        /// </summary>
        public static void BroadcastAllSyncData()
        {
            if (OnSendSyncData == null) return;

            foreach (var kvp in _networkData)
            {
                if (kvp.Value.IsServerAuthoritative)
                {
                    var syncData = GetSyncData(kvp.Key);
                    OnSendSyncData.Invoke(syncData);
                }
            }
        }

        private static void HandleNetworkComplete(TimerHandle handle, uint networkId)
        {
            OnNetworkTimerComplete?.Invoke(networkId);
            RemoveNetworked(handle);
        }

        private static void HandleNetworkTick(TimerHandle handle, uint networkId, float deltaTime)
        {
            OnNetworkTimerTick?.Invoke(networkId, deltaTime);
        }

        /// <summary>
        /// Clears all network timer data.
        /// </summary>
        public static void Clear()
        {
            _networkData.Clear();
            _networkIdToHandle.Clear();
        }

        private struct NetworkTimerData
        {
            public uint NetworkId;
            public bool IsServerAuthoritative;
            public float LastSyncTime;
        }
    }

    /// <summary>
    /// Data structure for network synchronization.
    /// Serialize this to send timer state over network.
    /// </summary>
    [Serializable]
    public struct NetworkTimerSyncData
    {
        public uint NetworkId;
        public float RemainingTime;
        public float Progress;
        public bool IsRunning;
        public bool IsFinished;
        public bool IsServerAuthoritative;
    }

    // Extension for Timer class
    public static partial class Timer
    {
        /// <summary>
        /// Creates a timer and makes it networked.
        /// </summary>
        /// <typeparam name="T">Timer type implementing ITimer.</typeparam>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="isServerAuthoritative">If true, server controls this timer.</param>
        /// <returns>Tuple of (TimerHandle, NetworkId).</returns>
        public static (TimerHandle Handle, uint NetworkId) CreateNetworked<T>(float duration, bool isServerAuthoritative = true) 
            where T : struct, ITimer
        {
            var handle = Create<T>(duration);
            var networkId = NetworkTimerSync.MakeNetworked(handle, isServerAuthoritative);
            return (handle, networkId);
        }

        /// <summary>
        /// Makes an existing timer networked.
        /// </summary>
        public static uint MakeNetworked(TimerHandle handle, bool isServerAuthoritative = true, uint networkId = 0)
        {
            return NetworkTimerSync.MakeNetworked(handle, isServerAuthoritative, networkId);
        }

        /// <summary>
        /// Checks if a timer is networked.
        /// </summary>
        public static bool IsNetworked(TimerHandle handle)
        {
            return NetworkTimerSync.IsNetworked(handle);
        }

        /// <summary>
        /// Gets the network ID for a timer.
        /// </summary>
        public static uint GetNetworkId(TimerHandle handle)
        {
            return NetworkTimerSync.GetNetworkId(handle);
        }
    }
}
