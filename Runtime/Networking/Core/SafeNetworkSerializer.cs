/*
 * ============================================================================
 * SAFE NETWORK SERIALIZER
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * Provides safe serialization methods with:
 * - Size validation before reading
 * - Configurable limits
 * - Exception-safe reads
 * 
 * ============================================================================
 */

using System;
using System.IO;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Extension methods for safe network deserialization.
    /// </summary>
    public static class SafeNetworkSerializer
    {
        /// <summary>Default max string length (256 characters).</summary>
        public const int DefaultMaxStringLength = 256;
        
        /// <summary>Default max array size (4KB).</summary>
        public const int DefaultMaxArraySize = 4096;

        /// <summary>
        /// Reads a string with length validation.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="maxLength">Maximum allowed length.</param>
        /// <returns>The string, or null if invalid.</returns>
        /// <exception cref="InvalidDataException">If length exceeds maximum.</exception>
        public static string ReadSafeString(this BinaryReader reader, int maxLength = DefaultMaxStringLength)
        {
            // Read length prefix (BinaryWriter.Write(string) uses 7-bit encoded length)
            int length = Read7BitEncodedInt(reader);
            
            if (length < 0)
                throw new InvalidDataException("Negative string length");
            
            if (length > maxLength)
                throw new InvalidDataException($"String length {length} exceeds max {maxLength}");
            
            if (length == 0)
                return string.Empty;
            
            // Seek back and let normal ReadString handle it
            reader.BaseStream.Position -= GetVarIntSize(length);
            return reader.ReadString();
        }

        /// <summary>
        /// Reads a byte array with size validation.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="maxSize">Maximum allowed size.</param>
        /// <returns>The byte array.</returns>
        /// <exception cref="InvalidDataException">If size exceeds maximum.</exception>
        public static byte[] ReadSafeBytes(this BinaryReader reader, int maxSize = DefaultMaxArraySize)
        {
            int length = reader.ReadInt32();
            
            if (length < 0)
                throw new InvalidDataException("Negative array length");
            
            if (length > maxSize)
                throw new InvalidDataException($"Array size {length} exceeds max {maxSize}");
            
            if (length == 0)
                return Array.Empty<byte>();
            
            return reader.ReadBytes(length);
        }

        /// <summary>
        /// Writes a byte array with length prefix.
        /// </summary>
        public static void WriteSafeBytes(this BinaryWriter writer, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                writer.Write(0);
                return;
            }
            writer.Write(data.Length);
            writer.Write(data);
        }

        /// <summary>
        /// Tries to deserialize, returning false on failure.
        /// </summary>
        public static bool TryDeserialize<T>(byte[] data, out T message) where T : struct, INetworkMessage
        {
            message = default;
            
            if (data == null || data.Length == 0)
                return false;
            
            try
            {
                message = NetworkSerializer.Deserialize<T>(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SafeNetworkSerializer] Deserialize failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads a 7-bit encoded integer.
        /// This work by reading a byte at a time and using the lower 7 bits of each byte to build the integer.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <returns>The integer.</returns>
        /// <exception cref="InvalidDataException">If the integer is invalid.</exception>
        private static int Read7BitEncodedInt(BinaryReader reader)
        {
            int result = 0;
            int shift = 0;
            byte b;
            
            do
            {
                if (shift == 35)
                    throw new InvalidDataException("Invalid 7-bit encoded int");
                    
                b = reader.ReadByte();
                result |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            
            return result;
        }

        private static int GetVarIntSize(int value)
        {
            int size = 0;
            uint v = (uint)value;
            do { size++; v >>= 7; } while (v != 0);
            return size;
        }
    }
}
