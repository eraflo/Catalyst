using System;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Connection
{
    /// <summary>
    /// Service for managing connection approval.
    /// Allows setting a payload on the client and validating it on the server.
    /// </summary>
    [Service(Priority = 4)]
    public class ConnectionManager : IGameService
    {
        private byte[] _localPayload;
        
        /// <summary>
        /// Event triggered on the server to validate an incoming connection.
        /// Return a ConnectionResponse to approve or reject.
        /// </summary>
        public event Func<ConnectionRequest, ConnectionResponse> OnValidateConnection;

        public void Initialize() { }
        public void Shutdown() 
        {
            OnValidateConnection = null;
            _localPayload = null;
        }

        /// <summary>
        /// Sets the payload to be sent when connecting as a client.
        /// </summary>
        public void SetPayload<T>(T payload)
        {
            _localPayload = NetworkSerializer.SerializeValue(payload);
        }

        public byte[] GetLocalPayload() => _localPayload;

        /// <summary>
        /// Internal: Handles the validation request from the backend.
        /// </summary>
        internal ConnectionResponse HandleApproval(ulong clientId, byte[] payload)
        {
            if (OnValidateConnection == null)
            {
                // Default to approval if no validator is set
                return ConnectionResponse.Success();
            }

            var request = new ConnectionRequest
            {
                ClientId = clientId,
                Payload = payload
            };

            try
            {
                return OnValidateConnection.Invoke(request);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return ConnectionResponse.Reject("Internal Server Error during validation");
            }
        }
    }
}
