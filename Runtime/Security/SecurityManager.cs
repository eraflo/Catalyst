/*
 * ============================================================================
 * SECURITY MANAGER
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * Central service for all cryptographic and security operations.
 * Provides access to pluggable providers for hashing, encryption,
 * signatures, tokens, and secure random generation.
 * 
 * ARCHITECTURE:
 * -------------
 * Uses the Provider Pattern for maximum flexibility:
 * - IHashProvider      → Sha256HashProvider (default)
 * - IEncryptionProvider → AesGcmEncryptionProvider (default)
 * - ISignatureProvider  → HmacSignatureProvider (default)
 * - ITokenProvider      → SecureTokenProvider (default)
 * - IRandomProvider     → CryptoRandomProvider (default)
 * 
 * SERVICE PRIORITY:
 * -----------------
 * Priority -15 ensures initialization before other services that may
 * need cryptographic operations (e.g., NetworkManager at Priority 0).
 * 
 * CUSTOMIZATION:
 * --------------
 * All providers can be swapped at runtime:
 *   security.SetHashProvider(new Argon2HashProvider());
 * 
 * ============================================================================
 */

using System;
using UnityEngine;
using Eraflo.Catalyst;

namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// Central service for security operations.
    /// Provides access to crypto providers and manages session keys.
    /// </summary>
    [Service(Priority = -15)]
    public class SecurityManager : IGameService
    {
        private IHashProvider _hashProvider;
        private IEncryptionProvider _encryptionProvider;
        private ISignatureProvider _signatureProvider;
        private ITokenProvider _tokenProvider;
        private IRandomProvider _randomProvider;
        
        private byte[] _sessionKey;

        /// <summary>Hash provider (default: SHA256).</summary>
        public IHashProvider Hash => _hashProvider;
        
        /// <summary>Encryption provider (default: AES-GCM).</summary>
        public IEncryptionProvider Encryption => _encryptionProvider;
        
        /// <summary>Signature provider (default: HMAC-SHA256).</summary>
        public ISignatureProvider Signature => _signatureProvider;
        
        /// <summary>Token provider (default: SecureRandom).</summary>
        public ITokenProvider Token => _tokenProvider;
        
        /// <summary>Random provider (default: CryptoRandom).</summary>
        public IRandomProvider Random => _randomProvider;

        public void Initialize()
        {
            // Set default providers
            _randomProvider = new CryptoRandomProvider();
            _hashProvider = new Sha256HashProvider();
            _encryptionProvider = new AesGcmEncryptionProvider();
            _signatureProvider = new HmacSignatureProvider();
            _tokenProvider = new SecureTokenProvider(_randomProvider);
            
            // Generate session key
            _sessionKey = _encryptionProvider.GenerateKey();
            
            Debug.Log("[SecurityManager] Initialized with default providers.");
        }

        public void Shutdown()
        {
            // Securely clear session key
            if (_sessionKey != null)
            {
                Array.Clear(_sessionKey, 0, _sessionKey.Length);
                _sessionKey = null;
            }
        }

        #region Provider Setters
        
        public void SetHashProvider(IHashProvider provider)
        {
            _hashProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            Debug.Log($"[SecurityManager] Hash provider set to: {provider.Name}");
        }
        
        public void SetEncryptionProvider(IEncryptionProvider provider)
        {
            _encryptionProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            _sessionKey = _encryptionProvider.GenerateKey();
            Debug.Log($"[SecurityManager] Encryption provider set to: {provider.Name}");
        }
        
        public void SetSignatureProvider(ISignatureProvider provider)
        {
            _signatureProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            Debug.Log($"[SecurityManager] Signature provider set to: {provider.Name}");
        }
        
        public void SetTokenProvider(ITokenProvider provider)
        {
            _tokenProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            Debug.Log($"[SecurityManager] Token provider set to: {provider.Name}");
        }
        
        public void SetRandomProvider(IRandomProvider provider)
        {
            _randomProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            Debug.Log($"[SecurityManager] Random provider set to: CryptoRandom");
        }
        
        #endregion

        #region Session Key Convenience Methods
        
        /// <summary>Encrypts data with session key.</summary>
        public byte[] EncryptWithSession(byte[] data) => _encryptionProvider.Encrypt(data, _sessionKey);
        
        /// <summary>Decrypts data with session key.</summary>
        public byte[] DecryptWithSession(byte[] data) => _encryptionProvider.Decrypt(data, _sessionKey);
        
        /// <summary>Generates an alphanumeric room code (e.g., "A3B7X9").</summary>
        public string GenerateRoomCode(int length = 6) => _tokenProvider.GenerateAlphanumeric(length).ToUpperInvariant();
        
        #endregion
    }
}
