using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Collections
{
    /// <summary>
    /// A synchronized queue that propagates changes over the network.
    /// Note: Enqueue/Dequeue behavior depends on AuthorityMode.
    /// </summary>
    public class NetworkQueue<T> : IEnumerable<T>
    {
        private readonly Queue<T> _items = new Queue<T>();
        private readonly uint _networkId;
        private readonly string _name;
        private readonly NetworkManager _network;
        private readonly NetworkOwnershipManager _ownership;
        private readonly AuthorityMode _authorityMode;

        public event Action<T> OnEnqueued;
        public event Action<T> OnDequeued;
        public event Action OnCleared;
        public event Action OnChanged;

        public int Count => _items.Count;

        public NetworkQueue(string name, uint networkId, AuthorityMode authorityMode = AuthorityMode.ServerAuthoritative)
        {
            _name = name;
            _networkId = networkId;
            _authorityMode = authorityMode;
            _network = App.Get<NetworkManager>();
            _ownership = App.Get<NetworkOwnershipManager>();

            _network.On<NetworkQueueDeltaMessage>(HandleDelta);

        }

        private bool HasAuthority()
        {
            if (_ownership == null) return _network.IsServer;
            return _ownership.HasAuthority(_networkId, _authorityMode);
        }



        public void Enqueue(T item)
        {
            if (!HasAuthority()) return;
            _items.Enqueue(item);
            SendDelta(QueueOperation.Enqueue, item);
            OnEnqueued?.Invoke(item);
            OnChanged?.Invoke();
        }

        public T Dequeue()
        {
            if (!HasAuthority()) return default;
            if (_items.Count == 0) return default;
            
            T item = _items.Dequeue();
            SendDelta(QueueOperation.Dequeue, default);
            OnDequeued?.Invoke(item);
            OnChanged?.Invoke();
            return item;
        }

        public void Clear()
        {
            if (!HasAuthority()) return;
            _items.Clear();
            SendDelta(QueueOperation.Clear, default);
            OnCleared?.Invoke();
            OnChanged?.Invoke();
        }

        public T Peek() => _items.Peek();

        private void SendDelta(QueueOperation op, T value)
        {
            if (!_network.IsConnected) return;

            var msg = new NetworkQueueDeltaMessage
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

        private void HandleDelta(NetworkQueueDeltaMessage msg)
        {
            if (msg.NetworkId != _networkId || msg.CollectionName != _name) return;
            
            // Ignore our own messages (we already applied the change locally)
            if (msg.SenderId == _network.LocalClientId) return;

            if (_network.IsServer)
            {
                if (_ownership != null && !_ownership.HasAuthority(msg.SenderId, _networkId, _authorityMode))
                {
                    Debug.LogWarning($"[NetworkQueue] Rejected delta from client {msg.SenderId} for collection '{_name}' (No Authority)");
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

        private void ApplyDelta(NetworkQueueDeltaMessage msg)
        {
            T value = NetworkSerializer.DeserializeValue<T>(msg.Data);

            switch (msg.Operation)
            {
                case QueueOperation.Enqueue:
                    _items.Enqueue(value);
                    OnEnqueued?.Invoke(value);
                    break;
                case QueueOperation.Dequeue:
                    if (_items.Count > 0)
                    {
                        T dequeued = _items.Dequeue();
                        OnDequeued?.Invoke(dequeued);
                    }
                    break;
                case QueueOperation.Clear:
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
