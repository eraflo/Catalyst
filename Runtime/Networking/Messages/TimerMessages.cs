using System.IO;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Network message for timer synchronization.
    /// </summary>
    public struct TimerSyncMessage : INetworkMessage
    {
        public uint NetworkId;
        public float RemainingTime;
        public float Progress;
        public bool IsRunning;
        public bool IsFinished;
        public bool IsPaused;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetworkId);
            writer.Write(RemainingTime);
            writer.Write(Progress);
            writer.Write(IsRunning);
            writer.Write(IsFinished);
            writer.Write(IsPaused);
        }

        public void Deserialize(BinaryReader reader)
        {
            NetworkId = reader.ReadUInt32();
            RemainingTime = reader.ReadSingle();
            Progress = reader.ReadSingle();
            IsRunning = reader.ReadBoolean();
            IsFinished = reader.ReadBoolean();
            IsPaused = reader.ReadBoolean();
        }
    }

    /// <summary>
    /// Network message for timer creation.
    /// </summary>
    public struct TimerCreateMessage : INetworkMessage
    {
        public uint NetworkId;
        public float Duration;
        public string TimerType;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NetworkId);
            writer.Write(Duration);
            writer.Write(TimerType ?? "");
        }

        public void Deserialize(BinaryReader reader)
        {
            NetworkId = reader.ReadUInt32();
            Duration = reader.ReadSingle();
            TimerType = reader.ReadString();
        }
    }

    /// <summary>
    /// Network message for timer cancellation.
    /// </summary>
    public struct TimerCancelMessage : INetworkMessage
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
