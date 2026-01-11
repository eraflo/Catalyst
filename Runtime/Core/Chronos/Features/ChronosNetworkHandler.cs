using System;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.EasingSystem;
using UnityEngine;
using Eraflo.Catalyst.Core.Chronos;

namespace Eraflo.Catalyst.Core.Chronos.Features
{
    /// <summary>
    /// Handles network synchronization for the Chronos Manager.
    /// Ensures that time transitions on the server are replicated to clients.
    /// </summary>
    public class ChronosNetworkHandler : INetworkMessageHandler
    {
        private ChronosManager _chronos;
        private NetworkManager _network;

        public void OnRegistered()
        {
            _chronos = App.Get<ChronosManager>();
            _network = App.Get<NetworkManager>();

            if (_chronos != null)
            {
                _chronos.OnChannelTransitionStarted += HandleLocalTransition;
            }

            if (_network != null)
            {
                _network.On<ChronosSyncMessage>(HandleRemoteTransition);
            }
        }

        public void OnUnregistered()
        {
            if (_chronos != null)
            {
                _chronos.OnChannelTransitionStarted -= HandleLocalTransition;
            }

            if (_network != null)
            {
                _network.Off<ChronosSyncMessage>(HandleRemoteTransition);
            }
        }

        public void OnNetworkConnected() { }
        public void OnNetworkDisconnected() { }

        private void HandleLocalTransition(string channel, float target, float duration, EasingType ease)
        {
            // Only the server broadcasts transitions
            if (_network != null && _network.IsConnected && _network.IsServer)
            {
                var msg = new ChronosSyncMessage
                {
                    ChannelId = channel,
                    TargetScale = target,
                    Duration = duration,
                    EaseType = ease
                };
                _network.SendToClients(msg);
            }
        }

        private void HandleRemoteTransition(ChronosSyncMessage msg)
        {
            // Clients apply transitions received from the server
            if (_network != null && _network.IsClient && !_network.IsServer)
            {
                if (_chronos != null)
                {
                    // Use a temporary flag or check to prevent re-broadcasting if needed, 
                    // though HandleLocalTransition already checks IsServer.
                    _chronos.SetTimeScale(msg.ChannelId, msg.TargetScale, msg.Duration, msg.EaseType);
                }
            }
        }
    }
}
