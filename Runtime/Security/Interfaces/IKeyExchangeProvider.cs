namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Interface for key exchange providers (Diffie-Hellman style).
    /// 
    /// <para><b>What is Key Exchange?</b></para>
    /// Key exchange allows two parties to establish a shared secret over
    /// an insecure channel without ever transmitting the secret itself.
    /// 
    /// <para><b>ECDH (Elliptic Curve Diffie-Hellman):</b></para>
    /// <list type="bullet">
    ///   <item>Each party generates a key pair (public + private)</item>
    ///   <item>They exchange public keys</item>
    ///   <item>Each party derives the same shared secret using their private key + other's public key</item>
    /// </list>
    /// 
    /// <para><b>Use cases:</b></para>
    /// <list type="bullet">
    ///   <item>Establishing encryption keys for network sessions</item>
    ///   <item>Perfect forward secrecy (PFS) in secure connections</item>
    ///   <item>End-to-end encryption setup</item>
    /// </list>
    /// 
    /// <para><b>Implementations:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="EcdhKeyExchangeProvider"/> - ECDH with P-256 curve (default)</item>
    /// </list>
    /// </summary>
    public interface IKeyExchangeProvider
    {
        /// <summary>Algorithm name (e.g., "ECDH-P256").</summary>
        string Name { get; }
        
        /// <summary>
        /// Generates a new ephemeral key pair.
        /// </summary>
        /// <returns>Key pair containing public and private keys.</returns>
        KeyPair GenerateKeyPair();
        
        /// <summary>
        /// Derives a shared secret from our private key and their public key.
        /// </summary>
        /// <param name="ourPrivateKey">Our private key.</param>
        /// <param name="theirPublicKey">Their public key.</param>
        /// <returns>Shared secret (can be used as encryption key).</returns>
        byte[] DeriveSharedSecret(byte[] ourPrivateKey, byte[] theirPublicKey);
    }

    /// <summary>
    /// Represents a cryptographic key pair.
    /// </summary>
    public struct KeyPair
    {
        /// <summary>Public key (can be shared).</summary>
        public byte[] PublicKey;
        
        /// <summary>Private key (keep secret!).</summary>
        public byte[] PrivateKey;
    }
}
