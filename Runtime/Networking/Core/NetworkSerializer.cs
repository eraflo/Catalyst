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
            if (value is int i) writer.Write(i);
            else if (value is float f) writer.Write(f);
            else if (value is bool b) writer.Write(b);
            else if (value is string s) writer.Write(s ?? string.Empty);
            else if (value is Vector3 v) WriteVector3(writer, v);
            else if (value is Quaternion q) WriteQuaternion(writer, q);
            else if (value is byte b2) writer.Write(b2);
            else if (value is long l) writer.Write(l);
            else if (value is double d) writer.Write(d);
            else throw new NotSupportedException($"Type {typeof(T).Name} is not supported for automatic network serialization.");
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
