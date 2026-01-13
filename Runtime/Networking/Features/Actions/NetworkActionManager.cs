using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Actions
{
    /// <summary>
    /// Service for triggering lightweight network actions without custom message classes.
    /// Uses string-based identifiers hashed for efficiency.
    /// </summary>
    [Service(Priority = 6)]
    public class NetworkActionManager : IGameService
    {
        private readonly Dictionary<int, Action<byte[]>> _actionHandlers = new Dictionary<int, Action<byte[]>>();
        private NetworkManager _network;

        public void Initialize()
        {
            _network = App.Get<NetworkManager>();
            _network.On<NetworkActionMessage>(HandleActionMessage);
        }

        public void Shutdown()
        {
            _actionHandlers.Clear();
        }

        /// <summary>
        /// Registers a callback for a specific action ID.
        /// </summary>
        public void RegisterAction(string actionId, Action<byte[]> handler)
        {
            int hash = actionId.GetHashCode();
            _actionHandlers[hash] = handler;
        }

        /// <summary>
        /// Triggers an action on other clients.
        /// </summary>
        public void Trigger(string actionId, params object[] data)
        {
            if (!_network.IsConnected) return;

            var msg = new NetworkActionMessage
            {
                ActionHash = actionId.GetHashCode(),
                Payload = NetworkSerializer.SerializeValue(data)
            };

            _network.Send(msg, NetworkTarget.Others);
        }

        public void TriggerToTarget(string actionId, NetworkTarget target, params object[] data)
        {
            if (!_network.IsConnected) return;

            var msg = new NetworkActionMessage
            {
                ActionHash = actionId.GetHashCode(),
                Payload = NetworkSerializer.SerializeValue(data)
            };

            _network.Send(msg, target);
        }

        private void HandleActionMessage(NetworkActionMessage msg)
        {
            if (_actionHandlers.TryGetValue(msg.ActionHash, out var handler))
            {
                handler.Invoke(msg.Payload);
            }
        }
    }
}
