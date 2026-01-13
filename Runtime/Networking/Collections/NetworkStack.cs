using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Collections
{
    /// <summary>
    /// A synchronized stack that propagates changes over the network.
    /// Note: Push/Pop behavior depends on AuthorityMode.
    /// </summary>
    public class NetworkStack<T> : IEnumerable<T>
    {
        private readonly Stack<T> _items = new Stack<T>();
        private readonly uint _networkId;
        private readonly string _name;
        private readonly NetworkManager _network;
        private readonly NetworkOwnershipManager _ownership;
        private readonly AuthorityMode _authorityMode;

        public event Action<T> OnPushed;
        public event Action<T> OnPopped;
        public event Action OnCleared;
        public event Action OnChanged;

        public int Count => _items.Count;

        public NetworkStack(string name, uint networkId, AuthorityMode authorityMode = AuthorityMode.ServerAuthoritative)
        {
            _name = name;
            _networkId = networkId;
            _authorityMode = authorityMode;
            _network = App.Get<NetworkManager>();
            _ownership = App.Get<NetworkOwnershipManager>();

            _network.On<NetworkStackDeltaMessage>(HandleDelta);

        }

        private bool HasAuthority()
        {
            if (_ownership == null) return _network.IsServer;
            return _ownership.HasAuthority(_networkId, _authorityMode);
        }



        public void Push(T item)
        {
            if (!HasAuthority()) return;
            _items.Push(item);
            SendDelta(StackOperation.Push, item);
            OnPushed?.Invoke(item);
            OnChanged?.Invoke();
        }

        public T Pop()
        {
            if (!HasAuthority()) return default;
            if (_items.Count == 0) return default;
            
            T item = _items.Pop();
            SendDelta(StackOperation.Pop, default);
            OnPopped?.Invoke(item);
            OnChanged?.Invoke();
            return item;
        }

        public void Clear()
        {
            if (!HasAuthority()) return;
            _items.Clear();
            SendDelta(StackOperation.Clear, default);
            OnCleared?.Invoke();
            OnChanged?.Invoke();
        }

        public T Peek() => _items.Peek();

        private void SendDelta(StackOperation op, T value)
        {
            if (!_network.IsConnected) return;

            var msg = new NetworkStackDeltaMessage
            {
                NetworkId = _networkId,
                SenderId = _network.LocalClientId,
                CollectionName = _name,
                Operation = op,
                Data = NetworkSerializer.SerializeValue(value)
            };

            var target = _network.IsServer ? NetworkTarget.Others : NetworkTarget.Server;
            _network.Send(msg, target, NetworkDelivery.ReliableSequenced);
        }

        private void HandleDelta(NetworkStackDeltaMessage msg)
        {
            if (msg.NetworkId != _networkId || msg.CollectionName != _name) return;
            
            // Ignore our own messages (we already applied the change locally)
            if (msg.SenderId == _network.LocalClientId) return;

            if (_network.IsServer)
            {
                if (_ownership != null && !_ownership.HasAuthority(msg.SenderId, _networkId, _authorityMode))
                {
                    Debug.LogWarning($"[NetworkStack] Rejected delta from client {msg.SenderId} for collection '{_name}' (No Authority)");
                    return;
                }

                ApplyDelta(msg);
                _network.Send(msg, NetworkTarget.Others, NetworkDelivery.ReliableSequenced);
            }
            else
            {
                ApplyDelta(msg);
            }
        }

        private void ApplyDelta(NetworkStackDeltaMessage msg)
        {
            T value = NetworkSerializer.DeserializeValue<T>(msg.Data);

            switch (msg.Operation)
            {
                case StackOperation.Push:
                    _items.Push(value);
                    OnPushed?.Invoke(value);
                    break;
                case StackOperation.Pop:
                    if (_items.Count > 0)
                    {
                        T popped = _items.Pop();
                        OnPopped?.Invoke(popped);
                    }
                    break;
                case StackOperation.Clear:
                    _items.Clear();
                    OnCleared?.Invoke();
                    break;
            }
            OnChanged?.Invoke();
        }

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}
