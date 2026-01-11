using System;
using System.IO;
using Eraflo.Catalyst.EasingSystem;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Synchronizes a time scale transition across the network.
    /// </summary>
    [Serializable]
    public struct ChronosSyncMessage : INetworkMessage
    {
        public string ChannelId;
        public float TargetScale;
        public float Duration;
        public EasingType EaseType;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ChannelId);
            writer.Write(TargetScale);
            writer.Write(Duration);
            writer.Write((int)EaseType);
        }

        public void Deserialize(BinaryReader reader)
        {
            ChannelId = reader.ReadString();
            TargetScale = reader.ReadSingle();
            Duration = reader.ReadSingle();
            EaseType = (EasingType)reader.ReadInt32();
        }
    }
}
