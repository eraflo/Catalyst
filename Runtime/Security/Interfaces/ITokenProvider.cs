namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Interface for secure token generators.
    /// 
    /// <para><b>What are tokens?</b></para>
    /// Tokens are random strings used for identification, authentication,
    /// or verification. Security-critical tokens must be cryptographically
    /// random to prevent guessing attacks.
    /// 
    /// <para><b>Token formats:</b></para>
    /// <list type="bullet">
    ///   <item><b>Base64</b>: Compact, binary-safe (may need URL encoding)</item>
    ///   <item><b>Alphanumeric</b>: URL-safe, human-readable (A-Z, a-z, 0-9)</item>
    ///   <item><b>Numeric</b>: PINs and verification codes (0-9)</item>
    /// </list>
    /// 
    /// <para><b>Common use cases:</b></para>
    /// <list type="bullet">
    ///   <item>Room codes: 6-char alphanumeric → "A3B7X9"</item>
    ///   <item>Session tokens: 32-char alphanumeric</item>
    ///   <item>Verification PINs: 6-digit numeric → "847291"</item>
    /// </list>
    /// 
    /// <para><b>Implementations:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="SecureTokenProvider"/> - CSPRNG-based (default)</item>
    /// </list>
    /// </summary>
    public interface ITokenProvider
    {
        /// <summary>Provider name.</summary>
        string Name { get; }
        
        /// <summary>Generates a Base64-encoded token.</summary>
        string Generate(int byteLength);
        
        /// <summary>Generates alphanumeric token (A-Z, a-z, 0-9).</summary>
        string GenerateAlphanumeric(int length);
        
        /// <summary>Generates numeric-only token (0-9).</summary>
        string GenerateNumeric(int length);
    }
}
