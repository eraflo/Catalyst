using System;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// A generic property that automatically synchronizes its value over the network.
    /// Works for C# classes and GameObjects.
    /// </summary>
    public class NetworkProperty<T> where T : IEquatable<T>
    {
        private T _value;
        private readonly string _name;
        private readonly uint _networkId;
        private readonly NetworkManager _network;

        public event Action<T> OnValueChanged;

        public T Value
        {
            get => _value;
            set
            {
                if (_value == null && value == null) return;
                if (_value != null && _value.Equals(value)) return;

                _value = value;
                OnValueChanged?.Invoke(_value);

                if (_network != null && _network.IsConnected && _network.IsServer)
                {
                    Sync();
                }
            }
        }

        public NetworkProperty(string name, uint networkId, T defaultValue = default)
        {
            _name = name;
            _networkId = networkId;
            _value = defaultValue;
            _network = App.Get<NetworkManager>();
        }

        public void Sync()
        {
            if (_network == null || !_network.IsConnected || !_network.IsServer) return;

            var msg = new NetworkStateUpdateMessage
            {
                NetworkId = _networkId,
                PropertyName = _name,
                Data = NetworkSerializer.SerializeValue(_value)
            };

            _network.Send(msg, NetworkTarget.Clients, NetworkDelivery.ReliableSequenced);
        }

        /// <summary>
        /// Internal use: Updates the value without triggering a network sync.
        /// </summary>
        internal void SetValueInternal(T newValue)
        {
            _value = newValue;
            OnValueChanged?.Invoke(_value);
        }
    }
}
