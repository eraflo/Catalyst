using System;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Networking
{
    /// <summary>
    /// Central network manager facade.
    /// </summary>
    public static class NetworkManager
    {
        private static INetworkBackend _backend;
        private static readonly NetworkBackendRegistry _backends = new NetworkBackendRegistry();
        private static readonly NetworkMessageRouter _router = new NetworkMessageRouter();
        private static readonly NetworkHandlerRegistry _handlers = new NetworkHandlerRegistry();

        #region Registries

        /// <summary>Backend factory registry.</summary>
        public static NetworkBackendRegistry Backends => _backends;

        /// <summary>Message router.</summary>
        public static NetworkMessageRouter Router => _router;

        /// <summary>System handler registry.</summary>
        public static NetworkHandlerRegistry Handlers => _handlers;

        #endregion

        #region State

        /// <summary>Current backend.</summary>
        public static INetworkBackend Backend => _backend;

        /// <summary>Whether a backend is set.</summary>
        public static bool HasBackend => _backend != null;

        /// <summary>Is server.</summary>
        public static bool IsServer => _backend?.IsServer ?? true;

        /// <summary>Is client.</summary>
        public static bool IsClient => _backend?.IsClient ?? false;

        /// <summary>Is connected.</summary>
        public static bool IsConnected => _backend?.IsConnected ?? false;

        /// <summary>Is host (server + client).</summary>
        public static bool IsHost => IsServer && IsClient;

        #endregion

        #region Events

        public static event Action<INetworkBackend> OnBackendChanged;
        public static event Action OnConnected;
        public static event Action OnDisconnected;

        #endregion

        #region Backend

        public static bool SetBackendById(string id)
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

        public static void SetBackend(INetworkBackend backend)
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
                
                // Wire router to backend - check for null since backend might change
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

            OnBackendChanged?.Invoke(_backend);

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkManager] Backend: {backend?.GetType().Name ?? "none"}");
            }
        }

        public static void ClearBackend() => SetBackend(null);

        #endregion

        #region Messaging

        public static void Send<T>(T message, NetworkTarget target = NetworkTarget.All) where T : struct, INetworkMessage
        {
            if (_backend == null || !_backend.IsConnected) return;

            var msgId = _router.GetId<T>();
            var data = NetworkSerializer.Serialize(message);
            _backend.Send(msgId, data, target);

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkManager] Sent {typeof(T).Name}");
            }
        }

        public static void SendToServer<T>(T message) where T : struct, INetworkMessage
            => Send(message, NetworkTarget.Server);

        public static void SendToClients<T>(T message) where T : struct, INetworkMessage
            => Send(message, NetworkTarget.Clients);

        public static void On<T>(Action<T> handler) where T : struct, INetworkMessage
            => _router.On(handler);

        public static void Off<T>(Action<T> handler) where T : struct, INetworkMessage
            => _router.Off(handler);

        /// <summary>
        /// Sends a message to a specific client by ID. Server only.
        /// </summary>
        public static void SendToClient<T>(T message, ulong clientId) where T : struct, INetworkMessage
        {
            if (_backend == null || !_backend.IsConnected || !_backend.IsServer) return;

            var msgId = _router.GetId<T>();
            var data = NetworkSerializer.Serialize(message);
            _backend.SendToClient(msgId, data, clientId);

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkManager] Sent {typeof(T).Name} to client {clientId}");
            }
        }

        /// <summary>
        /// Sends a message to multiple specific clients by ID. Server only.
        /// </summary>
        public static void SendToClients<T>(T message, params ulong[] clientIds) where T : struct, INetworkMessage
        {
            if (_backend == null || !_backend.IsConnected || !_backend.IsServer) return;

            var msgId = _router.GetId<T>();
            var data = NetworkSerializer.Serialize(message);
            _backend.SendToClients(msgId, data, clientIds);

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkManager] Sent {typeof(T).Name} to {clientIds.Length} clients");
            }
        }

        /// <summary>
        /// Gets the local client ID.
        /// </summary>
        public static ulong LocalClientId => _backend?.LocalClientId ?? 0;

        #endregion

        #region Lifecycle

        public static void NotifyConnected()
        {
            _handlers.NotifyConnected();
            OnConnected?.Invoke();
        }

        public static void NotifyDisconnected()
        {
            _handlers.NotifyDisconnected();
            OnDisconnected?.Invoke();
        }

        public static void Reset()
        {
            _handlers.Clear();
            _router.Clear();
            ClearBackend();
            _backends.Clear();
        }

        #endregion
    }
}
