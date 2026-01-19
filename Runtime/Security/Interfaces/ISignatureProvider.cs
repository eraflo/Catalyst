namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Interface for message signature providers.
    /// 
    /// <para><b>What are signatures?</b></para>
    /// Signatures prove data authenticity and integrity. Unlike hashing,
    /// signatures require a secret key - only the key holder can create
    /// a valid signature.
    /// 
    /// <para><b>HMAC vs Digital Signatures:</b></para>
    /// <list type="bullet">
    ///   <item><b>HMAC</b>: Symmetric (same key to sign and verify)</item>
    ///   <item><b>Digital</b>: Asymmetric (private key signs, public key verifies)</item>
    /// </list>
    /// This interface covers both types.
    /// 
    /// <para><b>Use cases:</b></para>
    /// <list type="bullet">
    ///   <item>API request authentication</item>
    ///   <item>Save file anti-tampering</item>
    ///   <item>Network message verification</item>
    /// </list>
    /// 
    /// <para><b>Implementations:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="HmacSignatureProvider"/> - HMAC-SHA256 (default)</item>
    /// </list>
    /// </summary>
    public interface ISignatureProvider
    {
        /// <summary>Algorithm name (e.g., "HMAC-SHA256").</summary>
        string Name { get; }
        
        /// <summary>Signs data with the secret key.</summary>
        byte[] Sign(byte[] data, byte[] key);
        
        /// <summary>
        /// Verifies a signature.
        /// Uses constant-time comparison to prevent timing attacks.
        /// </summary>
        bool Verify(byte[] data, byte[] signature, byte[] key);
    }
}
