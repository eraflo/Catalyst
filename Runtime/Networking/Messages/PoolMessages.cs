using System.IO;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Network message for pool spawn.
    /// </summary>
    public struct PoolSpawnMessage : INetworkMessage
    {
        public uint NetworkId;
        public int PrefabHash;
        public Vector3 Position;
        public Quaternion Rotation;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetworkId);
            writer.Write(PrefabHash);
            NetworkSerializer.WriteVector3(writer, Position);
            NetworkSerializer.WriteQuaternion(writer, Rotation);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetworkId = reader.ReadUInt32();
            PrefabHash = reader.ReadInt32();
            Position = NetworkSerializer.ReadVector3(reader);
            Rotation = NetworkSerializer.ReadQuaternion(reader);
        }
    }

    /// <summary>
    /// Network message for pool despawn.
    /// </summary>
    public struct PoolDespawnMessage : INetworkMessage
    {
        public uint NetworkId;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetworkId);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetworkId = reader.ReadUInt32();
        }
    }
}
