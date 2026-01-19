using System;
using Eraflo.Catalyst;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Central network manager service.
    /// Provides access to the backend, messaging router, and network handlers.
    /// </summary>
    [Service(Priority = 2)]
    public class NetworkManager : IGameService, INetworkService, IUpdatable
    {
        private INetworkBackend _backend;
        private readonly NetworkBackendRegistry _backends = new NetworkBackendRegistry();
        private readonly NetworkMessageRouter _router = new NetworkMessageRouter();
        private readonly NetworkHandlerRegistry _handlers = new NetworkHandlerRegistry();

        public INetworkBackend Backend => _backend;
        public NetworkBackendRegistry Backends => _backends;
        public NetworkMessageRouter Router => _router;
        public NetworkHandlerRegistry Handlers => _handlers;

        public bool HasBackend => _backend != null;
        public bool IsServer => _backend != null && _backend.IsServer;
        public bool IsClient => _backend != null && _backend.IsClient;
        public bool IsConnected => _backend != null && _backend.IsConnected;
        public bool IsHost => IsServer && IsClient;
        public ulong LocalClientId => _backend?.LocalClientId ?? 0;
        public ulong ServerClientId => _backend?.ServerClientId ?? 0;

        public event Action<INetworkBackend> OnBackendChanged
        {
            add => _onBackendChanged += value;
            remove => _onBackendChanged -= value;
        }

        public event Action OnConnected
        {
            add => _onConnected += value;
            remove => _onConnected -= value;
        }

        public event Action OnDisconnected
        {
            add => _onDisconnected += value;
            remove => _onDisconnected -= value;
        }

        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;

        #region IGameService

        void IGameService.Initialize()
        {
            // Initialization logic if needed
        }

        void IGameService.Shutdown()
        {
            Reset();
        }
        
        void IUpdatable.OnUpdate()
        {
            if (_backend == null || !_backend.IsConnected) return;
            _handlers.Update();
        }

        #endregion


        #region Instance Events

        private event Action<INetworkBackend> _onBackendChanged;
        private event Action _onConnected;
        private event Action _onDisconnected;

        #endregion

        #region Instance Methods

        public bool SetBackendById(string id)
        {
            var backend = _backends.Create(id);
            if (backend == null)
            {
                Debug.LogWarning($"[NetworkManager] Backend not found: {id}");
                return false;
            }
            SetBackend(backend);
            return true;
        }

        public void SetBackend(INetworkBackend backend)
        {
            bool wasConnected = _backend?.IsConnected ?? false;

            if (_backend != null)
            {
                if (wasConnected) _handlers.NotifyDisconnected();
                _backend.Shutdown();
            }

            _backend = backend;

            if (_backend != null)
            {
                _backend.Initialize();

                // Ensure we don't double-subscribe to router events
                _router.ClearEventSubscribers();

                // Register existing types
                foreach (var type in _router.RegisteredTypes)
                {
                    var msgId = _router.GetIdByType(type);
                    _backend.RegisterHandler(msgId, (data, senderId) => _router.Route(msgId, data, senderId));
                }

                // Wire router to backend
                _router.OnTypeRegistered += msgId =>
                {
                    if (_backend != null)
                        _backend.RegisterHandler(msgId, (data, senderId) => _router.Route(msgId, data, senderId));
                };
                _router.OnTypeUnregistered += msgId =>
                {
                    if (_backend != null)
                        _backend.UnregisterHandler(msgId);
                };

                if (_backend.IsConnected) _handlers.NotifyConnected();
            }

            _onBackendChanged?.Invoke(_backend);

            if (PackageSettings.Instance.NetworkDebugMode)
                Debug.Log($"[NetworkManager] Active Backend set to: {(_backend != null ? _backend.GetType().Name : "NULL")}");
        }

        public void Send<T>(T message, NetworkTarget target = NetworkTarget.All, NetworkDelivery delivery = NetworkDelivery.Reliable) where T : struct, INetworkMessage
        {
            if (_backend == null || !_backend.IsConnected) return;

            var msgId = _router.GetId<T>();
            var data = NetworkSerializer.Serialize(message);

            // 1. Centralized Loopback logic
            if (ShouldLoopback(target))
            {
                _router.Route(msgId, data, LocalClientId);
            }

            // 2. Remote Send (Backends should skip self)
            _backend.Send(msgId, data, target, delivery);

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkManager] Sent {typeof(T).Name} ({delivery})");
            }
        }

        public void SendToClient<T>(T message, ulong clientId, NetworkDelivery delivery = NetworkDelivery.Reliable) where T : struct, INetworkMessage
        {
            if (_backend == null || !_backend.IsConnected || !IsServer) return;

            var msgId = _router.GetId<T>();
            var data = NetworkSerializer.Serialize(message);

            if (clientId == LocalClientId)
            {
                _router.Route(msgId, data, LocalClientId);
            }
            else
            {
                _backend.SendToClient(msgId, data, clientId, delivery);
            }

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkManager] Sent {typeof(T).Name} to client {clientId} ({delivery})");
            }
        }

        public GameObject SpawnPlayer(ulong clientId, Vector3? position = null, Quaternion? rotation = null)
        {
            if (_backend == null || !IsServer) return null;
            return _backend.SpawnPlayer(clientId, position, rotation);
        }

        public ulong GetOwner(GameObject go)
        {
            if (_backend == null) return 0;
            return _backend.GetOwner(go);
        }

        public void SendToClients<T>(T message, NetworkDelivery delivery, params ulong[] clientIds) where T : struct, INetworkMessage
        {
            if (_backend == null || !_backend.IsConnected || !IsServer) return;

            var msgId = _router.GetId<T>();
            var data = NetworkSerializer.Serialize(message);

            // 1. Loopback for each occurrence of self in the list
            foreach (var id in clientIds)
            {
                if (id == LocalClientId)
                {
                    _router.Route(msgId, data, LocalClientId);
                }
            }

            // 2. Remote Send
            _backend.SendToClients(msgId, data, clientIds, delivery);

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkManager] Sent {typeof(T).Name} to {clientIds.Length} clients ({delivery})");
            }
        }

        private bool ShouldLoopback(NetworkTarget target)
        {
            switch (target)
            {
                case NetworkTarget.All: return true;
                case NetworkTarget.Others: return false;
                case NetworkTarget.Server: return IsServer;
                case NetworkTarget.Clients: return IsClient || IsServer; // Host acts as client too
                default: return false;
            }
        }

        public void SendToClients<T>(T message, params ulong[] clientIds) where T : struct, INetworkMessage
            => SendToClients(message, NetworkDelivery.Reliable, clientIds);

        public void SendToServer<T>(T message, NetworkDelivery delivery = NetworkDelivery.Reliable) where T : struct, INetworkMessage
            => Send(message, NetworkTarget.Server, delivery);

        public void SendToClients<T>(T message, NetworkDelivery delivery = NetworkDelivery.Reliable) where T : struct, INetworkMessage
            => Send(message, NetworkTarget.Clients, delivery);

        public void On<T>(Action<T> handler) where T : struct, INetworkMessage
            => _router.On(handler);

        public void Off<T>(Action<T> handler) where T : struct, INetworkMessage
            => _router.Off(handler);

        public void NotifyConnected()
        {
            _handlers.NotifyConnected();
            _onConnected?.Invoke();
        }

        public void NotifyDisconnected()
        {
            _handlers.NotifyDisconnected();
            _onDisconnected?.Invoke();
        }

        public void Reset()
        {
            _handlers.Clear();
            _router.Clear();
            SetBackend(null);
            _backends.Clear();
        }

        #region Lifecycle Proxies

        public bool StartServer(string address = "127.0.0.1", ushort port = 7777, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            if (_backend is INetworkLifecycle lifecycle) return lifecycle.StartServer(address, port, transport);
            Debug.LogWarning("[NetworkManager] Current backend does not support manual server starting.");
            return false;
        }

        public bool StartClient(string address = "127.0.0.1", ushort port = 7777, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            if (_backend is INetworkLifecycle lifecycle) return lifecycle.StartClient(address, port, transport);
            Debug.LogWarning("[NetworkManager] Current backend does not support manual client starting.");
            return false;
        }

        public bool StartHost(string address = "127.0.0.1", ushort port = 7777, NetworkTransportType transport = NetworkTransportType.UDP)
        {
            if (_backend is INetworkLifecycle lifecycle) return lifecycle.StartHost(address, port, transport);
            Debug.LogWarning("[NetworkManager] Current backend does not support manual host starting.");
            return false;
        }

        public void Stop()
        {
            if (_backend is INetworkLifecycle lifecycle) lifecycle.Stop();
            else _backend?.Shutdown();
        }

        /// <summary>Internal use only: notifies the manager of a client connection.</summary>
        internal void NotifyClientConnected(ulong clientId) => OnClientConnected?.Invoke(clientId);

        /// <summary>Internal use only: notifies the manager of a client disconnection.</summary>
        internal void NotifyClientDisconnected(ulong clientId) => OnClientDisconnected?.Invoke(clientId);

        #endregion

        #endregion
    }
}
