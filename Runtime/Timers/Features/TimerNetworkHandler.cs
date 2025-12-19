using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Timers
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
            Networking.NetworkManager.On<Networking.TimerSyncMessage>(HandleSync);
            Networking.NetworkManager.On<Networking.TimerCancelMessage>(HandleCancel);
        }

        public void OnUnregistered()
        {
            Networking.NetworkManager.Off<Networking.TimerSyncMessage>(HandleSync);
            Networking.NetworkManager.Off<Networking.TimerCancelMessage>(HandleCancel);
            Clear();
        }

        public void OnNetworkConnected() => _connected = true;
        public void OnNetworkDisconnected() => _connected = false;

        /// <summary>
        /// Makes a timer networked.
        /// </summary>
        public uint MakeNetworked(TimerHandle handle, bool serverAuthoritative = true, uint id = 0)
        {
            if (!handle.IsValid) return 0;

            if (id == 0) id = _nextId++;

            _timers[handle] = new NetworkTimerData { Id = id, ServerAuth = serverAuthoritative };
            _idToHandle[id] = handle;

            Timer.On<OnComplete>(handle, () => HandleComplete(handle, id));
            Timer.On<OnTick, float>(handle, dt => OnTick?.Invoke(id, dt));

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
            if (!Networking.NetworkManager.IsConnected || !Networking.NetworkManager.IsServer) return;

            foreach (var kvp in _timers)
            {
                if (!kvp.Value.ServerAuth) continue;

                var msg = new Networking.TimerSyncMessage
                {
                    NetworkId = kvp.Value.Id,
                    RemainingTime = Timer.GetCurrentTime(kvp.Key),
                    Progress = Timer.GetProgress(kvp.Key),
                    IsRunning = Timer.IsRunning(kvp.Key),
                    IsFinished = Timer.IsFinished(kvp.Key)
                };
                Networking.NetworkManager.SendToClients(msg);
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
            if (!_timers.TryGetValue(handle, out var data) || !data.ServerAuth) return;

            if (msg.IsFinished)
            {
                Timer.Cancel(handle);
            }
            else if (msg.IsRunning && !Timer.IsRunning(handle))
            {
                Timer.Resume(handle);
            }
            else if (!msg.IsRunning && Timer.IsRunning(handle))
            {
                Timer.Pause(handle);
            }
        }

        private void HandleCancel(Networking.TimerCancelMessage msg)
        {
            var handle = GetHandle(msg.NetworkId);
            if (handle.IsValid) Timer.Cancel(handle);
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
            public bool ServerAuth;
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
