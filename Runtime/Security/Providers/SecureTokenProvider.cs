/*
 * ============================================================================
 * SECURE TOKEN GENERATION
 * ============================================================================
 * 
 * WHAT ARE SECURE TOKENS?
 * -----------------------
 * Secure tokens are random strings used for authentication, identification,
 * or verification purposes. They must be:
 * - Unpredictable (cannot guess the next token)
 * - Unique (extremely low collision probability)
 * - Unbiased (each character has equal probability)
 * 
 * TOKEN FORMATS:
 * --------------
 * 
 * BASE64 (default):
 * - Alphabet: A-Z, a-z, 0-9, +, /, =
 * - 64 characters = 6 bits per character
 * - Most compact for bytes-to-string conversion
 * - Example: "dGhpcyBpcyBhIHRlc3Q="
 * 
 * ALPHANUMERIC:
 * - Alphabet: A-Z, a-z, 0-9
 * - 62 characters = ~5.95 bits per character
 * - Safe for URLs and filenames
 * - Example: "A3b7X9kLm2"
 * 
 * NUMERIC:
 * - Alphabet: 0-9
 * - 10 characters = ~3.32 bits per character
 * - Human-friendly PINs/codes
 * - Example: "847291"
 * 
 * ENTROPY CALCULATION:
 * --------------------
 * Entropy (bits) = log2(alphabet_size) * length
 * 
 * Examples:
 * - 6-char alphanumeric: log2(62) * 6 ≈ 36 bits = 68 billion combinations
 * - 6-char numeric: log2(10) * 6 ≈ 20 bits = 1 million combinations
 * - 32-char alphanumeric: log2(62) * 32 ≈ 190 bits (extremely strong)
 * 
 * USE CASES IN GAMES:
 * -------------------
 * - Room/lobby codes: GenerateAlphanumeric(6).ToUpper() → "A3B7X9"
 * - Session tokens: GenerateAlphanumeric(32)
 * - Verification PINs: GenerateNumeric(6) → "847291"
 * - API keys: Generate(32) for Base64 token
 * 
 * ============================================================================
 */

using System;

namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Secure token generator using cryptographic randomness.
    /// 
    /// <para><b>Token types:</b></para>
    /// <list type="bullet">
    ///   <item><b>Base64</b>: Compact, binary-safe (URLs may need encoding)</item>
    ///   <item><b>Alphanumeric</b>: URL-safe, human-readable</item>
    ///   <item><b>Numeric</b>: PINs, verification codes</item>
    /// </list>
    /// 
    /// <para><b>Recommended lengths:</b></para>
    /// <list type="bullet">
    ///   <item>Room codes: 6 alphanumeric (68B combinations)</item>
    ///   <item>Session tokens: 32 alphanumeric (190 bits entropy)</item>
    ///   <item>Verification PINs: 6 numeric (1M combinations)</item>
    /// </list>
    /// </summary>
    public class SecureTokenProvider : ITokenProvider
    {
        /// <inheritdoc/>
        public string Name => "SecureRandom";
        
        private readonly IRandomProvider _random;

        /// <summary>
        /// Creates a token provider using the specified random source.
        /// </summary>
        /// <param name="random">Cryptographic random provider.</param>
        public SecureTokenProvider(IRandomProvider random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Generates a Base64-encoded token.
        /// </summary>
        /// <param name="byteLength">Number of random bytes (output will be ~4/3 longer).</param>
        /// <returns>Base64 string.</returns>
        public string Generate(int byteLength)
        {
            return Convert.ToBase64String(_random.GetBytes(byteLength));
        }

        /// <summary>
        /// Generates an alphanumeric token (A-Z, a-z, 0-9).
        /// URL-safe and human-readable.
        /// </summary>
        /// <param name="length">Number of characters.</param>
        /// <returns>Alphanumeric string.</returns>
        public string GenerateAlphanumeric(int length)
        {
            return GenerateFromCharset(
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 
                length);
        }

        /// <summary>
        /// Generates a numeric-only token (0-9).
        /// Suitable for PINs and verification codes.
        /// </summary>
        /// <param name="length">Number of digits.</param>
        /// <returns>Numeric string.</returns>
        public string GenerateNumeric(int length)
        {
            return GenerateFromCharset("0123456789", length);
        }

        private string GenerateFromCharset(string charset, int length)
        {
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = charset[_random.GetInt(0, charset.Length)];
            }
            return new string(result);
        }
    }
}
