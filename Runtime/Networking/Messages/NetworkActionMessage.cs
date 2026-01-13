using System.IO;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Message sent to trigger a networked action.
    /// </summary>
    public struct NetworkActionMessage : INetworkMessage
    {
        public int ActionHash;
        public byte[] Payload;

        public void Serialize(BinaryWriter w)
        {
            w.Write(ActionHash);
            w.Write(Payload?.Length ?? 0);
            if (Payload != null) w.Write(Payload);
        }

        public void Deserialize(BinaryReader r)
        {
            ActionHash = r.ReadInt32();
            int len = r.ReadInt32();
            Payload = len > 0 ? r.ReadBytes(len) : null;
        }
    }
}
