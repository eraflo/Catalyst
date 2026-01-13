using System.IO;

namespace Eraflo.Catalyst.Networking
{
    // --- Shared Operations ---

    public enum ListOperation : byte
    {
        Add,
        Remove,
        Set,
        Clear,
        Insert
    }

    public enum DictionaryOperation : byte
    {
        Add,
        Remove,
        Set,
        Clear
    }

    public enum SetOperation : byte
    {
        Add,
        Remove,
        Clear
    }

    public enum QueueOperation : byte
    {
        Enqueue,
        Dequeue,
        Clear
    }

    public enum StackOperation : byte
    {
        Push,
        Pop,
        Clear
    }

    // --- Messages ---

    /// <summary>
    /// Message sent to synchronize a specific operation on a NetworkList.
    /// </summary>
    public struct NetworkListDeltaMessage : INetworkMessage
    {
        public uint NetworkId;
        public ulong SenderId;
        public string CollectionName;
        public ListOperation Operation;
        public int Index;
        public byte[] Data;

        public void Serialize(BinaryWriter w)
        {
            w.Write(NetworkId);
            w.Write(SenderId);
            w.Write(CollectionName ?? "");
            w.Write((byte)Operation);
            w.Write(Index);
            w.Write(Data?.Length ?? 0);
            if (Data != null) w.Write(Data);
        }

        public void Deserialize(BinaryReader r)
        {
            NetworkId = r.ReadUInt32();
            SenderId = r.ReadUInt64();
            CollectionName = r.ReadString();
            Operation = (ListOperation)r.ReadByte();
            Index = r.ReadInt32();
            int len = r.ReadInt32();
            Data = len > 0 ? r.ReadBytes(len) : null;
        }
    }

    /// <summary>
    /// Message sent to synchronize a specific operation on a NetworkDictionary.
    /// </summary>
    public struct NetworkDictionaryDeltaMessage : INetworkMessage
    {
        public uint NetworkId;
        public ulong SenderId;
        public string CollectionName;
        public DictionaryOperation Operation;
        public byte[] KeyData;
        public byte[] ValueData;

        public void Serialize(BinaryWriter w)
        {
            w.Write(NetworkId);
            w.Write(SenderId);
            w.Write(CollectionName ?? "");
            w.Write((byte)Operation);
            w.Write(KeyData?.Length ?? 0);
            if (KeyData != null) w.Write(KeyData);
            w.Write(ValueData?.Length ?? 0);
            if (ValueData != null) w.Write(ValueData);
        }

        public void Deserialize(BinaryReader r)
        {
            NetworkId = r.ReadUInt32();
            SenderId = r.ReadUInt64();
            CollectionName = r.ReadString();
            Operation = (DictionaryOperation)r.ReadByte();
            int keyLen = r.ReadInt32();
            KeyData = keyLen > 0 ? r.ReadBytes(keyLen) : null;
            int valLen = r.ReadInt32();
            ValueData = valLen > 0 ? r.ReadBytes(valLen) : null;
        }
    }

    /// <summary>
    /// Message sent to synchronize a specific operation on a NetworkHashSet.
    /// </summary>
    public struct NetworkHashSetDeltaMessage : INetworkMessage
    {
        public uint NetworkId;
        public ulong SenderId;
        public string CollectionName;
        public SetOperation Operation;
        public byte[] Data;

        public void Serialize(BinaryWriter w)
        {
            w.Write(NetworkId);
            w.Write(SenderId);
            w.Write(CollectionName ?? "");
            w.Write((byte)Operation);
            w.Write(Data?.Length ?? 0);
            if (Data != null) w.Write(Data);
        }

        public void Deserialize(BinaryReader r)
        {
            NetworkId = r.ReadUInt32();
            SenderId = r.ReadUInt64();
            CollectionName = r.ReadString();
            Operation = (SetOperation)r.ReadByte();
            int len = r.ReadInt32();
            Data = len > 0 ? r.ReadBytes(len) : null;
        }
    }

    /// <summary>
    /// Message sent to synchronize a specific operation on a NetworkQueue.
    /// </summary>
    public struct NetworkQueueDeltaMessage : INetworkMessage
    {
        public uint NetworkId;
        public ulong SenderId;
        public string CollectionName;
        public QueueOperation Operation;
        public byte[] Data;

        public void Serialize(BinaryWriter w)
        {
            w.Write(NetworkId);
            w.Write(SenderId);
            w.Write(CollectionName ?? "");
            w.Write((byte)Operation);
            w.Write(Data?.Length ?? 0);
            if (Data != null) w.Write(Data);
        }

        public void Deserialize(BinaryReader r)
        {
            NetworkId = r.ReadUInt32();
            SenderId = r.ReadUInt64();
            CollectionName = r.ReadString();
            Operation = (QueueOperation)r.ReadByte();
            int len = r.ReadInt32();
            Data = len > 0 ? r.ReadBytes(len) : null;
        }
    }

    /// <summary>
    /// Message sent to synchronize a specific operation on a NetworkStack.
    /// </summary>
    public struct NetworkStackDeltaMessage : INetworkMessage
    {
        public uint NetworkId;
        public ulong SenderId;
        public string CollectionName;
        public StackOperation Operation;
        public byte[] Data;

        public void Serialize(BinaryWriter w)
        {
            w.Write(NetworkId);
            w.Write(SenderId);
            w.Write(CollectionName ?? "");
            w.Write((byte)Operation);
            w.Write(Data?.Length ?? 0);
            if (Data != null) w.Write(Data);
        }

        public void Deserialize(BinaryReader r)
        {
            NetworkId = r.ReadUInt32();
            SenderId = r.ReadUInt64();
            CollectionName = r.ReadString();
            Operation = (StackOperation)r.ReadByte();
            int len = r.ReadInt32();
            Data = len > 0 ? r.ReadBytes(len) : null;
        }
    }
}
