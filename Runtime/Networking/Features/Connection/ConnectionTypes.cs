using System;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Connection
{
    public struct ConnectionRequest
    {
        public ulong ClientId;
        public byte[] Payload;
        
        public T GetPayload<T>()
        {
            if (Payload == null || Payload.Length == 0) return default;
            return NetworkSerializer.DeserializeValue<T>(Payload);
        }
    }

    public struct ConnectionResponse
    {
        public bool Approved;
        public string Reason;
        public bool CreatePlayerObject;
        public int? PlayerPrefabHash;
        public Vector3? Position;
        public Quaternion? Rotation;

        public static ConnectionResponse Success() => new ConnectionResponse { Approved = true, CreatePlayerObject = true };
        public static ConnectionResponse Success(Vector3 position, Quaternion rotation) => new ConnectionResponse 
        { 
            Approved = true, 
            CreatePlayerObject = true,
            Position = position,
            Rotation = rotation
        };
        public static ConnectionResponse Reject(string reason) => new ConnectionResponse { Approved = false, Reason = reason };
    }
}
