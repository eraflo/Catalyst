using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Routes network messages to handlers.
    /// </summary>
    public class NetworkMessageRouter
    {
        private readonly Dictionary<Type, ushort> _typeToId = new Dictionary<Type, ushort>();
        private readonly Dictionary<ushort, Type> _idToType = new Dictionary<ushort, Type>();
        private readonly Dictionary<ushort, List<Delegate>> _handlers = new Dictionary<ushort, List<Delegate>>();
        private readonly Dictionary<ushort, System.Reflection.MethodInfo> _deserializeCache = new Dictionary<ushort, System.Reflection.MethodInfo>();
        private ushort _nextId = 1;
        private bool _isRouting = false;
        
        /// <summary>The ID of the client that sent the last routed message.</summary>
        public ulong LastMessageSenderId { get; private set; }

        public event Action<ushort> OnTypeRegistered;
        public event Action<ushort> OnTypeUnregistered;
        
        /// <summary>Types currently registered in the router.</summary>
        public IEnumerable<Type> RegisteredTypes => _typeToId.Keys;

        /// <summary>Gets the ID associated with a type.</summary>
        public ushort GetIdByType(Type type) => _typeToId.TryGetValue(type, out var id) ? id : (ushort)0;

        public void On<T>(Action<T> handler) where T : struct, INetworkMessage
        {
            var msgId = GetOrCreateId<T>();

            if (!_handlers.TryGetValue(msgId, out var list))
            {
                list = new List<Delegate>();
                _handlers[msgId] = list;
                OnTypeRegistered?.Invoke(msgId);
            }
            list.Add(handler);
        }

        public void Off<T>(Action<T> handler) where T : struct, INetworkMessage
        {
            var msgId = GetOrCreateId<T>();

            if (_handlers.TryGetValue(msgId, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    _handlers.Remove(msgId);
                    OnTypeUnregistered?.Invoke(msgId);
                }
            }
        }

        public ushort GetId<T>() where T : struct, INetworkMessage => GetOrCreateId<T>();

        public void Route(ushort msgId, byte[] data, ulong senderId)
        {
            if (_isRouting) return; // Recursion loop guard
            
            LastMessageSenderId = senderId;
            if (!_idToType.TryGetValue(msgId, out var type)) return;
            if (!_handlers.TryGetValue(msgId, out var handlers)) return;

            if (!_deserializeCache.TryGetValue(msgId, out var deserialize))
            {
                deserialize = typeof(NetworkSerializer).GetMethod("Deserialize").MakeGenericMethod(type);
                _deserializeCache[msgId] = deserialize;
            }

            var message = deserialize.Invoke(null, new object[] { data });

            _isRouting = true;
            try
            {
                foreach (var handler in handlers.ToArray())
                {
                    try { handler.DynamicInvoke(message); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
            finally
            {
                _isRouting = false;
            }

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkMessageRouter] Routed {type.Name}");
            }
        }

        private ushort GetOrCreateId<T>() where T : struct, INetworkMessage
        {
            var type = typeof(T);
            if (!_typeToId.TryGetValue(type, out var id))
            {
                id = _nextId++;
                _typeToId[type] = id;
                _idToType[id] = type;
            }
            return id;
        }

        public void ClearEventSubscribers()
        {
            OnTypeRegistered = null;
            OnTypeUnregistered = null;
        }

        public void Clear()
        {
            _handlers.Clear();
            _typeToId.Clear();
            _idToType.Clear();
            _deserializeCache.Clear();
            _nextId = 1;
            ClearEventSubscribers();
        }

    }
}
