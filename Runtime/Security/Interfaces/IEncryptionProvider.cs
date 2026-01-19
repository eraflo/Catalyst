namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Interface for encryption providers.
    /// 
    /// <para><b>What is encryption?</b></para>
    /// Encryption transforms plaintext into ciphertext using a secret key.
    /// Unlike hashing, encryption is REVERSIBLE - you can decrypt the ciphertext
    /// back to plaintext if you have the key.
    /// 
    /// <para><b>Symmetric vs Asymmetric:</b></para>
    /// <list type="bullet">
    ///   <item><b>Symmetric</b>: Same key for encrypt/decrypt (AES, ChaCha20)</item>
    ///   <item><b>Asymmetric</b>: Public/private key pair (RSA, ECDSA)</item>
    /// </list>
    /// This interface is for symmetric encryption.
    /// 
    /// <para><b>Authenticated Encryption:</b></para>
    /// Modern algorithms like AES-GCM provide both confidentiality AND integrity.
    /// If data is tampered with, decryption will fail.
    /// 
    /// <para><b>Implementations:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="AesGcmEncryptionProvider"/> - AES-256-GCM (default)</item>
    ///   <item>ChaCha20Poly1305Provider - Alternative (custom)</item>
    /// </list>
    /// </summary>
    public interface IEncryptionProvider
    {
        /// <summary>Algorithm name (e.g., "AES-GCM").</summary>
        string Name { get; }
        
        /// <summary>Required key size in bytes (e.g., 32 for 256-bit).</summary>
        int KeySize { get; }
        
        /// <summary>Encrypts plaintext. Output includes nonce and auth tag.</summary>
        byte[] Encrypt(byte[] plaintext, byte[] key);
        
        /// <summary>Decrypts ciphertext. Throws if data is tampered.</summary>
        byte[] Decrypt(byte[] ciphertext, byte[] key);
        
        /// <summary>Generates a cryptographically secure key.</summary>
        byte[] GenerateKey();
    }
}
