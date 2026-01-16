/*
 * ============================================================================
 * AES-GCM ENCRYPTION ALGORITHM
 * ============================================================================
 * 
 * WHAT IS AES-GCM?
 * ----------------
 * AES-GCM (Advanced Encryption Standard - Galois/Counter Mode) is an
 * authenticated encryption algorithm that provides BOTH confidentiality
 * AND integrity in a single operation.
 * 
 * HOW IT WORKS:
 * -------------
 * 1. A random 12-byte NONCE (number used once) is generated
 * 2. Data is encrypted using AES in counter mode
 * 3. A 16-byte authentication TAG is computed using Galois field math
 * 4. Output = Nonce + Ciphertext + Tag
 * 
 * OUTPUT FORMAT:
 * --------------
 * | 12 bytes NONCE | Variable CIPHERTEXT | 16 bytes TAG |
 * 
 * KEY PROPERTIES:
 * ---------------
 * - SYMMETRIC: Same key for encryption and decryption
 * - AUTHENTICATED: Detects if data was tampered with
 * - NONCE-BASED: Each encryption uses a unique random nonce
 * - FAST: Hardware-accelerated on modern CPUs (AES-NI)
 * 
 * USE CASES IN GAMES:
 * -------------------
 * - Encrypting save files
 * - Secure network payloads
 * - Protecting sensitive player data
 * - DLC/content protection
 * 
 * SECURITY NOTES:
 * ---------------
 * - NEVER reuse a nonce with the same key (catastrophic failure)
 * - Key must be kept secret (store securely, never in code)
 * - 256-bit keys provide ~128 bits of security against quantum attacks
 * 
 * WHY GCM OVER OTHER MODES?
 * -------------------------
 * - CBC: No authentication, vulnerable to padding oracle attacks
 * - CTR: No authentication, malleable ciphertext
 * - GCM: Authenticated + fast + parallelizable
 * 
 * ============================================================================
 */

using System;
using System.Security.Cryptography;

namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// AES-GCM authenticated encryption provider.
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    ///   <item>Encrypting sensitive data (save files, network payloads)</item>
    ///   <item>Protecting data integrity AND confidentiality</item>
    ///   <item>Any scenario requiring symmetric encryption</item>
    /// </list>
    /// 
    /// <para><b>Output format:</b></para>
    /// <c>[12-byte nonce][ciphertext][16-byte tag]</c>
    /// </summary>
    public class AesGcmEncryptionProvider : IEncryptionProvider
    {
        /// <inheritdoc/>
        public string Name => "AES-GCM";
        
        /// <summary>Key size in bytes (32 = 256 bits).</summary>
        public int KeySize => 32;

        private const int NonceSize = 12;  // 96 bits (GCM standard)
        private const int TagSize = 16;    // 128 bits (maximum security)

        /// <summary>
        /// Encrypts data using AES-256-GCM.
        /// </summary>
        /// <param name="plaintext">Data to encrypt.</param>
        /// <param name="key">256-bit (32-byte) encryption key.</param>
        /// <returns>Nonce + ciphertext + authentication tag.</returns>
        /// <exception cref="ArgumentException">Key is not 32 bytes.</exception>
        public byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            if (key.Length != KeySize)
                throw new ArgumentException($"Key must be {KeySize} bytes (256 bits).", nameof(key));

            // Generate random nonce (CRITICAL: must be unique per encryption)
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            // Pack: nonce + ciphertext + tag
            var result = new byte[NonceSize + ciphertext.Length + TagSize];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSize + ciphertext.Length, TagSize);
            return result;
        }

        /// <summary>
        /// Decrypts data encrypted with AES-256-GCM.
        /// </summary>
        /// <param name="data">Encrypted data (nonce + ciphertext + tag).</param>
        /// <param name="key">256-bit (32-byte) decryption key.</param>
        /// <returns>Original plaintext.</returns>
        /// <exception cref="CryptographicException">Authentication failed (data tampered or wrong key).</exception>
        public byte[] Decrypt(byte[] data, byte[] key)
        {
            if (key.Length != KeySize)
                throw new ArgumentException($"Key must be {KeySize} bytes (256 bits).", nameof(key));

            if (data.Length < NonceSize + TagSize)
                throw new ArgumentException("Data too short to contain nonce and tag.", nameof(data));

            // Unpack: nonce + ciphertext + tag
            var nonce = new byte[NonceSize];
            var ciphertextLength = data.Length - NonceSize - TagSize;
            var ciphertext = new byte[ciphertextLength];
            var tag = new byte[TagSize];

            Buffer.BlockCopy(data, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(data, NonceSize, ciphertext, 0, ciphertextLength);
            Buffer.BlockCopy(data, data.Length - TagSize, tag, 0, TagSize);

            var plaintext = new byte[ciphertextLength];
            using var aes = new AesGcm(key);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }

        /// <summary>
        /// Generates a cryptographically secure 256-bit key.
        /// </summary>
        /// <returns>32-byte random key.</returns>
        public byte[] GenerateKey()
        {
            var key = new byte[KeySize];
            RandomNumberGenerator.Fill(key);
            return key;
        }
    }
}
