/*
 * ============================================================================
 * SHA-256 HASH ALGORITHM
 * ============================================================================
 * 
 * WHAT IS SHA-256?
 * ----------------
 * SHA-256 (Secure Hash Algorithm 256-bit) is a cryptographic hash function
 * that produces a 256-bit (32-byte) hash value, typically rendered as a
 * 64-character hexadecimal string.
 * 
 * HOW IT WORKS:
 * -------------
 * 1. The input is padded to a multiple of 512 bits
 * 2. The input is divided into 512-bit blocks
 * 3. Each block goes through 64 rounds of compression
 * 4. The final output is a 256-bit digest
 * 
 * KEY PROPERTIES:
 * ---------------
 * - DETERMINISTIC: Same input always produces same output
 * - ONE-WAY: Cannot reverse the hash to get the original input
 * - COLLISION-RESISTANT: Extremely unlikely two inputs produce same hash
 * - AVALANCHE EFFECT: Small input change = completely different output
 * 
 * USE CASES IN GAMES:
 * -------------------
 * - Password storage (hash passwords, never store plaintext)
 * - Data integrity verification (detect file tampering)
 * - Unique identifiers (hash player ID + session for tokens)
 * - Anti-cheat checksums
 * 
 * SECURITY NOTES:
 * ---------------
 * - SHA-256 is NOT password hashing (use Argon2/bcrypt for passwords)
 * - For passwords, consider adding salt + stretching
 * - SHA-256 is fast, which makes brute-force attacks easier on passwords
 * 
 * ============================================================================
 */

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// SHA-256 cryptographic hash provider.
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    ///   <item>Verifying data integrity (file checksums)</item>
    ///   <item>Creating unique identifiers from data</item>
    ///   <item>Quick password comparison (NOT storage - use Argon2 for that)</item>
    /// </list>
    /// 
    /// <para><b>When NOT to use:</b></para>
    /// <list type="bullet">
    ///   <item>Long-term password storage (use Argon2/bcrypt instead)</item>
    ///   <item>Encryption (hashing is one-way, not reversible)</item>
    /// </list>
    /// </summary>
    public class Sha256HashProvider : IHashProvider
    {
        /// <inheritdoc/>
        public string Name => "SHA256";

        /// <summary>
        /// Computes SHA-256 hash of raw bytes.
        /// </summary>
        /// <param name="data">Input bytes to hash.</param>
        /// <returns>32-byte hash.</returns>
        public byte[] Hash(byte[] data)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(data);
        }

        /// <summary>
        /// Computes SHA-256 hash of a string (UTF-8 encoded).
        /// </summary>
        /// <param name="input">String to hash.</param>
        /// <returns>32-byte hash.</returns>
        public byte[] Hash(string input)
        {
            return Hash(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Computes SHA-256 hash and returns as lowercase hexadecimal string.
        /// </summary>
        /// <param name="input">String to hash.</param>
        /// <returns>64-character hex string (e.g., "a3b2c1...").</returns>
        public string HashToHex(string input)
        {
            return BitConverter.ToString(Hash(input)).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Verifies that data matches an expected hash.
        /// </summary>
        /// <param name="data">Data to verify.</param>
        /// <param name="expectedHash">Expected 32-byte hash.</param>
        /// <returns>True if hashes match.</returns>
        public bool Verify(byte[] data, byte[] expectedHash)
        {
            var computed = Hash(data);
            return computed.SequenceEqual(expectedHash);
        }

        /// <summary>
        /// Verifies that a string matches an expected hash (hex format).
        /// </summary>
        /// <param name="input">String to verify.</param>
        /// <param name="expectedHashHex">Expected 64-character hex hash.</param>
        /// <returns>True if hashes match.</returns>
        public bool Verify(string input, string expectedHashHex)
        {
            return HashToHex(input) == expectedHashHex.ToLowerInvariant();
        }
    }
}
