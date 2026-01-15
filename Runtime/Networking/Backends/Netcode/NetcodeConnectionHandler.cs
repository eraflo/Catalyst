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

            // Sync local payload
            var connectionManager = App.Get<ConnectionManager>();
            if (connectionManager != null)
            {
                var payload = connectionManager.GetLocalPayload();
                _netcodeMgr.NetworkConfig.ConnectionData = payload ?? System.Array.Empty<byte>();
            }
        }

        private void HandleConnectionApproval(NetcodeMgr.ConnectionApprovalRequest request, NetcodeMgr.ConnectionApprovalResponse response)
        {
            var cm = App.Get<ConnectionManager>();
            if (cm == null)
            {
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