using System;
using System.IO;
using NUnit.Framework;
using Eraflo.Catalyst.Networking.Features.Connection;
using Eraflo.Catalyst.Security;

namespace Eraflo.Catalyst.Tests.Networking
{
    /// <summary>
    /// Tests for SecureConnectionPayload signature, timestamp, and serialization.
    /// </summary>
    public class SecureConnectionPayloadTests
    {
        private MockSignatureProvider _signature;
        private MockRandomProvider _random;
        private byte[] _testKey;
        private byte[] _testData;

        [SetUp]
        public void SetUp()
        {
            _signature = new MockSignatureProvider();
            _random = new MockRandomProvider();
            _testKey = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            _testData = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        }

        [Test]
        public void Create_GeneratesValidPayload()
        {
            var payload = SecureConnectionPayload.Create(_testData, _signature, _random, _testKey);

            Assert.IsNotNull(payload.Data);
            Assert.IsNotNull(payload.Signature);
            Assert.IsNotNull(payload.Nonce);
            Assert.AreEqual(_testData, payload.Data);
            Assert.AreEqual(16, payload.Nonce.Length);
            Assert.Greater(payload.Timestamp, 0);
        }

        [Test]
        public void Validate_ValidSignature_ReturnsTrue()
        {
            var payload = SecureConnectionPayload.Create(_testData, _signature, _random, _testKey);

            bool isValid = payload.Validate(_signature, _testKey, maxAgeSeconds: 60);

            Assert.IsTrue(isValid);
        }

        [Test]
        public void Validate_InvalidSignature_ReturnsFalse()
        {
            var payload = SecureConnectionPayload.Create(_testData, _signature, _random, _testKey);
            
            // Tamper with the signature
            payload.Signature[0] ^= 0xFF;

            bool isValid = payload.Validate(_signature, _testKey, maxAgeSeconds: 60);

            Assert.IsFalse(isValid);
        }

        [Test]
        public void Validate_ExpiredTimestamp_ReturnsFalse()
        {
            var payload = SecureConnectionPayload.Create(_testData, _signature, _random, _testKey);
            
            // Set timestamp to 2 minutes ago
            payload.Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2).ToUnixTimeSeconds();
            // Re-sign with the old timestamp
            payload.Signature = _signature.Sign(GetSignatureData(payload), _testKey);

            bool isValid = payload.Validate(_signature, _testKey, maxAgeSeconds: 30);

            Assert.IsFalse(isValid);
        }

        [Test]
        public void Validate_FutureTimestamp_ReturnsFalse()
        {
            var payload = SecureConnectionPayload.Create(_testData, _signature, _random, _testKey);
            
            // Set timestamp to 2 minutes in the future
            payload.Timestamp = DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds();
            payload.Signature = _signature.Sign(GetSignatureData(payload), _testKey);

            bool isValid = payload.Validate(_signature, _testKey, maxAgeSeconds: 30);

            Assert.IsFalse(isValid);
        }

        [Test]
        public void SerializeDeserialize_RoundTrip_Preserves()
        {
            var original = SecureConnectionPayload.Create(_testData, _signature, _random, _testKey);

            byte[] serialized = original.Serialize();
            var restored = SecureConnectionPayload.Deserialize(serialized);

            Assert.AreEqual(original.Data, restored.Data);
            Assert.AreEqual(original.Signature, restored.Signature);
            Assert.AreEqual(original.Timestamp, restored.Timestamp);
            Assert.AreEqual(original.Nonce, restored.Nonce);
        }

        [Test]
        public void Deserialize_MalformedData_ThrowsException()
        {
            // Data too short to be a valid payload
            var malformed = new byte[] { 0x00, 0x00, 0x00, 0x00 };

            // Deserialize expects more data (sig length, timestamp, nonce length)
            Assert.Throws<EndOfStreamException>(() => SecureConnectionPayload.Deserialize(malformed));
        }

        // Helper to replicate GetSignatureData logic
        private byte[] GetSignatureData(SecureConnectionPayload payload)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write(payload.Data?.Length ?? 0);
            if (payload.Data != null) writer.Write(payload.Data);
            writer.Write(payload.Timestamp);
            if (payload.Nonce != null) writer.Write(payload.Nonce);
            
            return ms.ToArray();
        }
    }

    /// <summary>
    /// Mock signature provider for testing.
    /// </summary>
    internal class MockSignatureProvider : ISignatureProvider
    {
        public string Name => "Mock";

        public byte[] Sign(byte[] data, byte[] key)
        {
            // Simple XOR-based mock signature
            var sig = new byte[32];
            for (int i = 0; i < data.Length && i < sig.Length; i++)
                sig[i] = (byte)(data[i] ^ (i < key.Length ? key[i] : 0));
            return sig;
        }

        public bool Verify(byte[] data, byte[] signature, byte[] key)
        {
            var expected = Sign(data, key);
            if (expected.Length != signature.Length) return false;
            
            // Constant-time comparison
            int diff = 0;
            for (int i = 0; i < expected.Length; i++)
                diff |= expected[i] ^ signature[i];
            return diff == 0;
        }
    }

    /// <summary>
    /// Mock random provider for testing.
    /// </summary>
    internal class MockRandomProvider : IRandomProvider
    {
        private byte _counter = 0;

        public void Fill(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = _counter++;
        }

        public byte[] GetBytes(int count)
        {
            var bytes = new byte[count];
            Fill(bytes);
            return bytes;
        }

        public int GetInt(int minInclusive, int maxExclusive)
        {
            return minInclusive + (_counter++ % (maxExclusive - minInclusive));
        }
    }
}
