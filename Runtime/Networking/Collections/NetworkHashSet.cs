using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Collections
{
    /// <summary>
    /// A synchronized set that propagates changes over the network.
    /// </summary>
    public class NetworkHashSet<T> : ISet<T>
    {
        private readonly HashSet<T> _items = new HashSet<T>();
        private readonly uint _networkId;
        private readonly string _name;
        private readonly NetworkManager _network;
        private readonly NetworkOwnershipManager _ownership;
        private readonly AuthorityMode _authorityMode;

        public event Action<T> OnItemAdded;
        public event Action<T> OnItemRemoved;
        public event Action OnCleared;
        public event Action OnChanged;

        public int Count => _items.Count;
        public bool IsReadOnly => !HasAuthority();

        public NetworkHashSet(string name, uint networkId, AuthorityMode authorityMode = AuthorityMode.ServerAuthoritative)
        {
            _name = name;
            _networkId = networkId;
            _authorityMode = authorityMode;
            _network = App.Get<NetworkManager>();
            _ownership = App.Get<NetworkOwnershipManager>();

            _network.On<NetworkHashSetDeltaMessage>(HandleDelta);

        }

        private bool HasAuthority()
        {
            if (_ownership == null) return _network.IsServer;
            return _ownership.HasAuthority(_networkId, _authorityMode);
        }



        public bool Add(T item)
        {
            if (!HasAuthority()) return false;
            if (_items.Add(item))
            {
                SendDelta(SetOperation.Add, item);
                OnItemAdded?.Invoke(item);
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool Remove(T item)
        {
            if (!HasAuthority()) return false;
            if (_items.Remove(item))
            {
                SendDelta(SetOperation.Remove, item);
                OnItemRemoved?.Invoke(item);
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            if (!HasAuthority()) return;
            _items.Clear();
            SendDelta(SetOperation.Clear, default);
            OnCleared?.Invoke();
            OnChanged?.Invoke();
        }

        private void SendDelta(SetOperation op, T value)
        {
            if (!_network.IsConnected) return;

            var msg = new NetworkHashSetDeltaMessage
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

        private void HandleDelta(NetworkHashSetDeltaMessage msg)
        {
            if (msg.NetworkId != _networkId || msg.CollectionName != _name) return;
            
            // Ignore our own messages (we already applied the change locally)
            if (msg.SenderId == _network.LocalClientId) return;

            if (_network.IsServer)
            {
                if (_ownership != null && !_ownership.HasAuthority(msg.SenderId, _networkId, _authorityMode))
                {
                    Debug.LogWarning($"[NetworkHashSet] Rejected delta from client {msg.SenderId} for collection '{_name}' (No Authority)");
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

        private void ApplyDelta(NetworkHashSetDeltaMessage msg)
        {
            T value = NetworkSerializer.DeserializeValue<T>(msg.Data);

            switch (msg.Operation)
            {
                case SetOperation.Add:
                    if (_items.Add(value)) OnItemAdded?.Invoke(value);
                    break;
                case SetOperation.Remove:
                    if (_items.Remove(value)) OnItemRemoved?.Invoke(value);
                    break;
                case SetOperation.Clear:
                    _items.Clear();
                    OnCleared?.Invoke();
                    break;
            }
            OnChanged?.Invoke();
        }

        #region Interface Stubs
        void ICollection<T>.Add(T item) => Add(item);
        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public void ExceptWith(IEnumerable<T> other) { if (!HasAuthority()) return; foreach (var item in other) Remove(item); }
        public void IntersectWith(IEnumerable<T> other) { throw new NotImplementedException("Complex set operations not synced yet."); }
        public bool IsProperSubsetOf(IEnumerable<T> other) => _items.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => _items.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => _items.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => _items.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => _items.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => _items.SetEquals(other);
        public void SymmetricExceptWith(IEnumerable<T> other) { throw new NotImplementedException(); }
        public void UnionWith(IEnumerable<T> other) { if (!HasAuthority()) return; foreach (var item in other) Add(item); }
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        #endregion
    }
}
