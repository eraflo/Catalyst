using System.IO;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Message sent to update a specific property on a networked object.
    /// </summary>
    public struct NetworkStateUpdateMessage : INetworkMessage
    {
        public uint NetworkId;
        public string PropertyName;
        public byte[] Data;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetworkId);
            writer.Write(PropertyName ?? string.Empty);
            writer.Write(Data?.Length ?? 0);
            if (Data != null) writer.Write(Data);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetworkId = reader.ReadUInt32();
            PropertyName = reader.ReadString();
            int len = reader.ReadInt32();
            if (len > 0) Data = reader.ReadBytes(len);
        }
    }
}
