/*
 * ============================================================================
 * SECURE CONNECTION TYPES
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * Provides secure connection payload structures with:
 * - HMAC signature verification
 * - Timestamp-based replay attack prevention
 * - Timing-safe comparison
 * 
 * SECURITY MODEL:
 * ---------------
 * 1. Client creates payload with data + timestamp
 * 2. Client signs payload with shared secret (from SecurityManager)
 * 3. Server validates signature and timestamp freshness
 * 4. Prevents: payload forgery, replay attacks, timing attacks
 * 
 * ============================================================================
 */

using System;
using System.IO;
using Eraflo.Catalyst.Security;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Connection
{
    /// <summary>
    /// Secure connection payload with signature and replay protection.
    /// </summary>
    public struct SecureConnectionPayload
    {
        /// <summary>The actual payload data.</summary>
        public byte[] Data;
        
        /// <summary>HMAC signature of (Data + Timestamp).</summary>
        public byte[] Signature;
        
        /// <summary>Unix timestamp (seconds) when payload was created.</summary>
        public long Timestamp;
        
        /// <summary>Random nonce for additional replay protection.</summary>
        public byte[] Nonce;

        /// <summary>
        /// Creates a signed payload.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="security">SecurityManager instance.</param>
        /// <param name="key">Shared secret key.</param>
        public static SecureConnectionPayload Create(byte[] data, SecurityManager security, byte[] key)
        {
            var payload = new SecureConnectionPayload
            {
                Data = data,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Nonce = security.Random.GetBytes(16)  // Use IRandomProvider
            };
            
            payload.Signature = security.Signature.Sign(payload.GetSignatureData(), key);
            
            return payload;
        }

        /// <summary>
        /// Creates a signed payload using ISignatureProvider directly.
        /// </summary>
        public static SecureConnectionPayload Create(byte[] data, ISignatureProvider signatureProvider, IRandomProvider randomProvider, byte[] key)
        {
            var payload = new SecureConnectionPayload
            {
                Data = data,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Nonce = randomProvider.GetBytes(16)
            };
            
            payload.Signature = signatureProvider.Sign(payload.GetSignatureData(), key);
            
            return payload;
        }

        /// <summary>
        /// Validates the payload signature and timestamp.
        /// </summary>
        /// <param name="signatureProvider">Signature provider.</param>
        /// <param name="key">Shared secret key.</param>
        /// <param name="maxAgeSeconds">Maximum age of payload (default: 30 seconds).</param>
        /// <returns>True if valid.</returns>
        public bool Validate(ISignatureProvider signatureProvider, byte[] key, int maxAgeSeconds = 30)
        {
            // Check timestamp freshness
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - Timestamp) > maxAgeSeconds)
            {
                Debug.LogWarning("[SecureConnectionPayload] Timestamp expired or future");
                return false;
            }

            // Validate signature (timing-safe via provider)
            return signatureProvider.Verify(GetSignatureData(), Signature, key);
        }

        private byte[] GetSignatureData()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write(Data?.Length ?? 0);
            if (Data != null) writer.Write(Data);
            writer.Write(Timestamp);
            if (Nonce != null) writer.Write(Nonce);
            
            return ms.ToArray();
        }

        /// <summary>
        /// Serializes the payload for network transmission.
        /// </summary>
        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write(Data?.Length ?? 0);
            if (Data != null) writer.Write(Data);
            
            writer.Write(Signature?.Length ?? 0);
            if (Signature != null) writer.Write(Signature);
            
            writer.Write(Timestamp);
            
            writer.Write(Nonce?.Length ?? 0);
            if (Nonce != null) writer.Write(Nonce);
            
            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes a payload from network data.
        /// </summary>
        public static SecureConnectionPayload Deserialize(byte[] raw)
        {
            using var ms = new MemoryStream(raw);
            using var reader = new BinaryReader(ms);
            
            var payload = new SecureConnectionPayload();
            
            int dataLen = reader.ReadInt32();
            if (dataLen > 0 && dataLen < 65536) // Max 64KB
                payload.Data = reader.ReadBytes(dataLen);
            
            int sigLen = reader.ReadInt32();
            if (sigLen > 0 && sigLen <= 64) // Max 512-bit signature
                payload.Signature = reader.ReadBytes(sigLen);
            
            payload.Timestamp = reader.ReadInt64();
            
            int nonceLen = reader.ReadInt32();
            if (nonceLen > 0 && nonceLen <= 32) // Max 256-bit nonce
                payload.Nonce = reader.ReadBytes(nonceLen);
            
            return payload;
        }
    }

    /// <summary>
    /// Connection security configuration.
    /// </summary>
    public class ConnectionSecurityConfig
    {
        /// <summary>Enable secure payloads (signature + timestamp).</summary>
        public bool EnableSecurePayloads { get; set; } = true;
        
        /// <summary>Maximum payload age in seconds.</summary>
        public int MaxPayloadAgeSeconds { get; set; } = 30;
        
        /// <summary>Maximum connection attempts before temporary ban.</summary>
        public int MaxAttemptsPerMinute { get; set; } = 5;
        
        /// <summary>Ban duration in seconds after exceeding attempts.</summary>
        public int BanDurationSeconds { get; set; } = 60;
    }
}
