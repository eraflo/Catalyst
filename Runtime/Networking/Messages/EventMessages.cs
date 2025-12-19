using System.IO;

namespace Eraflo.UnityImportPackage.Networking
{
    /// <summary>
    /// Network message for event channel broadcasts.
    /// </summary>
    public struct EventChannelMessage : INetworkMessage
    {
        public string ChannelId;
        public byte[] Payload;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ChannelId ?? "");
            writer.Write(Payload?.Length ?? 0);
            if (Payload != null && Payload.Length > 0)
            {
                writer.Write(Payload);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            ChannelId = reader.ReadString();
            int length = reader.ReadInt32();
            Payload = length > 0 ? reader.ReadBytes(length) : null;
        }
    }
}
