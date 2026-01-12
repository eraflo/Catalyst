using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Timers
{
    /// <summary>
    /// Handles network synchronization for timers.
    /// </summary>
    public class TimerNetworkHandler : Networking.INetworkMessageHandler
    {
        private readonly Dictionary<TimerHandle, Networking.AuthorityMode> _timers = new Dictionary<TimerHandle, Networking.AuthorityMode>();
        private Networking.NetworkIdManager _idManager;
        private uint _nextId = 1;
        private bool _connected;

        /// <summary>Fired when a networked timer ticks.</summary>
        public event Action<uint, float> OnTick;

        /// <summary>Fired when a networked timer completes.</summary>
        public event Action<uint> OnComplete;

        public void OnRegistered()
        {
            _idManager = App.Get<Networking.NetworkIdManager>();
            var network = App.Get<Networking.NetworkManager>();
            network.On<Networking.TimerSyncMessage>(HandleSync);
            network.On<Networking.TimerCancelMessage>(HandleCancel);
        }

        public void OnUnregistered()
        {
            var network = App.Get<Networking.NetworkManager>();
            network.Off<Networking.TimerSyncMessage>(HandleSync);
            network.Off<Networking.TimerCancelMessage>(HandleCancel);
            Clear();
        }

        public void OnNetworkConnected() => _connected = true;
        public void OnNetworkDisconnected() => _connected = false;

        /// <summary>
        /// Makes a timer networked.
        /// </summary>
        public uint MakeNetworked(TimerHandle handle, Networking.AuthorityMode authority = Networking.AuthorityMode.ServerAuthoritative, uint id = 0)
        {
            if (!handle.IsValid || _idManager == null) return 0;

            if (id == 0) id = _nextId++;
            
            _timers[handle] = authority;
            _idManager.Register(id, handle);

            var timer = App.Get<Timer>();
            timer.On<OnComplete>(handle, () => HandleComplete(handle, id));
            timer.On<OnTick, float>(handle, dt => OnTick?.Invoke(id, dt));

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
            var network = App.Get<Networking.NetworkManager>();
            if (network == null || !network.IsConnected || !network.IsServer) return;

            var timer = App.Get<Timer>();
            var ownership = App.Get<Networking.NetworkOwnershipManager>();
            
            foreach (var kvp in _timers)
            {
                uint id = GetId(kvp.Key);
                // Only broadcast if we have authority
                if (ownership != null && !ownership.HasAuthority(id, kvp.Value))
                    continue;

                var msg = new Networking.TimerSyncMessage
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

        private void HandleSync(Networking.TimerSyncMessage msg)
        {
            var handle = GetHandle(msg.NetworkId);
            if (!handle.IsValid) return;
            if (!_timers.TryGetValue(handle, out var authority)) return;

            // Don't apply sync if we have authority (we are the source)
            var ownership = App.Get<Networking.NetworkOwnershipManager>();
            if (ownership != null && ownership.HasAuthority(msg.NetworkId, authority))
                return;

            var timer = App.Get<Timer>();
            if (msg.IsFinished)
            {
                timer.CancelTimer(handle);
            }
            else if (msg.IsRunning && !timer.IsRunning(handle))
            {
                timer.Resume(handle);
            }
            else if (!msg.IsRunning && timer.IsRunning(handle))
            {
                timer.Pause(handle);
            }
        }

        private void HandleCancel(Networking.TimerCancelMessage msg)
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
