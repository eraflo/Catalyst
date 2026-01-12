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
        private readonly Dictionary<TimerHandle, NetworkTimerData> _timers = new Dictionary<TimerHandle, NetworkTimerData>();
        private readonly Dictionary<uint, TimerHandle> _idToHandle = new Dictionary<uint, TimerHandle>();
        private uint _nextId = 1;
        private bool _connected;

        /// <summary>Fired when a networked timer ticks.</summary>
        public event Action<uint, float> OnTick;

        /// <summary>Fired when a networked timer completes.</summary>
        public event Action<uint> OnComplete;

        public void OnRegistered()
        {
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
            if (!handle.IsValid) return 0;

            if (id == 0) id = _nextId++;
            
            _timers[handle] = new NetworkTimerData { Id = id, Authority = authority };
            _idToHandle[id] = handle;

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
            if (_timers.TryGetValue(handle, out var data))
            {
                _idToHandle.Remove(data.Id);
                _timers.Remove(handle);
            }
        }

        /// <summary>
        /// Gets the network ID for a timer.
        /// </summary>
        public uint GetId(TimerHandle handle)
            => _timers.TryGetValue(handle, out var d) ? d.Id : 0;

        /// <summary>
        /// Gets the handle for a network ID.
        /// </summary>
        public TimerHandle GetHandle(uint id)
            => _idToHandle.TryGetValue(id, out var h) ? h : TimerHandle.None;

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
                // Only broadcast if we have authority
                if (ownership != null && !ownership.HasAuthority(kvp.Value.Id, kvp.Value.Authority))
                    continue;

                var msg = new Networking.TimerSyncMessage
                {
                    NetworkId = kvp.Value.Id,
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
            if (!_idToHandle.TryGetValue(msg.NetworkId, out var handle)) return;
            if (!_timers.TryGetValue(handle, out var data)) return;

            // Don't apply sync if we have authority (we are the source)
            var ownership = App.Get<Networking.NetworkOwnershipManager>();
            if (ownership != null && ownership.HasAuthority(msg.NetworkId, data.Authority))
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
            _idToHandle.Clear();
        }

        private struct NetworkTimerData
        {
            public uint Id;
            public Networking.AuthorityMode Authority;
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
