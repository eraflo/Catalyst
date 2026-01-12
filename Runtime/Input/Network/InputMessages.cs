using Eraflo.Catalyst.Networking;
using System.Collections.Generic;

namespace Eraflo.Catalyst.InputSystem.Network
{
    /// <summary>
    /// Message sent to synchronize raw inputs (for server-authoritative combos).
    /// </summary>
    public struct InputSyncMessage : INetworkMessage
    {
        public struct InputData
        {
            public string ActionId;
            public float Timestamp;
        }

        public List<InputData> Inputs;

        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(Inputs?.Count ?? 0);
            if (Inputs != null)
            {
                foreach (var input in Inputs)
                {
                    writer.Write(input.ActionId);
                    writer.Write(input.Timestamp);
                }
            }
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Inputs = new List<InputData>(count);
            for (int i = 0; i < count; i++)
            {
                Inputs.Add(new InputData
                {
                    ActionId = reader.ReadString(),
                    Timestamp = reader.ReadSingle()
                });
            }
        }
    }

    /// <summary>
    /// Message sent when a combo is executed.
    /// </summary>
    public struct ComboExecutedMessage : INetworkMessage
    {
        public string ComboId;
        public ulong ClientId; // Who did it

        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(ComboId);
            writer.Write(ClientId);
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            ComboId = reader.ReadString();
            ClientId = reader.ReadUInt64();
        }
    }
}
