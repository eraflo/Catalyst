using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Eraflo.Catalyst.Core.Settings;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Tests
{
    public class SettingsPersistenceTests
    {
        private SettingsManager _manager;
        private string _testFilePath;

        [SetUp]
        public void SetUp()
        {
            _manager = new SettingsManager();
            ((IGameService)_manager).Initialize();
            
            // LocalDiskStorage uses Application.persistentDataPath/SaveData
            _testFilePath = Path.Combine(Application.persistentDataPath, "SaveData", "settings.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [UnityTest]
        public IEnumerator Settings_SaveAndLoad_PersistsValues()
        {
            // 1. Modify settings
            _manager.Data.MasterVolume = 0.123f;
            _manager.Data.Fullscreen = false;
            _manager.SetSetting("TestKey", "TestValue");

            // 2. Save
            var saveTask = _manager.SaveAsync();
            yield return new WaitUntil(() => saveTask.IsCompleted);

            // 3. Create a new manager instance and initialize (should load)
            var newManager = new SettingsManager();
            ((IGameService)newManager).Initialize();

            // 4. Verify values
            Assert.AreEqual(0.123f, newManager.Data.MasterVolume, 0.001f);
            Assert.IsFalse(newManager.Data.Fullscreen);
            Assert.AreEqual("TestValue", newManager.GetSetting<string>("TestKey"));
        }

        [Test]
        public void SettingsUtils_LinearToDecibel_IsCorrect()
        {
            Assert.AreEqual(0f, SettingsUtils.LinearToDecibel(1f), 0.01f);
            Assert.AreEqual(-20f, SettingsUtils.LinearToDecibel(0.1f), 0.01f);
            Assert.AreEqual(-80f, SettingsUtils.LinearToDecibel(0f), 0.01f);
        }
    }
}
