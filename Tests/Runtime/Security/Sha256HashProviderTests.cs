using NUnit.Framework;
using Eraflo.Catalyst.Security;

namespace Eraflo.Catalyst.Tests.Security
{
    public class Sha256HashProviderTests
    {
        private IHashProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _provider = new Sha256HashProvider();
        }

        [Test]
        public void Name_Returns_SHA256()
        {
            Assert.AreEqual("SHA256", _provider.Name);
        }

        [Test]
        public void Hash_SameInput_ProducesSameHash()
        {
            var hash1 = _provider.Hash("test");
            var hash2 = _provider.Hash("test");
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void Hash_DifferentInputs_ProduceDifferentHashes()
        {
            var hash1 = _provider.HashToHex("password1");
            var hash2 = _provider.HashToHex("password2");
            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void HashToHex_Returns64Characters()
        {
            var hash = _provider.HashToHex("test");
            Assert.AreEqual(64, hash.Length); // SHA256 = 256 bits = 64 hex chars
        }

        [Test]
        public void HashToHex_IsLowercase()
        {
            var hash = _provider.HashToHex("TEST");
            Assert.AreEqual(hash.ToLowerInvariant(), hash);
        }

        [Test]
        public void Verify_CorrectHash_ReturnsTrue()
        {
            string input = "secret";
            var hashHex = _provider.HashToHex(input);
            Assert.IsTrue(_provider.Verify(input, hashHex));
        }

        [Test]
        public void Verify_WrongHash_ReturnsFalse()
        {
            var hashHex = _provider.HashToHex("correct");
            Assert.IsFalse(_provider.Verify("wrong", hashHex));
        }

        [Test]
        public void VerifyHex_CorrectHash_ReturnsTrue()
        {
            string input = "password";
            var hashHex = _provider.HashToHex(input);
            Assert.IsTrue(_provider.Verify(input, hashHex));
        }

        [Test]
        public void VerifyHex_CaseInsensitive()
        {
            string input = "test";
            var hashHex = _provider.HashToHex(input).ToUpperInvariant();
            Assert.IsTrue(_provider.Verify(input, hashHex));
        }
    }
}
