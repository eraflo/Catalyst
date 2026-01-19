/*
 * ============================================================================
 * CRYPTOGRAPHIC RANDOM NUMBER GENERATOR
 * ============================================================================
 * 
 * WHAT IS CSPRNG?
 * ---------------
 * A Cryptographically Secure Pseudo-Random Number Generator (CSPRNG) produces
 * random numbers that are statistically indistinguishable from true random
 * and unpredictable even to an attacker who knows the algorithm.
 * 
 * WHY NOT System.Random?
 * ----------------------
 * System.Random is:
 * - PREDICTABLE: If seed is known, all outputs can be predicted
 * - NOT THREAD-SAFE: Can produce duplicate values across threads
 * - WEAK SEED: Often seeded from DateTime, which has low entropy
 * 
 * RandomNumberGenerator (this) is:
 * - CRYPTOGRAPHICALLY SECURE: Uses OS entropy sources
 * - UNPREDICTABLE: No practical way to predict next value
 * - THREAD-SAFE: Safe to use from any thread
 * 
 * ENTROPY SOURCES (Windows):
 * --------------------------
 * - CPU timing jitter
 * - Mouse/keyboard timing
 * - Network packet timing
 * - Disk access timing
 * - TPM (if available)
 * 
 * USE CASES IN GAMES:
 * -------------------
 * - Generating encryption keys
 * - Creating session tokens
 * - Password salt generation
 * - Nonces for encryption
 * - Room codes / join codes
 * 
 * PERFORMANCE NOTE:
 * -----------------
 * CSPRNG is slower than System.Random (~10x). Use System.Random for
 * non-security purposes (shuffling, game randomness). Use CSPRNG only
 * when cryptographic security is required.
 * 
 * ============================================================================
 */

using System.Security.Cryptography;

namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Cryptographically secure random number generator.
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    ///   <item>Generating encryption keys</item>
    ///   <item>Creating session tokens</item>
    ///   <item>Producing unpredictable room codes</item>
    ///   <item>Any security-sensitive randomness</item>
    /// </list>
    /// 
    /// <para><b>When NOT to use:</b></para>
    /// <list type="bullet">
    ///   <item>Game randomness (loot drops, shuffling) - use System.Random</item>
    ///   <item>Performance-critical random (slower than System.Random)</item>
    /// </list>
    /// </summary>
    public class CryptoRandomProvider : IRandomProvider
    {
        /// <summary>
        /// Fills a buffer with cryptographically secure random bytes.
        /// </summary>
        /// <param name="buffer">Buffer to fill.</param>
        public void Fill(byte[] buffer)
        {
            RandomNumberGenerator.Fill(buffer);
        }

        /// <summary>
        /// Generates cryptographically secure random bytes.
        /// </summary>
        /// <param name="count">Number of bytes to generate.</param>
        /// <returns>Array of random bytes.</returns>
        public byte[] GetBytes(int count)
        {
            var buffer = new byte[count];
            Fill(buffer);
            return buffer;
        }

        /// <summary>
        /// Generates a cryptographically secure random integer.
        /// </summary>
        /// <param name="minInclusive">Minimum value (inclusive).</param>
        /// <param name="maxExclusive">Maximum value (exclusive).</param>
        /// <returns>Random integer in range [min, max).</returns>
        public int GetInt(int minInclusive, int maxExclusive)
        {
            return RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
        }
    }
}
