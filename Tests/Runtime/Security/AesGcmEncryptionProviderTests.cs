using NUnit.Framework;
using Eraflo.Catalyst.Security;

namespace Eraflo.Catalyst.Tests.Security
{
    public class AesGcmEncryptionProviderTests
    {
        private IEncryptionProvider _provider;
        private byte[] _key;

        [SetUp]
        public void SetUp()
        {
            _provider = new AesGcmEncryptionProvider();
            _key = _provider.GenerateKey();
        }

        [Test]
        public void Name_Returns_AESGCM()
        {
            Assert.AreEqual("AES-GCM", _provider.Name);
        }

        [Test]
        public void KeySize_Is32Bytes()
        {
            Assert.AreEqual(32, _provider.KeySize);
        }

        [Test]
        public void GenerateKey_Returns32Bytes()
        {
            var key = _provider.GenerateKey();
            Assert.AreEqual(32, key.Length);
        }

        [Test]
        public void GenerateKey_ProducesUniqueKeys()
        {
            var key1 = _provider.GenerateKey();
            var key2 = _provider.GenerateKey();
            Assert.AreNotEqual(key1, key2);
        }

        [Test]
        public void EncryptDecrypt_RoundTrip_PreservesData()
        {
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
            
            byte[] encrypted = _provider.Encrypt(plaintext, _key);
            byte[] decrypted = _provider.Decrypt(encrypted, _key);
            
            Assert.AreEqual(plaintext, decrypted);
        }

        [Test]
        public void Encrypt_ProducesDifferentOutputs_ForSameInput()
        {
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("test");
            
            byte[] encrypted1 = _provider.Encrypt(plaintext, _key);
            byte[] encrypted2 = _provider.Encrypt(plaintext, _key);
            
            // Due to random nonce, outputs should differ
            Assert.AreNotEqual(encrypted1, encrypted2);
        }

        [Test]
        public void Decrypt_WrongKey_Throws()
        {
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("secret");
            byte[] encrypted = _provider.Encrypt(plaintext, _key);
            byte[] wrongKey = _provider.GenerateKey();
            
            Assert.Throws<System.Security.Cryptography.CryptographicException>(
                () => _provider.Decrypt(encrypted, wrongKey));
        }

        [Test]
        public void Decrypt_TamperedData_Throws()
        {
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("important");
            byte[] encrypted = _provider.Encrypt(plaintext, _key);
            
            // Tamper with ciphertext
            encrypted[20] ^= 0xFF;
            
            Assert.Throws<System.Security.Cryptography.CryptographicException>(
                () => _provider.Decrypt(encrypted, _key));
        }
    }
}
