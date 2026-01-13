using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Collections
{
    /// <summary>
    /// A synchronized collection that propagates changes over the network.
    /// Provides detailed events for better DX and reactivity.
    /// </summary>
    public class NetworkList<T> : IList<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly uint _networkId;
        private readonly string _name;
        private readonly NetworkManager _network;
        private readonly NetworkOwnershipManager _ownership;
        private readonly AuthorityMode _authorityMode;

        // Detailed events for better DX
        public event Action<T> OnItemAdded;
        public event Action<int, T> OnItemRemoved;
        public event Action<int, T, T> OnItemSet; // index, old, new
        public event Action OnCleared;
        public event Action OnChanged;

        public int Count => _items.Count;
        public bool IsReadOnly => !HasAuthority();

        public NetworkList(string name, uint networkId, AuthorityMode authorityMode = AuthorityMode.ServerAuthoritative)
        {
            _name = name;
            _networkId = networkId;
            _authorityMode = authorityMode;
            _network = App.Get<NetworkManager>();
            _ownership = App.Get<NetworkOwnershipManager>();
            
            // Register for routing
            _network.On<NetworkListDeltaMessage>(HandleDelta);
        }

        private bool HasAuthority()
        {
            if (_ownership == null) return _network.IsServer;
            return _ownership.HasAuthority(_networkId, _authorityMode);
        }

        public T this[int index]
        {
            get => _items[index];
            set 
            {
                if (!HasAuthority()) return;
                var old = _items[index];
                _items[index] = value;
                SendDelta(ListOperation.Set, index, value);
                NotifySet(index, old, value);
            }
        }

        public void Add(T item)
        {
            if (!HasAuthority()) return;
            _items.Add(item);
            SendDelta(ListOperation.Add, -1, item);
            NotifyAdded(item);
        }

        public void Insert(int index, T item)
        {
            if (!HasAuthority()) return;
            _items.Insert(index, item);
            SendDelta(ListOperation.Insert, index, item);
            NotifyAdded(item); // Simple notification for now
        }

        public bool Remove(T item)
        {
            if (!_network.IsServer) return false;
            int index = _items.IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (!HasAuthority()) return;
            var item = _items[index];
            _items.RemoveAt(index);
            SendDelta(ListOperation.Remove, index, default);
            NotifyRemoved(index, item);
        }

        public void Clear()
        {
            if (!HasAuthority()) return;
            _items.Clear();
            SendDelta(ListOperation.Clear, -1, default);
            NotifyCleared();
        }

        private void SendDelta(ListOperation op, int index, T value)
        {
            if (!_network.IsConnected) return;

            var msg = new NetworkListDeltaMessage
            {
                NetworkId = _networkId,
                SenderId = _network.LocalClientId,
                CollectionName = _name,
                Operation = op,
                Index = index,
                Data = NetworkSerializer.SerializeValue(value)
            };

            // Server broadcasts to others, Client sends to Server for relay
            var target = _network.IsServer ? NetworkTarget.Others : NetworkTarget.Server;
            _network.Send(msg, target, NetworkDelivery.ReliableSequenced);
        }

        private void HandleDelta(NetworkListDeltaMessage msg)
        {
            if (msg.NetworkId != _networkId || msg.CollectionName != _name) return;

            if (_network.IsServer)
            {
                // Server validation: Check if sender has authority
                if (_ownership != null && !_ownership.HasAuthority(msg.SenderId, _networkId, _authorityMode))
                {
                    Debug.LogWarning($"[NetworkList] Rejected delta from client {msg.SenderId} for list '{_name}' (No Authority)");
                    return;
                }

                ApplyDelta(msg);

                // Relay to other clients
                _network.Send(msg, NetworkTarget.Others, NetworkDelivery.ReliableSequenced);
            }
            else
            {
                ApplyDelta(msg);
            }
        }

        private void ApplyDelta(NetworkListDeltaMessage msg)
        {
            T value = NetworkSerializer.DeserializeValue<T>(msg.Data);

            switch (msg.Operation)
            {
                case ListOperation.Add:
                    _items.Add(value);
                    NotifyAdded(value);
                    break;
                case ListOperation.Remove:
                    if (msg.Index >= 0 && msg.Index < _items.Count)
                    {
                        var removedItem = _items[msg.Index];
                        _items.RemoveAt(msg.Index);
                        NotifyRemoved(msg.Index, removedItem);
                    }
                    break;
                case ListOperation.Set:
                    if (msg.Index >= 0 && msg.Index < _items.Count)
                    {
                        var oldItem = _items[msg.Index];
                        _items[msg.Index] = value;
                        NotifySet(msg.Index, oldItem, value);
                    }
                    break;
                case ListOperation.Clear:
                    _items.Clear();
                    NotifyCleared();
                    break;
                case ListOperation.Insert:
                    if (msg.Index >= 0 && msg.Index <= _items.Count)
                    {
                        _items.Insert(msg.Index, value);
                        NotifyAdded(value);
                    }
                    break;
            }
        }

        #region Notifications
        private void NotifyAdded(T item) { OnItemAdded?.Invoke(item); OnChanged?.Invoke(); }
        private void NotifyRemoved(int index, T item) { OnItemRemoved?.Invoke(index, item); OnChanged?.Invoke(); }
        private void NotifySet(int index, T old, T @new) { OnItemSet?.Invoke(index, old, @new); OnChanged?.Invoke(); }
        private void NotifyCleared() { OnCleared?.Invoke(); OnChanged?.Invoke(); }
        #endregion

        #region Interface Stubs
        public int IndexOf(T item) => _items.IndexOf(item);
        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        #endregion
    }
}
