using System.Collections.Generic;
using System;
using System.Threading;
using System.Text;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Events;
using UnityEngine;

namespace Eraflo.Catalyst.HFSM.Networking
{
    /// <summary>
    /// Handles synchronization of StateMachines across the network.
    /// </summary>
    [Service(Priority = 21)]
    public class HfsmNetworkHandler : IGameService
    {
        private readonly Dictionary<uint, StateMachine> _machines = new Dictionary<uint, StateMachine>();
        private EventBus _events;
        private NetworkManager _network;
        private NetworkOwnershipManager _ownership;

        public void Initialize()
        {
            _events = App.Get<EventBus>();
            _network = App.Get<NetworkManager>();
            _ownership = App.Get<NetworkOwnershipManager>();

            _events?.Subscribe<HfsmStateChangedEvent>(OnLocalStateChanged);
            _network?.On<HfsmSyncMessage>(OnSyncMessageReceived);
        }

        public void Shutdown()
        {
            _events?.Unsubscribe<HfsmStateChangedEvent>(OnLocalStateChanged);
            _machines.Clear();
        }

        public void RegisterMachine(uint networkId, StateMachine machine)
        {
            _machines[networkId] = machine;
        }

        private void OnLocalStateChanged(HfsmStateChangedEvent evt)
        {
            // Find if this machine is registered for networking
            uint networkId = 0;
            foreach (var kvp in _machines)
            {
                if (kvp.Value == evt.Machine)
                {
                    networkId = kvp.Key;
                    break;
                }
            }

            if (networkId == 0) return;
            if (_network == null || !_network.IsConnected) return;

            var machine = evt.Machine;
            bool hasAuthority = _ownership != null && _ownership.HasAuthority(networkId, machine.Authority);

            if (hasAuthority)
            {
                var path = BuildPathString(machine.ActivePath);
                _network.Send(new HfsmSyncMessage(networkId, path), NetworkTarget.Others);
                
                if (PackageSettings.Instance.NetworkDebugMode)
                {
                    Debug.Log($"[HFSM Network] Authority verified for machine {networkId}. Broadcasted: {path}");
                }
            }
        }

        private void OnSyncMessageReceived(HfsmSyncMessage msg)
        {
            if (_machines.TryGetValue(msg.NetworkId, out var machine))
            {
                machine.ChangeStateByPath(msg.StatePath);
            }
        }

        private string BuildPathString(IReadOnlyList<StateBase> path)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < path.Count; i++)
            {
                sb.Append(path[i].Name);
                if (i < path.Count - 1) sb.Append("/");
            }
            return sb.ToString();
        }
    }
}
