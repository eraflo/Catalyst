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

        public static byte[] SerializeValues(params object[] values)
        {
            if (values == null || values.Length == 0) return Array.Empty<byte>();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(values.Length);
                foreach (var val in values)
                {
                    WriteValue(writer, val);
                }
                return stream.ToArray();
            }
        }

        public static object[] DeserializeValues(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<object>();
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                int count = reader.ReadInt32();
                var values = new object[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = ReadValue(reader);
                }
                return values;
            }
        }

        private static void WriteValue(BinaryWriter writer, object value)
        {
            if (value == null) { writer.Write((byte)0); return; }

            if (value is int i) { writer.Write((byte)1); writer.Write(i); }
            else if (value is float f) { writer.Write((byte)2); writer.Write(f); }
            else if (value is bool b) { writer.Write((byte)3); writer.Write(b); }
            else if (value is string s) { writer.Write((byte)4); writer.Write(s); }
            else if (value is Vector3 v) { writer.Write((byte)5); WriteVector3(writer, v); }
            else if (value is Quaternion q) { writer.Write((byte)6); WriteQuaternion(writer, q); }
            else if (value is byte bt) { writer.Write((byte)7); writer.Write(bt); }
            else if (value is long l) { writer.Write((byte)8); writer.Write(l); }
            else if (value is double d) { writer.Write((byte)9); writer.Write(d); }
            else throw new NotSupportedException($"Type {value.GetType().Name} is not supported for automatic network serialization.");
        }

        private static object ReadValue(BinaryReader reader)
        {
            byte typeCode = reader.ReadByte();
            switch (typeCode)
            {
                case 0: return null;
                case 1: return reader.ReadInt32();
                case 2: return reader.ReadSingle();
                case 3: return reader.ReadBoolean();
                case 4: return reader.ReadString();
                case 5: return ReadVector3(reader);
                case 6: return ReadQuaternion(reader);
                case 7: return reader.ReadByte();
                case 8: return reader.ReadInt64();
                case 9: return reader.ReadDouble();
                default: throw new NotSupportedException($"TypeCode {typeCode} is not supported.");
            }
        }

        public static byte[] SerializeValue<T>(T value)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                WriteSpecificValue(writer, value);
                return stream.ToArray();
            }
        }

        public static T DeserializeValue<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default;
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                return ReadSpecificValue<T>(reader);
            }
        }

        private static void WriteSpecificValue<T>(BinaryWriter writer, T value)
        {
            Type t = typeof(T);
            if (typeof(INetworkMessage).IsAssignableFrom(t))
            {
                ((INetworkMessage)value).Serialize(writer);
                return;
            }

            if (t == typeof(int)) writer.Write((int)(object)value);
            else if (t == typeof(float)) writer.Write((float)(object)value);
            else if (t == typeof(bool)) writer.Write((bool)(object)value);
            else if (t == typeof(string)) writer.Write((string)(object)value ?? string.Empty);
            else if (t == typeof(Vector3)) WriteVector3(writer, (Vector3)(object)value);
            else if (t == typeof(Quaternion)) WriteQuaternion(writer, (Quaternion)(object)value);
            else if (t == typeof(byte)) writer.Write((byte)(object)value);
            else if (t == typeof(long)) writer.Write((long)(object)value);
            else if (t == typeof(double)) writer.Write((double)(object)value);
            else if (t == typeof(object[]))
            {
                var arr = (object[])(object)value;
                writer.Write(arr.Length);
                foreach (var o in arr) WriteValue(writer, o);
            }
            else throw new NotSupportedException($"Type {t.Name} is not supported for automatic network serialization.");
        }

        private static T ReadSpecificValue<T>(BinaryReader reader)
        {
            Type t = typeof(T);
            if (typeof(INetworkMessage).IsAssignableFrom(t))
            {
                var message = (INetworkMessage)Activator.CreateInstance(t);
                message.Deserialize(reader);
                return (T)message;
            }

            if (t == typeof(int)) return (T)(object)reader.ReadInt32();
            if (t == typeof(float)) return (T)(object)reader.ReadSingle();
            if (t == typeof(bool)) return (T)(object)reader.ReadBoolean();
            if (t == typeof(string)) return (T)(object)reader.ReadString();
            if (t == typeof(Vector3)) return (T)(object)ReadVector3(reader);
            if (t == typeof(Quaternion)) return (T)(object)ReadQuaternion(reader);
            if (t == typeof(byte)) return (T)(object)reader.ReadByte();
            if (t == typeof(long)) return (T)(object)reader.ReadInt64();
            if (t == typeof(double)) return (T)(object)reader.ReadDouble();
            if (t == typeof(object[]))
            {
                int count = reader.ReadInt32();
                var arr = new object[count];
                for (int i = 0; i < count; i++) arr[i] = ReadValue(reader);
                return (T)(object)arr;
            }
            throw new NotSupportedException($"Type {t.Name} is not supported for automatic network serialization.");
        }

        #endregion
    }
}
