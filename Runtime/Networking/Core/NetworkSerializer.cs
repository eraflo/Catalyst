using System;
using System.IO;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
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
        #region Generic Value Serialization

        public static byte[] SerializeValue<T>(T value)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                WriteValue(writer, value);
                return stream.ToArray();
            }
        }

        public static T DeserializeValue<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default;
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                return ReadValue<T>(reader);
            }
        }

        private static void WriteValue<T>(BinaryWriter writer, T value)
        {
            Type t = typeof(T);
            if (t == typeof(int)) writer.Write((int)(object)value);
            else if (t == typeof(float)) writer.Write((float)(object)value);
            else if (t == typeof(bool)) writer.Write((bool)(object)value);
            else if (t == typeof(string)) writer.Write((string)(object)value ?? string.Empty);
            else if (t == typeof(Vector3)) WriteVector3(writer, (Vector3)(object)value);
            else if (t == typeof(Quaternion)) WriteQuaternion(writer, (Quaternion)(object)value);
            else if (t == typeof(byte)) writer.Write((byte)(object)value);
            else if (t == typeof(long)) writer.Write((long)(object)value);
            else if (t == typeof(double)) writer.Write((double)(object)value);
            else throw new NotSupportedException($"Type {t.Name} is not supported for automatic network serialization.");
        }

        private static T ReadValue<T>(BinaryReader reader)
        {
            Type t = typeof(T);
            if (t == typeof(int)) return (T)(object)reader.ReadInt32();
            if (t == typeof(float)) return (T)(object)reader.ReadSingle();
            if (t == typeof(bool)) return (T)(object)reader.ReadBoolean();
            if (t == typeof(string)) return (T)(object)reader.ReadString();
            if (t == typeof(Vector3)) return (T)(object)ReadVector3(reader);
            if (t == typeof(Quaternion)) return (T)(object)ReadQuaternion(reader);
            if (t == typeof(byte)) return (T)(object)reader.ReadByte();
            if (t == typeof(long)) return (T)(object)reader.ReadInt64();
            if (t == typeof(double)) return (T)(object)reader.ReadDouble();
            throw new NotSupportedException($"Type {t.Name} is not supported for automatic network serialization.");
        }

        #endregion
    }
}
