using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Core.Save;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends;
using Eraflo.Catalyst.Networking.Backends.Mock;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Tests
{
    public class SaveSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            var nm = App.Get<NetworkManager>();
            nm.Reset();
            nm.SetBackend(new MockNetworkBackend(isServer: true, isClient: true, isConnected: true));
        }

        [Test]
        public void JsonSerializer_SerializesUnityTypes()
        {
            var serializer = new JsonSerializer();
            var data = new TestData
            {
                Pos = new Vector3(1.1f, 2.2f, 3.3f),
                Rot = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f),
                Col = new Color(1, 0, 0, 1)
            };

            byte[] bytes = serializer.Serialize(data);
            var restored = serializer.Deserialize<TestData>(bytes);

            Assert.AreEqual(data.Pos.x, restored.Pos.x, 0.001f);
            Assert.AreEqual(data.Pos.y, restored.Pos.y, 0.001f);
            Assert.AreEqual(data.Pos.z, restored.Pos.z, 0.001f);
            Assert.AreEqual(data.Rot.x, restored.Rot.x, 0.001f);
            Assert.AreEqual(data.Col.r, restored.Col.r, 0.001f);
        }

        [Test]
        public async Task SaveManager_Aborts_WhenNotServer()
        {
            // Override setup to be a client
            App.Get<NetworkManager>().SetBackend(new MockNetworkBackend(isServer: false));

            var sm = new SaveManager();
            ((IGameService)sm).Initialize();

            // Test
            bool success = await sm.SaveGame("TestSave");

            Assert.IsFalse(success, "Save should fail when not a server");
        }

        [Test]
        public async Task SaveManager_UsesRegistry_ForSaving()
        {
            var sm = new SaveManager();
            ((IGameService)sm).Initialize();
            sm.Storage = new MockStorage();

            var go = new GameObject("TestEntity");
            var entity = go.AddComponent<SaveableEntity>();
            sm.Register(entity); // Manual registration for test

            bool success = await sm.SaveGame("TestRegistry");
            Assert.IsTrue(success);
            
            var storage = (MockStorage)sm.Storage;
            Assert.IsTrue(storage.LastSavedData.Length > 0);
            Assert.IsTrue(System.Text.Encoding.UTF8.GetString(storage.LastSavedData).Contains(entity.Guid));
        }

        [Test]
        public async Task SaveManager_OptimizedMetadata_CorrectlyReadsHeader()
        {
            var sm = new SaveManager();
            ((IGameService)sm).Initialize();
            var storage = new MockStorage();
            sm.Storage = storage;

            var data = new GameData("HeavySave");
            data.Metadata.Version = "2.0";
            byte[] bytes = sm.Serializer.Serialize(data);
            await storage.SaveAsync("HeavySave", bytes);

            var metadata = await sm.GetSaveMetadata("HeavySave");
            Assert.IsNotNull(metadata);
            Assert.AreEqual("HeavySave", metadata.Name);
            Assert.AreEqual("2.0", metadata.Version);
        }

        private class MockStorage : IStorageBackend
        {
            public byte[] LastSavedData;
            private Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

            public Task SaveAsync(string name, byte[] data)
            {
                LastSavedData = data;
                _files[name] = data;
                return Task.CompletedTask;
            }

            public Task<byte[]> LoadAsync(string name) => Task.FromResult(_files.GetValueOrDefault(name));
            public Task DeleteAsync(string name) { _files.Remove(name); return Task.CompletedTask; }
            public bool Exists(string name) => _files.ContainsKey(name);
        }

        [System.Serializable]
        private class TestData
        {
            public Vector3 Pos;
            public Quaternion Rot;
            public Color Col;
        }
    }
}
