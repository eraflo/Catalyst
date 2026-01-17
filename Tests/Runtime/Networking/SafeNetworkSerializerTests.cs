using System;
using System.IO;
using NUnit.Framework;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.Tests.Networking
{
    /// <summary>
    /// Tests for SafeNetworkSerializer safe deserialization methods.
    /// </summary>
    public class SafeNetworkSerializerTests
    {
        [Test]
        public void ReadSafeString_ValidString_ReturnsString()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write("Hello, World!");
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);
            string result = reader.ReadSafeString(maxLength: 100);

            Assert.AreEqual("Hello, World!", result);
        }

        [Test]
        public void ReadSafeString_ExceedsMaxLength_ThrowsInvalidDataException()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write("This is a very long string that exceeds the limit");
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);

            Assert.Throws<InvalidDataException>(() => reader.ReadSafeString(maxLength: 10));
        }

        [Test]
        public void ReadSafeString_EmptyString_ReturnsEmpty()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(string.Empty);
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);
            string result = reader.ReadSafeString(maxLength: 100);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ReadSafeBytes_ValidBytes_ReturnsBytes()
        {
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(testData.Length);
                writer.Write(testData);
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);
            byte[] result = reader.ReadSafeBytes(maxSize: 100);

            Assert.AreEqual(testData, result);
        }

        [Test]
        public void ReadSafeBytes_ExceedsMaxSize_ThrowsInvalidDataException()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(1000); // Claim 1000 bytes
                writer.Write(new byte[100]); // But only write 100
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);

            Assert.Throws<InvalidDataException>(() => reader.ReadSafeBytes(maxSize: 50));
        }

        [Test]
        public void ReadSafeBytes_NegativeLength_ThrowsInvalidDataException()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(-1); // Negative length
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);

            Assert.Throws<InvalidDataException>(() => reader.ReadSafeBytes(maxSize: 100));
        }

        [Test]
        public void ReadSafeBytes_ZeroLength_ReturnsEmptyArray()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0);
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);
            byte[] result = reader.ReadSafeBytes(maxSize: 100);

            Assert.AreEqual(Array.Empty<byte>(), result);
        }

        [Test]
        public void WriteSafeBytes_ValidBytes_WritesWithLengthPrefix()
        {
            var testData = new byte[] { 0xAA, 0xBB, 0xCC };
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.WriteSafeBytes(testData);
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);
            int length = reader.ReadInt32();
            byte[] result = reader.ReadBytes(length);

            Assert.AreEqual(3, length);
            Assert.AreEqual(testData, result);
        }

        [Test]
        public void WriteSafeBytes_NullBytes_WritesZeroLength()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.WriteSafeBytes(null);
            }
            ms.Position = 0;

            using var reader = new BinaryReader(ms);
            int length = reader.ReadInt32();

            Assert.AreEqual(0, length);
        }

        [Test]
        public void TryDeserialize_ValidData_ReturnsTrue()
        {
            var original = new TestSerializableMessage { Value = 42 };
            byte[] data = NetworkSerializer.Serialize(original);

            bool success = SafeNetworkSerializer.TryDeserialize<TestSerializableMessage>(data, out var result);

            Assert.IsTrue(success);
            Assert.AreEqual(42, result.Value);
        }

        [Test]
        public void TryDeserialize_NullData_ReturnsFalse()
        {
            bool success = SafeNetworkSerializer.TryDeserialize<TestSerializableMessage>(null, out var result);

            Assert.IsFalse(success);
        }

        [Test]
        public void TryDeserialize_EmptyData_ReturnsFalse()
        {
            bool success = SafeNetworkSerializer.TryDeserialize<TestSerializableMessage>(Array.Empty<byte>(), out var result);

            Assert.IsFalse(success);
        }

        [Test]
        public void TryDeserialize_MalformedData_ReturnsFalse()
        {
            // Incomplete data that will fail during deserialization
            var malformed = new byte[] { 0x00 };

            bool success = SafeNetworkSerializer.TryDeserialize<TestSerializableMessage>(malformed, out var result);

            Assert.IsFalse(success);
        }
    }

    /// <summary>
    /// Test message for serialization tests.
    /// </summary>
    public struct TestSerializableMessage : INetworkMessage
    {
        public int Value;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadInt32();
        }
    }
}
