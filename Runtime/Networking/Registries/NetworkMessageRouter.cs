using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Networking
{
    /// <summary>
    /// Routes network messages to handlers.
    /// </summary>
    public class NetworkMessageRouter
    {
        private readonly Dictionary<Type, ushort> _typeToId = new Dictionary<Type, ushort>();
        private readonly Dictionary<ushort, Type> _idToType = new Dictionary<ushort, Type>();
        private readonly Dictionary<ushort, List<Delegate>> _handlers = new Dictionary<ushort, List<Delegate>>();
        private ushort _nextId = 1;

        public event Action<ushort> OnTypeRegistered;
        public event Action<ushort> OnTypeUnregistered;

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
            if (!_idToType.TryGetValue(msgId, out var type)) return;
            if (!_handlers.TryGetValue(msgId, out var handlers)) return;

            var deserialize = typeof(NetworkSerializer).GetMethod("Deserialize").MakeGenericMethod(type);
            var message = deserialize.Invoke(null, new object[] { data });

            foreach (var handler in handlers.ToArray())
            {
                try { handler.DynamicInvoke(message); }
                catch (Exception e) { Debug.LogException(e); }
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

        public void Clear()
        {
            _handlers.Clear();
            _typeToId.Clear();
            _idToType.Clear();
            _nextId = 1;
        }
    }
}
