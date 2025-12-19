using System;
using System.IO;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Networking
{
    /// <summary>
    /// Serialization utilities for network messages.
    /// Uses binary serialization for performance.
    /// </summary>
    public static class NetworkSerializer
    {
        /// <summary>
        /// Serializes a message to bytes.
        /// </summary>
        public static byte[] Serialize<T>(T message) where T : struct, INetworkMessage
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                message.Serialize(writer);
                return stream.ToArray();
            }
        }
        
        /// <summary>
        /// Deserializes bytes to a message.
        /// </summary>
        public static T Deserialize<T>(byte[] data) where T : struct, INetworkMessage
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                var message = default(T);
                message.Deserialize(reader);
                return message;
            }
        }
        
        // Helper methods for common types
        public static void WriteVector3(BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }
        
        public static Vector3 ReadVector3(BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
        
        public static void WriteQuaternion(BinaryWriter writer, Quaternion q)
        {
            writer.Write(q.x);
            writer.Write(q.y);
            writer.Write(q.z);
            writer.Write(q.w);
        }
        
        public static Quaternion ReadQuaternion(BinaryReader reader)
        {
            return new Quaternion(
                reader.ReadSingle(), 
                reader.ReadSingle(), 
                reader.ReadSingle(), 
                reader.ReadSingle()
            );
        }
    }
}
