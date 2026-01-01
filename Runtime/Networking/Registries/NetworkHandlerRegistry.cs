using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Manages system message handlers lifecycle.
    /// </summary>
    public class NetworkHandlerRegistry
    {
        private readonly List<INetworkMessageHandler> _handlers = new List<INetworkMessageHandler>();
        private readonly Dictionary<Type, INetworkMessageHandler> _byType = new Dictionary<Type, INetworkMessageHandler>();
        private bool _connected;

        public void Register(INetworkMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_handlers.Contains(handler)) return;
            
            _handlers.Add(handler);
            _byType[handler.GetType()] = handler;
            handler.OnRegistered();
            
            if (_connected) handler.OnNetworkConnected();
        }

        public void Unregister(INetworkMessageHandler handler)
        {
            if (_handlers.Remove(handler))
            {
                _byType.Remove(handler.GetType());
                handler.OnUnregistered();
            }
        }

        /// <summary>
        /// Gets a handler by type.
        /// </summary>
        public T Get<T>() where T : class, INetworkMessageHandler
        {
            return _byType.TryGetValue(typeof(T), out var h) ? h as T : null;
        }

        public void NotifyConnected()
        {
            _connected = true;
            foreach (var h in _handlers)
            {
                try { h.OnNetworkConnected(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public void NotifyDisconnected()
        {
            _connected = false;
            foreach (var h in _handlers)
            {
                try { h.OnNetworkDisconnected(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public void Clear()
        {
            foreach (var h in _handlers)
            {
                try { h.OnUnregistered(); }
                catch (Exception e) { Debug.LogException(e); }
            }
            _handlers.Clear();
            _byType.Clear();
        }
    }
}
