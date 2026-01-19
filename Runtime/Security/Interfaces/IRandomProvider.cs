namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Interface for cryptographically secure random providers.
    /// 
    /// <para><b>Why cryptographic randomness?</b></para>
    /// Standard random generators (System.Random) are PREDICTABLE if the
    /// seed is known. For security purposes, you need randomness that is
    /// computationally indistinguishable from true random.
    /// 
    /// <para><b>System.Random vs CSPRNG:</b></para>
    /// <list type="bullet">
    ///   <item><b>System.Random</b>: Fast, predictable, NOT for security</item>
    ///   <item><b>CSPRNG</b>: Slower, unpredictable, for security</item>
    /// </list>
    /// 
    /// <para><b>When to use CSPRNG:</b></para>
    /// <list type="bullet">
    ///   <item>Encryption keys and nonces</item>
    ///   <item>Session tokens and room codes</item>
    ///   <item>Password salts</item>
    ///   <item>Any security-sensitive random</item>
    /// </list>
    /// 
    /// <para><b>When to use System.Random:</b></para>
    /// <list type="bullet">
    ///   <item>Game mechanics (loot drops, shuffling)</item>
    ///   <item>Visual effects (particles, colors)</item>
    ///   <item>Non-security randomness</item>
    /// </list>
    /// 
    /// <para><b>Implementations:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="CryptoRandomProvider"/> - OS CSPRNG (default)</item>
    /// </list>
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Fills buffer with random bytes.</summary>
        void Fill(byte[] buffer);
        
        /// <summary>Returns random bytes.</summary>
        byte[] GetBytes(int count);
        
        /// <summary>Returns random int in [minInclusive, maxExclusive).</summary>
        int GetInt(int minInclusive, int maxExclusive);
    }
}
