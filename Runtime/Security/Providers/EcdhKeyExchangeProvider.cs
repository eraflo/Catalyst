/*
 * ============================================================================
 * ECDH KEY EXCHANGE PROVIDER
 * ============================================================================
 * 
 * WHAT IS ECDH?
 * -------------
 * Elliptic Curve Diffie-Hellman (ECDH) is a key agreement protocol that
 * allows two parties to establish a shared secret over an insecure channel.
 * 
 * HOW IT WORKS:
 * -------------
 * 1. Alice generates a key pair (private + public)
 * 2. Bob generates a key pair (private + public)
 * 3. They exchange PUBLIC keys (safe to intercept)
 * 4. Alice: SharedSecret = ECDH(Alice.Private, Bob.Public)
 * 5. Bob:   SharedSecret = ECDH(Bob.Private, Alice.Public)
 * 6. Both arrive at the SAME shared secret!
 * 
 * SECURITY PROPERTIES:
 * --------------------
 * - Passive attackers can't derive the shared secret
 * - Perfect Forward Secrecy (PFS) when using ephemeral keys
 * - P-256 curve provides ~128 bits of security
 * 
 * USE IN NETWORKING:
 * ------------------
 * 1. Client connects and sends its ephemeral public key
 * 2. Server responds with its ephemeral public key
 * 3. Both derive shared secret for session encryption
 * 4. All further traffic is encrypted with shared secret
 * 
 * ============================================================================
 */

using System;
using System.Security.Cryptography;

namespace Eraflo.Catalyst.Security
{
    /// <summary>
    /// ECDH key exchange provider using the P-256 (secp256r1) curve.
    /// 
    /// <para><b>Key sizes:</b></para>
    /// <list type="bullet">
    ///   <item>Public key: 65 bytes (uncompressed point)</item>
    ///   <item>Private key: 32 bytes</item>
    ///   <item>Shared secret: 32 bytes</item>
    /// </list>
    /// </summary>
    public class EcdhKeyExchangeProvider : IKeyExchangeProvider
    {
        /// <inheritdoc/>
        public string Name => "ECDH-P256";

        /// <summary>
        /// Generates an ephemeral ECDH key pair.
        /// </summary>
        public KeyPair GenerateKeyPair()
        {
            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            
            var parameters = ecdh.ExportParameters(includePrivateParameters: true);
            
            return new KeyPair
            {
                PublicKey = ExportPublicKey(parameters),
                PrivateKey = parameters.D
            };
        }

        /// <summary>
        /// Derives a shared secret from our private key and their public key.
        /// </summary>
        public byte[] DeriveSharedSecret(byte[] ourPrivateKey, byte[] theirPublicKey)
        {
            if (ourPrivateKey == null || ourPrivateKey.Length != 32)
                throw new ArgumentException("Private key must be 32 bytes.", nameof(ourPrivateKey));
            
            if (theirPublicKey == null || theirPublicKey.Length != 65)
                throw new ArgumentException("Public key must be 65 bytes (uncompressed).", nameof(theirPublicKey));

            // Import their public key
            var theirParams = ImportPublicKey(theirPublicKey);
            
            // Create our key from private
            var ourParams = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = ourPrivateKey,
                Q = new ECPoint { X = new byte[32], Y = new byte[32] } // Dummy, will be derived
            };
            
            // Compute Q from D (the public point)
            using var tempEcdh = ECDiffieHellman.Create();
            tempEcdh.ImportParameters(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = ourPrivateKey
            });
            ourParams = tempEcdh.ExportParameters(includePrivateParameters: true);

            using var ourEcdh = ECDiffieHellman.Create();
            ourEcdh.ImportParameters(ourParams);

            using var theirEcdh = ECDiffieHellman.Create();
            theirEcdh.ImportParameters(theirParams);

            // Derive shared secret
            return ourEcdh.DeriveKeyMaterial(theirEcdh.PublicKey);
        }

        /// <summary>
        /// Exports public key as uncompressed point (65 bytes: 0x04 + X + Y).
        /// </summary>
        private static byte[] ExportPublicKey(ECParameters parameters)
        {
            var publicKey = new byte[65];
            publicKey[0] = 0x04; // Uncompressed point format
            Buffer.BlockCopy(parameters.Q.X, 0, publicKey, 1, 32);
            Buffer.BlockCopy(parameters.Q.Y, 0, publicKey, 33, 32);
            return publicKey;
        }

        /// <summary>
        /// Imports a public key from uncompressed point format.
        /// </summary>
        private static ECParameters ImportPublicKey(byte[] publicKey)
        {
            if (publicKey[0] != 0x04)
                throw new ArgumentException("Public key must be in uncompressed format (0x04 prefix).");

            var x = new byte[32];
            var y = new byte[32];
            Buffer.BlockCopy(publicKey, 1, x, 0, 32);
            Buffer.BlockCopy(publicKey, 33, y, 0, 32);

            return new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = x, Y = y }
            };
        }
    }
}
