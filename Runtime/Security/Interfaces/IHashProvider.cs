namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Interface for cryptographic hash providers.
    /// 
    /// <para><b>What is hashing?</b></para>
    /// Hashing is a one-way function that converts data of any size into a 
    /// fixed-size digest. It's used for data integrity, password verification,
    /// and creating unique identifiers.
    /// 
    /// <para><b>Key properties:</b></para>
    /// <list type="bullet">
    ///   <item><b>Deterministic</b>: Same input = same output</item>
    ///   <item><b>One-way</b>: Cannot reverse hash to get input</item>
    ///   <item><b>Collision-resistant</b>: Hard to find two inputs with same hash</item>
    /// </list>
    /// 
    /// <para><b>Implementations:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="Sha256HashProvider"/> - SHA-256 (default)</item>
    ///   <item>Argon2HashProvider - Password hashing (custom)</item>
    /// </list>
    /// </summary>
    public interface IHashProvider
    {
        /// <summary>Algorithm name (e.g., "SHA256", "Argon2").</summary>
        string Name { get; }
        
        /// <summary>Computes hash of raw bytes.</summary>
        byte[] Hash(byte[] data);
        
        /// <summary>Computes hash of string (UTF-8 encoded).</summary>
        byte[] Hash(string input);
        
        /// <summary>Computes hash and returns as lowercase hex string.</summary>
        string HashToHex(string input);
        
        /// <summary>Verifies data matches expected hash.</summary>
        bool Verify(byte[] data, byte[] expectedHash);
        
        /// <summary>Verifies string matches expected hex hash.</summary>
        bool Verify(string input, string expectedHashHex);
    }
}
