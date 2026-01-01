using System;
using System.Collections.Generic;
using UnityEngine;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Handles network synchronization for event channels.
    /// </summary>
    public class EventNetworkHandler : INetworkMessageHandler
    {
        private readonly Dictionary<string, NetworkEventChannel> _voidChannels = new Dictionary<string, NetworkEventChannel>();
        private readonly Dictionary<string, object> _typedChannels = new Dictionary<string, object>();
        private bool _connected;

        /// <summary>Fired when an event message is received.</summary>
        public event Action<string, byte[]> OnEventReceived;

        public void OnRegistered()
        {
            NetworkManager.On<EventChannelMessage>(HandleEventMessage);
        }

        public void OnUnregistered()
        {
            NetworkManager.Off<EventChannelMessage>(HandleEventMessage);
            Clear();
        }

        public void OnNetworkConnected() => _connected = true;
        public void OnNetworkDisconnected() => _connected = false;

        /// <summary>
        /// Whether network is available.
        /// </summary>
        public bool IsNetworkAvailable => NetworkManager.HasBackend && NetworkManager.IsConnected;

        /// <summary>
        /// Registers a void channel for receiving.
        /// </summary>
        public void Register(NetworkEventChannel channel)
        {
            _voidChannels[channel.ChannelId] = channel;
        }

        /// <summary>
        /// Registers a typed channel for receiving.
        /// </summary>
        public void Register<T>(NetworkEventChannel<T> channel)
        {
            _typedChannels[channel.ChannelId] = channel;
        }

        /// <summary>
        /// Unregisters a channel.
        /// </summary>
        public void Unregister(string channelId)
        {
            _voidChannels.Remove(channelId);
            _typedChannels.Remove(channelId);
        }

        /// <summary>
        /// Sends a void event.
        /// </summary>
        public void Send(string channelId, NetworkTarget target)
        {
            if (!IsNetworkAvailable) return;

            var msg = new EventChannelMessage { ChannelId = channelId, Payload = null };
            NetworkManager.Send(msg, target);
        }

        /// <summary>
        /// Sends a typed event.
        /// </summary>
        public void Send(string channelId, byte[] payload, NetworkTarget target)
        {
            if (!IsNetworkAvailable) return;

            var msg = new EventChannelMessage { ChannelId = channelId, Payload = payload };
            NetworkManager.Send(msg, target);
        }

        private void HandleEventMessage(EventChannelMessage msg)
        {
            // Try void channel
            if (_voidChannels.TryGetValue(msg.ChannelId, out var voidChannel))
            {
                voidChannel.RaiseLocal();
                return;
            }

            // Try typed channels via reflection
            if (_typedChannels.TryGetValue(msg.ChannelId, out var typedChannel))
            {
                var type = typedChannel.GetType();
                var deserializeMethod = type.GetMethod("DeserializeValue");
                var raiseLocalMethod = type.GetMethod("RaiseLocal");
                
                if (deserializeMethod != null && raiseLocalMethod != null && msg.Payload != null)
                {
                    var value = deserializeMethod.Invoke(typedChannel, new object[] { msg.Payload });
                    raiseLocalMethod.Invoke(typedChannel, new object[] { value });
                    return;
                }
            }

            // Generic callback
            OnEventReceived?.Invoke(msg.ChannelId, msg.Payload);
        }

        public void Clear()
        {
            _voidChannels.Clear();
            _typedChannels.Clear();
        }
    }
}
