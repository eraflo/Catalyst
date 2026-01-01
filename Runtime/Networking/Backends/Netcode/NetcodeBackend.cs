#if UNITY_NETCODE
using System;
using System.Collections.Generic;
using UnityEngine;
using NetcodeMgr = Unity.Netcode.NetworkManager;

namespace Eraflo.Catalyst.Networking.Backends
{
    /// <summary>
    /// Network backend implementation for Unity Netcode for GameObjects.
    /// </summary>
    public class NetcodeBackend : INetworkBackend
    {
        private readonly Dictionary<ushort, Action<byte[], ulong>> _handlers = new Dictionary<ushort, Action<byte[], ulong>>();

        public bool IsServer => NetcodeMgr.Singleton != null && NetcodeMgr.Singleton.IsServer;
        public bool IsClient => NetcodeMgr.Singleton != null && NetcodeMgr.Singleton.IsClient;
        public bool IsConnected => NetcodeMgr.Singleton != null && NetcodeMgr.Singleton.IsConnectedClient;

        public void Initialize()
        {
            if (NetcodeMgr.Singleton == null)
            {
                Debug.LogWarning("[NetcodeBackend] NetworkManager.Singleton is null");
                return;
            }

            NetcodeMgr.Singleton.CustomMessagingManager.OnUnnamedMessage += HandleUnnamedMessage;
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log("[NetcodeBackend] Initialized");
            }
        }

        public void Shutdown()
        {
            if (NetcodeMgr.Singleton != null && NetcodeMgr.Singleton.CustomMessagingManager != null)
            {
                NetcodeMgr.Singleton.CustomMessagingManager.OnUnnamedMessage -= HandleUnnamedMessage;
            }
            
            _handlers.Clear();
        }

        public void Send(ushort msgType, byte[] data, NetworkTarget target)
        {
            if (NetcodeMgr.Singleton == null || !IsConnected) return;

            var fullData = new byte[2 + data.Length];
            fullData[0] = (byte)(msgType >> 8);
            fullData[1] = (byte)(msgType & 0xFF);
            Buffer.BlockCopy(data, 0, fullData, 2, data.Length);

            using (var writer = new Unity.Netcode.FastBufferWriter(fullData.Length, Unity.Collections.Allocator.Temp))
            {
                writer.WriteBytesSafe(fullData);

                switch (target)
                {
                    case NetworkTarget.All:
                        SendToAll(writer);
                        break;
                    case NetworkTarget.Others:
                        SendToOthers(writer);
                        break;
                    case NetworkTarget.Server:
                        SendToServer(writer);
                        break;
                    case NetworkTarget.Clients:
                        SendToClients(writer);
                        break;
                }
            }
        }

        private void SendToAll(Unity.Netcode.FastBufferWriter writer)
        {
            if (IsServer)
            {
                foreach (var clientId in NetcodeMgr.Singleton.ConnectedClientsIds)
                {
                    NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                        clientId, writer, Unity.Netcode.NetworkDelivery.ReliableSequenced);
                }
            }
            else
            {
                NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                    Unity.Netcode.NetworkManager.ServerClientId, writer, Unity.Netcode.NetworkDelivery.ReliableSequenced);
            }
        }

        private void SendToOthers(Unity.Netcode.FastBufferWriter writer)
        {
            if (IsServer)
            {
                var localClientId = NetcodeMgr.Singleton.LocalClientId;
                foreach (var clientId in NetcodeMgr.Singleton.ConnectedClientsIds)
                {
                    if (clientId != localClientId)
                    {
                        NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                            clientId, writer, Unity.Netcode.NetworkDelivery.ReliableSequenced);
                    }
                }
            }
            else
            {
                NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                    Unity.Netcode.NetworkManager.ServerClientId, writer, Unity.Netcode.NetworkDelivery.ReliableSequenced);
            }
        }

        private void SendToServer(Unity.Netcode.FastBufferWriter writer)
        {
            if (!IsServer)
            {
                NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                    Unity.Netcode.NetworkManager.ServerClientId, writer, Unity.Netcode.NetworkDelivery.ReliableSequenced);
            }
        }

        private void SendToClients(Unity.Netcode.FastBufferWriter writer)
        {
            if (IsServer)
            {
                foreach (var clientId in NetcodeMgr.Singleton.ConnectedClientsIds)
                {
                    if (clientId != NetcodeMgr.Singleton.LocalClientId)
                    {
                        NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                            clientId, writer, Unity.Netcode.NetworkDelivery.ReliableSequenced);
                    }
                }
            }
        }

        private void HandleUnnamedMessage(ulong senderId, Unity.Netcode.FastBufferReader reader)
        {
            var length = reader.Length - reader.Position;
            var fullData = new byte[length];
            reader.ReadBytesSafe(ref fullData, length);

            if (fullData.Length < 2) return;

            ushort msgType = (ushort)((fullData[0] << 8) | fullData[1]);

            var data = new byte[fullData.Length - 2];
            Buffer.BlockCopy(fullData, 2, data, 0, data.Length);

            if (_handlers.TryGetValue(msgType, out var handler))
            {
                try { handler.Invoke(data, senderId); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public void RegisterHandler(ushort msgType, Action<byte[], ulong> handler)
        {
            _handlers[msgType] = handler;
        }

        public void UnregisterHandler(ushort msgType)
        {
            _handlers.Remove(msgType);
        }

        public ulong LocalClientId => NetcodeMgr.Singleton?.LocalClientId ?? 0;

        public void SendToClient(ushort msgType, byte[] data, ulong clientId)
        {
            if (NetcodeMgr.Singleton == null || !IsConnected || !IsServer) return;

            var fullData = new byte[2 + data.Length];
            fullData[0] = (byte)(msgType >> 8);
            fullData[1] = (byte)(msgType & 0xFF);
            Buffer.BlockCopy(data, 0, fullData, 2, data.Length);

            using (var writer = new Unity.Netcode.FastBufferWriter(fullData.Length, Unity.Collections.Allocator.Temp))
            {
                writer.WriteBytesSafe(fullData);
                NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                    clientId, writer, Unity.Netcode.NetworkDelivery.ReliableSequenced);
            }
        }

        public void SendToClients(ushort msgType, byte[] data, ulong[] clientIds)
        {
            if (NetcodeMgr.Singleton == null || !IsConnected || !IsServer) return;

            var fullData = new byte[2 + data.Length];
            fullData[0] = (byte)(msgType >> 8);
            fullData[1] = (byte)(msgType & 0xFF);
            Buffer.BlockCopy(data, 0, fullData, 2, data.Length);

            foreach (var clientId in clientIds)
            {
                using (var writer = new Unity.Netcode.FastBufferWriter(fullData.Length, Unity.Collections.Allocator.Temp))
                {
                    writer.WriteBytesSafe(fullData);
                    NetcodeMgr.Singleton.CustomMessagingManager.SendUnnamedMessage(
                        clientId, writer, Unity.Netcode.NetworkDelivery.ReliableSequenced);
                }
            }
        }
    }
}
#endif
