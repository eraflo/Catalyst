/*
 * ============================================================================
 * HMAC-SHA256 SIGNATURE ALGORITHM
 * ============================================================================
 * 
 * WHAT IS HMAC?
 * -------------
 * HMAC (Hash-based Message Authentication Code) is a mechanism for verifying
 * both data integrity AND authenticity using a cryptographic hash function
 * combined with a secret key.
 * 
 * HOW IT WORKS:
 * -------------
 * 1. The key is XORed with inner/outer padding constants
 * 2. Inner hash: Hash(key XOR ipad || message)
 * 3. Outer hash: Hash(key XOR opad || inner_hash)
 * 4. Result is a fixed-size authentication tag
 * 
 * FORMULA:
 * --------
 * HMAC(key, message) = Hash((key XOR opad) || Hash((key XOR ipad) || message))
 * 
 * KEY PROPERTIES:
 * ---------------
 * - AUTHENTICATED: Only someone with the key can create a valid signature
 * - INTEGRITY: Any modification to the message invalidates the signature
 * - NON-REPUDIATION: Proves the message came from a key holder
 * - TIMING-SAFE: Must use constant-time comparison to prevent timing attacks
 * 
 * USE CASES IN GAMES:
 * -------------------
 * - API authentication (sign requests with secret key)
 * - Anti-tampering (sign save files, detect cheating)
 * - Message verification (ensure network messages are authentic)
 * - Discovery signatures (prove server is legitimate)
 * 
 * HMAC vs HASH:
 * -------------
 * - Hash: Anyone can compute, just checks integrity
 * - HMAC: Requires secret key, checks integrity + authenticity
 * 
 * SECURITY NOTES:
 * ---------------
 * - Key should be at least 256 bits (32 bytes) for HMAC-SHA256
 * - ALWAYS use constant-time comparison (CryptographicOperations.FixedTimeEquals)
 * - Never expose the key in logs or error messages
 * 
 * ============================================================================
 */

using System.Security.Cryptography;

namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// HMAC-SHA256 message authentication code provider.
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    ///   <item>Verifying message authenticity (only key holder can sign)</item>
    ///   <item>Protecting data from tampering</item>
    ///   <item>API request signing</item>
    ///   <item>Anti-cheat file signatures</item>
    /// </list>
    /// 
    /// <para><b>Important:</b></para>
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    public class HmacSignatureProvider : ISignatureProvider
    {
        /// <inheritdoc/>
        public string Name => "HMAC-SHA256";

        /// <summary>
        /// Signs data with HMAC-SHA256.
        /// </summary>
        /// <param name="data">Data to sign.</param>
        /// <param name="key">Secret signing key (recommend 32+ bytes).</param>
        /// <returns>32-byte signature.</returns>
        public byte[] Sign(byte[] data, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(data);
        }

        /// <summary>
        /// Verifies an HMAC-SHA256 signature.
        /// Uses constant-time comparison to prevent timing attacks.
        /// </summary>
        /// <param name="data">Original data.</param>
        /// <param name="signature">Signature to verify (32 bytes).</param>
        /// <param name="key">Secret signing key.</param>
        /// <returns>True if signature is valid.</returns>
        public bool Verify(byte[] data, byte[] signature, byte[] key)
        {
            var expected = Sign(data, key);
            // CRITICAL: Use constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(expected, signature);
        }
    }
}
