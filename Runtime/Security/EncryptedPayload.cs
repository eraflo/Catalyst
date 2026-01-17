/*
 * ============================================================================
 * ENCRYPTED PAYLOAD
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * Extension to SecureConnectionPayload that adds encryption layer on top of
 * signature verification. Provides full end-to-end encryption for sensitive
 * network data.
 * 
 * SECURITY MODEL:
 * ---------------
 * 1. Data is encrypted with AES-GCM using a shared secret
 * 2. Encrypted data is signed with HMAC for additional authentication
 * 3. Timestamp and nonce prevent replay attacks
 * 
 * KEY EXCHANGE:
 * -------------
 * Use ECDH to establish shared secrets:
 * 1. Exchange public keys during connection handshake
 * 2. Derive shared secret from ECDH
 * 3. Use shared secret as encryption key
 * 
 * ============================================================================
 */

using System;
using System.IO;

namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Encrypted payload with signature and replay protection.
    /// Combines AES-GCM encryption with HMAC signature.
    /// </summary>
    public struct EncryptedPayload
    {
        /// <summary>Encrypted data (includes AES-GCM nonce and tag).</summary>
        public byte[] EncryptedData;
        
        /// <summary>HMAC signature of the encrypted data.</summary>
        public byte[] Signature;
        
        /// <summary>Unix timestamp when payload was created.</summary>
        public long Timestamp;
        
        /// <summary>Random nonce for replay protection.</summary>
        public byte[] Nonce;

        /// <summary>
        /// Creates an encrypted and signed payload.
        /// </summary>
        /// <param name="plaintext">Data to encrypt.</param>
        /// <param name="encryptionKey">32-byte AES key (from ECDH shared secret).</param>
        /// <param name="signatureKey">Key for HMAC signature.</param>
        /// <param name="security">SecurityManager instance.</param>
        public static EncryptedPayload Create(byte[] plaintext, byte[] encryptionKey, byte[] signatureKey, SecurityManager security)
        {
            // 1. Encrypt the data
            byte[] encrypted = security.Encryption.Encrypt(plaintext, encryptionKey);
            
            var payload = new EncryptedPayload
            {
                EncryptedData = encrypted,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Nonce = security.Random.GetBytes(16)
            };
            
            // 2. Sign the encrypted data + timestamp + nonce
            payload.Signature = security.Signature.Sign(payload.GetSignatureData(), signatureKey);
            
            return payload;
        }

        /// <summary>
        /// Decrypts and validates the payload.
        /// </summary>
        /// <param name="encryptionKey">32-byte AES key.</param>
        /// <param name="signatureKey">Key for HMAC signature.</param>
        /// <param name="security">SecurityManager instance.</param>
        /// <param name="maxAgeSeconds">Maximum payload age.</param>
        /// <returns>Decrypted plaintext, or null if validation fails.</returns>
        public byte[] Decrypt(byte[] encryptionKey, byte[] signatureKey, SecurityManager security, int maxAgeSeconds = 30)
        {
            // 1. Check timestamp freshness
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - Timestamp) > maxAgeSeconds)
                return null;
            
            // 2. Validate signature
            if (!security.Signature.Verify(GetSignatureData(), Signature, signatureKey))
                return null;
            
            // 3. Decrypt
            try
            {
                return security.Encryption.Decrypt(EncryptedData, encryptionKey);
            }
            catch
            {
                return null; // Decryption failed (tampered or wrong key)
            }
        }

        private byte[] GetSignatureData()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write(EncryptedData?.Length ?? 0);
            if (EncryptedData != null) writer.Write(EncryptedData);
            writer.Write(Timestamp);
            if (Nonce != null) writer.Write(Nonce);
            
            return ms.ToArray();
        }

        /// <summary>
        /// Serializes for network transmission.
        /// </summary>
        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write(EncryptedData?.Length ?? 0);
            if (EncryptedData != null) writer.Write(EncryptedData);
            
            writer.Write(Signature?.Length ?? 0);
            if (Signature != null) writer.Write(Signature);
            
            writer.Write(Timestamp);
            
            writer.Write(Nonce?.Length ?? 0);
            if (Nonce != null) writer.Write(Nonce);
            
            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes from network data.
        /// </summary>
        public static EncryptedPayload Deserialize(byte[] raw)
        {
            using var ms = new MemoryStream(raw);
            using var reader = new BinaryReader(ms);
            
            var payload = new EncryptedPayload();
            
            int dataLen = reader.ReadInt32();
            if (dataLen > 0 && dataLen < 1048576) // Max 1MB
                payload.EncryptedData = reader.ReadBytes(dataLen);
            
            int sigLen = reader.ReadInt32();
            if (sigLen > 0 && sigLen <= 64)
                payload.Signature = reader.ReadBytes(sigLen);
            
            payload.Timestamp = reader.ReadInt64();
            
            int nonceLen = reader.ReadInt32();
            if (nonceLen > 0 && nonceLen <= 32)
                payload.Nonce = reader.ReadBytes(nonceLen);
            
            return payload;
        }
    }
}
