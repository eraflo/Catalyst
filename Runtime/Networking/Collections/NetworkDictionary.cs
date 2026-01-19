using System;
using System.Collections;
using System.Collections.Generic;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Collections
{
    /// <summary>
    /// A synchronized dictionary that propagates changes over the network.
    /// </summary>
    public class NetworkDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();
        private readonly uint _networkId;
        private readonly string _name;
        private readonly NetworkManager _network;
        private readonly NetworkOwnershipManager _ownership;
        private readonly AuthorityMode _authorityMode;

        public event Action<TKey, TValue> OnItemAdded;
        public event Action<TKey, TValue> OnItemRemoved;
        public event Action<TKey, TValue, TValue> OnItemSet; // key, old, new
        public event Action OnCleared;
        public event Action OnChanged;

        public int Count => _items.Count;
        public bool IsReadOnly => !HasAuthority();

        public NetworkDictionary(string name, uint networkId, AuthorityMode authorityMode = AuthorityMode.ServerAuthoritative)
        {
            _name = name;
            _networkId = networkId;
            _authorityMode = authorityMode;
            _network = App.Get<NetworkManager>();
            _ownership = App.Get<NetworkOwnershipManager>();

            _network.On<NetworkDictionaryDeltaMessage>(HandleDelta);

        }

        private bool HasAuthority()
        {
            if (_ownership == null) return _network.IsServer;
            return _ownership.HasAuthority(_networkId, _authorityMode);
        }



        public TValue this[TKey key]
        {
            get => _items[key];
            set
            {
                if (!HasAuthority()) return;
                bool exists = _items.TryGetValue(key, out TValue old);
                _items[key] = value;

                if (exists)
                {
                    SendDelta(DictionaryOperation.Set, key, value);
                    OnItemSet?.Invoke(key, old, value);
                }
                else
                {
                    SendDelta(DictionaryOperation.Add, key, value);
                    OnItemAdded?.Invoke(key, value);
                }
                OnChanged?.Invoke();
            }
        }

        public ICollection<TKey> Keys => _items.Keys;
        public ICollection<TValue> Values => _items.Values;

        public void Add(TKey key, TValue value)
        {
            if (!HasAuthority()) return;
            _items.Add(key, value);
            SendDelta(DictionaryOperation.Add, key, value);
            OnItemAdded?.Invoke(key, value);
            OnChanged?.Invoke();
        }

        public bool Remove(TKey key)
        {
            if (!HasAuthority()) return false;
            if (_items.TryGetValue(key, out TValue val))
            {
                _items.Remove(key);
                SendDelta(DictionaryOperation.Remove, key, default);
                OnItemRemoved?.Invoke(key, val);
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            if (!HasAuthority()) return;
            _items.Clear();
            SendDelta(DictionaryOperation.Clear, default, default);
            OnCleared?.Invoke();
            OnChanged?.Invoke();
        }

        private void SendDelta(DictionaryOperation op, TKey key, TValue value)
        {
            if (!_network.IsConnected) return;

            var msg = new NetworkDictionaryDeltaMessage
            {
                NetworkId = _networkId,
                SenderId = _network.LocalClientId,
                CollectionName = _name,
                Operation = op,
                KeyData = NetworkSerializer.SerializeValue(key),
                ValueData = NetworkSerializer.SerializeValue(value)
            };

            var target = _network.IsServer ? NetworkTarget.Others : NetworkTarget.Server;
            _network.Send(msg, target, NetworkDelivery.ReliableSequenced);
        }

        private void HandleDelta(NetworkDictionaryDeltaMessage msg)
        {
            if (msg.NetworkId != _networkId || msg.CollectionName != _name) return;

            // Ignore our own messages (we already applied the change locally)
            if (msg.SenderId == _network.LocalClientId) return;

            if (_network.IsServer)
            {
                if (_ownership != null && !_ownership.HasAuthority(msg.SenderId, _networkId, _authorityMode))
                {
                    Debug.LogWarning($"[NetworkDictionary] Rejected delta from client {msg.SenderId} for collection '{_name}' (No Authority)");
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

        private void ApplyDelta(NetworkDictionaryDeltaMessage msg)
        {
            TKey key = NetworkSerializer.DeserializeValue<TKey>(msg.KeyData);
            TValue value = NetworkSerializer.DeserializeValue<TValue>(msg.ValueData);

            switch (msg.Operation)
            {
                case DictionaryOperation.Add:
                    _items[key] = value;
                    OnItemAdded?.Invoke(key, value);
                    break;
                case DictionaryOperation.Remove:
                    if (_items.TryGetValue(key, out TValue removedVal))
                    {
                        _items.Remove(key);
                        OnItemRemoved?.Invoke(key, removedVal);
                    }
                    break;
                case DictionaryOperation.Set:
                    bool exists = _items.TryGetValue(key, out TValue old);
                    _items[key] = value;
                    if (exists) OnItemSet?.Invoke(key, old, value);
                    else OnItemAdded?.Invoke(key, value);
                    break;
                case DictionaryOperation.Clear:
                    _items.Clear();
                    OnCleared?.Invoke();
                    break;
            }
            OnChanged?.Invoke();
        }

        #region Interface Stubs
        public bool ContainsKey(TKey key) => _items.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _items.TryGetValue(key, out value);
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_items).Contains(item);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_items).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
        #endregion
    }
}
