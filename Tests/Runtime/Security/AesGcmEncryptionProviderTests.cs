using System;
using NUnit.Framework;
using Eraflo.Catalyst.Security;

namespace Eraflo.Catalyst.Tests.Security
{
    public class AesGcmEncryptionProviderTests
    {
        private IEncryptionProvider _provider;
        private byte[] _key;
        private bool _isSupported;

        [SetUp]
        public void SetUp()
        {
            // AES-GCM requires .NET Core 3.0+ and is not supported on Mono/IL2CPP
            // The exception is thrown during Encrypt(), not during construction
            try
            {
                _provider = new AesGcmEncryptionProvider();
                _key = _provider.GenerateKey();
                // Test actual encryption to trigger platform check
                _provider.Encrypt(new byte[] { 0x00 }, _key);
                _isSupported = true;
            }
            catch (PlatformNotSupportedException)
            {
                _isSupported = false;
            }
        }

        private void SkipIfNotSupported()
        {
            if (!_isSupported)
                Assert.Ignore("AES-GCM is not supported on this platform.");
        }

        [Test]
        public void Name_Returns_AESGCM()
        {
            SkipIfNotSupported();
            Assert.AreEqual("AES-GCM", _provider.Name);
        }

        [Test]
        public void KeySize_Is32Bytes()
        {
            SkipIfNotSupported();
            Assert.AreEqual(32, _provider.KeySize);
        }

        [Test]
        public void GenerateKey_Returns32Bytes()
        {
            SkipIfNotSupported();
            var key = _provider.GenerateKey();
            Assert.AreEqual(32, key.Length);
        }

        [Test]
        public void GenerateKey_ProducesUniqueKeys()
        {
            SkipIfNotSupported();
            var key1 = _provider.GenerateKey();
            var key2 = _provider.GenerateKey();
            Assert.AreNotEqual(key1, key2);
        }

        [Test]
        public void EncryptDecrypt_RoundTrip_PreservesData()
        {
            SkipIfNotSupported();
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
            
            byte[] encrypted = _provider.Encrypt(plaintext, _key);
            byte[] decrypted = _provider.Decrypt(encrypted, _key);
            
            Assert.AreEqual(plaintext, decrypted);
        }

        [Test]
        public void Encrypt_ProducesDifferentOutputs_ForSameInput()
        {
            SkipIfNotSupported();
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("test");
            
            byte[] encrypted1 = _provider.Encrypt(plaintext, _key);
            byte[] encrypted2 = _provider.Encrypt(plaintext, _key);
            
            // Due to random nonce, outputs should differ
            Assert.AreNotEqual(encrypted1, encrypted2);
        }

        [Test]
        public void Decrypt_WrongKey_Throws()
        {
            SkipIfNotSupported();
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("secret");
            byte[] encrypted = _provider.Encrypt(plaintext, _key);
            byte[] wrongKey = _provider.GenerateKey();
            
            Assert.Throws<System.Security.Cryptography.CryptographicException>(
                () => _provider.Decrypt(encrypted, wrongKey));
        }

        [Test]
        public void Decrypt_TamperedData_Throws()
        {
            SkipIfNotSupported();
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("important");
            byte[] encrypted = _provider.Encrypt(plaintext, _key);
            
            // Tamper with ciphertext
            encrypted[20] ^= 0xFF;
            
            Assert.Throws<System.Security.Cryptography.CryptographicException>(
                () => _provider.Decrypt(encrypted, _key));
        }
    }
}
