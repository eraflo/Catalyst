using System;
using System.Collections.Generic;
using Eraflo.Catalyst.Networking;
using UnityEngine;

namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Handles network synchronization for timers.
    /// </summary>
    public class TimerNetworkHandler : INetworkUpdatable
    {
        private readonly Dictionary<TimerHandle, AuthorityMode> _timers = new Dictionary<TimerHandle, AuthorityMode>();
        private NetworkIdManager _idManager;
        private uint _nextId = 1;
        private bool _connected;

        private float _syncTimer = 0f;
        private const float SyncInterval = 0.5f; // Sync twice per second

        /// <summary>Fired when a networked timer ticks.</summary>
        public event Action<uint, float> OnTick;

        /// <summary>Fired when a networked timer completes.</summary>
        public event Action<uint> OnComplete;

        /// <summary>Fired when a networked timer is registered/created.</summary>
        public event Action<uint, TimerHandle> OnTimerRegistered;

        public void OnRegistered()
        {
            _idManager = App.Get<NetworkIdManager>();
            var network = App.Get<NetworkManager>();
            network.On<TimerCreateMessage>(HandleCreate);
            network.On<TimerSyncMessage>(HandleSync);
            network.On<TimerCancelMessage>(HandleCancel);
        }

        public void OnUnregistered()
        {
            var network = App.Get<NetworkManager>();
            if (network != null)
            {
                network.Off<TimerCreateMessage>(HandleCreate);
                network.Off<TimerSyncMessage>(HandleSync);
                network.Off<TimerCancelMessage>(HandleCancel);
            }
            Clear();
        }

        public void OnNetworkConnected() => _connected = true;
        public void OnNetworkDisconnected() => _connected = false;

        public void OnUpdate()
        {
            if (!_connected) return;

            var network = App.Get<NetworkManager>();
            if (network != null && network.IsServer)
            {
                _syncTimer += Time.deltaTime;
                if (_syncTimer >= SyncInterval)
                {
                    _syncTimer = 0f;
                    BroadcastSync();
                }
            }
        }

        /// <summary>
        /// Makes a timer networked.
        /// </summary>
        public uint MakeNetworked(TimerHandle handle, AuthorityMode authority = AuthorityMode.ServerAuthoritative, uint id = 0)
        {
            if (!handle.IsValid || _idManager == null) return 0;

            if (id == 0) id = _nextId++;

            _timers[handle] = authority;
            _idManager.Register(id, handle);

            OnTimerRegistered?.Invoke(id, handle);

            var timer = App.Get<Timer>();
            timer.On<OnComplete>(handle, () => HandleComplete(handle, id));
            timer.On<OnTick, float>(handle, dt => OnTick?.Invoke(id, dt));

            // Server: Broadcast creation to all clients
            var network = App.Get<NetworkManager>();
            if (network != null && network.IsServer && network.IsConnected)
            {
                var msg = new TimerCreateMessage
                {
                    NetworkId = id,
                    Duration = timer.GetCurrentTime(handle),
                    TimerType = timer.Backend.GetTimerType(handle)
                };
                network.SendToClients(msg);
            }

            return id;
        }

        /// <summary>
        /// Removes networking from a timer.
        /// </summary>
        public void Remove(TimerHandle handle)
        {
            if (_timers.Remove(handle))
            {
                _idManager?.Unregister(handle);
            }
        }

        /// <summary>
        /// Gets the network ID for a timer.
        /// </summary>
        public uint GetId(TimerHandle handle)
            => _idManager?.GetId(handle) ?? 0;

        /// <summary>
        /// Gets the handle for a network ID.
        /// </summary>
        public TimerHandle GetHandle(uint id)
            => _idManager?.GetObject<TimerHandle>(id) ?? TimerHandle.None;

        /// <summary>
        /// Broadcasts sync data for all server-authoritative timers.
        /// </summary>
        public void BroadcastSync()
        {
            var network = App.Get<NetworkManager>();
            if (network == null || !network.IsConnected || !network.IsServer) return;

            var timer = App.Get<Timer>();
            var ownership = App.Get<NetworkOwnershipManager>();

            foreach (var kvp in _timers)
            {
                uint id = GetId(kvp.Key);
                // Only broadcast if we have authority
                if (ownership != null && !ownership.HasAuthority(id, kvp.Value))
                    continue;

                var msg = new TimerSyncMessage
                {
                    NetworkId = id,
                    RemainingTime = timer.GetCurrentTime(kvp.Key),
                    Progress = timer.GetProgress(kvp.Key),
                    IsRunning = timer.IsRunning(kvp.Key),
                    IsFinished = timer.IsFinished(kvp.Key)
                };
                network.SendToClients(msg);
            }
        }

        private void HandleComplete(TimerHandle handle, uint id)
        {
            OnComplete?.Invoke(id);
            Remove(handle);
        }

        private void HandleSync(TimerSyncMessage msg)
        {
            var handle = GetHandle(msg.NetworkId);
            if (!handle.IsValid) return;
            if (!_timers.TryGetValue(handle, out var authority)) return;

            // Don't apply sync if we have authority (we are the source)
            var ownership = App.Get<NetworkOwnershipManager>();
            if (ownership != null && ownership.HasAuthority(msg.NetworkId, authority))
                return;

            var timer = App.Get<Timer>();
            if (msg.IsFinished)
            {
                timer.CancelTimer(handle);
            }
            else
            {
                // Sync values
                timer.SetCurrentTime(handle, msg.RemainingTime);

                if (msg.IsRunning && !timer.IsRunning(handle))
                {
                    timer.Resume(handle);
                }
                else if (!msg.IsRunning && timer.IsRunning(handle))
                {
                    timer.Pause(handle);
                }
            }
        }

        private void HandleCreate(TimerCreateMessage msg)
        {
            var network = App.Get<NetworkManager>();
            if (network.IsServer) return; // Server initiated it

            // If already exists, ignore
            if (GetHandle(msg.NetworkId).IsValid) return;

            // Resolve the timer type
            Type timerType = null;
            if (!string.IsNullOrEmpty(msg.TimerType))
            {
                timerType = Type.GetType(msg.TimerType);
            }

            // Default to CountdownTimer if resolution fails or type is empty (backward compatibility/safety)
            if (timerType == null)
            {
                timerType = typeof(CountdownTimer);
            }

            // Instantiate local timer via reflection to call generic CreateTimer<T>
            var timer = App.Get<Timer>();
            var method = typeof(Timer).GetMethod(nameof(Timer.CreateTimer), new[] { typeof(float) });
            var generic = method.MakeGenericMethod(timerType);
            var handle = (TimerHandle)generic.Invoke(timer, new object[] { msg.Duration });

            // Register it
            MakeNetworked(handle, AuthorityMode.ServerAuthoritative, msg.NetworkId);
        }

        private void HandleCancel(TimerCancelMessage msg)
        {
            var handle = GetHandle(msg.NetworkId);
            if (handle.IsValid) App.Get<Timer>().CancelTimer(handle);
        }

        /// <summary>
        /// Clears all data.
        /// </summary>
        public void Clear()
        {
            _timers.Clear();
            _nextId = 1;
        }

    }

    /// <summary>
    /// Network sync data for a timer.
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
}
