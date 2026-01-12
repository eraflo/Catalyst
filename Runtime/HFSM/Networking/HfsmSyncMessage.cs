using System.IO;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.HFSM.Networking
{
    /// <summary>
    /// Network message to synchronize the active state path across clients.
    /// </summary>
    public struct HfsmSyncMessage : INetworkMessage
    {
        public uint NetworkId;
        public string StatePath; // Comma separated names or IDs

        public HfsmSyncMessage(uint networkId, string path)
        {
            NetworkId = networkId;
            StatePath = path;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetworkId);
            writer.Write(StatePath ?? string.Empty);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetworkId = reader.ReadUInt32();
            StatePath = reader.ReadString();
        }
    }
}
