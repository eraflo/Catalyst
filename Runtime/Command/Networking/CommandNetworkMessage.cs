using System.IO;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.Command.Networking
{
    /// <summary>
    /// Network message that carries a serialized command for remote execution.
    /// </summary>
    public struct CommandNetworkMessage : INetworkMessage
    {
        public string CommandType;
        public byte[] Payload;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(CommandType ?? string.Empty);
            bool hasPayload = Payload != null;
            writer.Write(hasPayload);
            if (hasPayload)
            {
                writer.Write(Payload.Length);
                writer.Write(Payload);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            CommandType = reader.ReadString();
            if (reader.ReadBoolean())
            {
                int length = reader.ReadInt32();
                Payload = reader.ReadBytes(length);
            }
        }
    }
}
