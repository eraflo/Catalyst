using System.IO;

namespace Eraflo.UnityImportPackage.Networking
{
    /// <summary>
    /// Base interface for all network messages.
    /// </summary>
    public interface INetworkMessage
    {
        /// <summary>Serializes this message to a binary writer.</summary>
        void Serialize(BinaryWriter writer);
        
        /// <summary>Deserializes this message from a binary reader.</summary>
        void Deserialize(BinaryReader reader);
    }
}
