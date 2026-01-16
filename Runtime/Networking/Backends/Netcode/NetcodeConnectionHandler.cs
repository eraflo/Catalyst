#if UNITY_NETCODE
using Eraflo.Catalyst.Networking.Features.Connection;
using UnityEngine;
using NetcodeMgr = Unity.Netcode.NetworkManager;

namespace Eraflo.Catalyst.Networking.Backends.Netcode
{
    /// <summary>
    /// Handles connection approval and payload synchronization for Netcode for GameObjects.
    /// </summary>
    public class NetcodeConnectionHandler : IConnectionBackend
    {
        private readonly NetcodeMgr _netcodeMgr;

        public NetcodeConnectionHandler(NetcodeMgr netcodeMgr)
        {
            _netcodeMgr = netcodeMgr;
        }

        public void Initialize()
        {
            _netcodeMgr.NetworkConfig.ConnectionApproval = true;
            _netcodeMgr.ConnectionApprovalCallback = HandleConnectionApproval;

            var connectionManager = App.Get<ConnectionManager>();
            if (connectionManager != null)
            {
                // Sync initial payload
                UpdateConnectionData(connectionManager.GetLocalPayload());

                // Subscribe to future changes
                connectionManager.OnPayloadChanged += UpdateConnectionData;
            }
        }

        private void UpdateConnectionData(byte[] payload)
        {
            _netcodeMgr.NetworkConfig.ConnectionData = payload ?? System.Array.Empty<byte>();
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetcodeConnectionHandler] Updated ConnectionData payload (Size: {_netcodeMgr.NetworkConfig.ConnectionData.Length} bytes)");
            }
        }

        private void HandleConnectionApproval(NetcodeMgr.ConnectionApprovalRequest request, NetcodeMgr.ConnectionApprovalResponse response)
        {

            var cm = App.Get<ConnectionManager>();
            if (cm == null)
            {
                Debug.LogWarning("[NetcodeConnectionHandler] ConnectionManager not found during approval. Defaulting to Approved.");
                response.Approved = true;
                response.CreatePlayerObject = true;
                return;
            }

            var result = cm.HandleApproval(request.ClientNetworkId, request.Payload);

            response.Approved = result.Approved;
            response.Reason = result.Reason;
            response.CreatePlayerObject = result.CreatePlayerObject;

            if (result.PlayerPrefabHash.HasValue)
                response.PlayerPrefabHash = (uint)result.PlayerPrefabHash.Value;

            if (result.Position.HasValue)
                response.Position = result.Position.Value;

            if (result.Rotation.HasValue)
                response.Rotation = result.Rotation.Value;
        }
    }
}
#endif