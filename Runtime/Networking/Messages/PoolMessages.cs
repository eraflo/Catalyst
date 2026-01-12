using System;
using System.IO;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Unified network message for pooling GameObjects and C# classes.
    /// </summary>
    [Serializable]
    public struct PoolNetworkMessage : INetworkMessage
    {
        public uint NetworkId;
        public string PoolId;    // Prefab name or Type FullName
        public bool IsSpawn;     // True for spawn, False for despawn
        public byte[] SpawnData; // Optional initialization data
        
        // For GameObjects
        public Vector3 Position;
        public Quaternion Rotation;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetworkId);
            writer.Write(PoolId ?? string.Empty);
            writer.Write(IsSpawn);
            
            bool hasSpawnData = SpawnData != null;
            writer.Write(hasSpawnData);
            if (hasSpawnData)
            {
                writer.Write(SpawnData.Length);
                writer.Write(SpawnData);
            }
            
            if (IsSpawn)
            {
                NetworkSerializer.WriteVector3(writer, Position);
                NetworkSerializer.WriteQuaternion(writer, Rotation);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            NetworkId = reader.ReadUInt32();
            PoolId = reader.ReadString();
            IsSpawn = reader.ReadBoolean();
            
            if (reader.ReadBoolean())
            {
                int length = reader.ReadInt32();
                SpawnData = reader.ReadBytes(length);
            }
            
            if (IsSpawn)
            {
                Position = NetworkSerializer.ReadVector3(reader);
                Rotation = NetworkSerializer.ReadQuaternion(reader);
            }
        }
    }
}
