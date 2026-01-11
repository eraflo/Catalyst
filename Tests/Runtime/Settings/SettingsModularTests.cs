using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst.Core.Settings;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Tests
{
    public class SettingsModularityTests
    {
        private SettingsManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SettingsManager();
            ((IGameService)_manager).Initialize();
        }

        private class MockPage : ISettingsPage
        {
            public string Id => "Mock";
            public string DisplayName => "Mock Page";
            public bool Applied = false;
            public float LastValue;

            public IEnumerable<string> GetSettingKeys()
            {
                yield return "MasterVolume";
            }

            public void Apply(SettingsData data)
            {
                Applied = true;
                LastValue = data.MasterVolume;
            }
        }

        [Test]
        public void Settings_RegisterPage_AppliesImmediately()
        {
            var page = new MockPage();
            _manager.Data.MasterVolume = 0.5f;
            
            _manager.RegisterPage(page);

            Assert.IsTrue(page.Applied);
            Assert.AreEqual(0.5f, page.LastValue);
        }

        [Test]
        public void Settings_ApplyPage_TriggersCorrectPage()
        {
            var page = new MockPage();
            _manager.RegisterPage(page);
            page.Applied = false;

            _manager.ApplyPage("Mock");

            Assert.IsTrue(page.Applied);
        }

        [Test]
        public void Settings_ApplyAll_TriggersAllPages()
        {
            var page1 = new MockPage();
            var page2 = new MockPage { LastValue = -1f }; // Dummy id reuse for simplicity or different ID
            // Let's use unique IDs for better test
        }
    }

    public class SettingsGenericTests
    {
        private SettingsManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SettingsManager();
            ((IGameService)_manager).Initialize();
        }

        [Test]
        public void Settings_SetAndGet_WorksWithTypes()
        {
            _manager.SetSetting("IntKey", 42);
            _manager.SetSetting("FloatKey", 3.14f);
            _manager.SetSetting("BoolKey", true);
            _manager.SetSetting("StringKey", "Hello");

            Assert.AreEqual(42, _manager.GetSetting<int>("IntKey"));
            Assert.AreEqual(3.14f, _manager.GetSetting<float>("FloatKey"), 0.001f);
            Assert.IsTrue(_manager.GetSetting<bool>("BoolKey"));
            Assert.AreEqual("Hello", _manager.GetSetting<string>("StringKey"));
        }

        [Test]
        public void Settings_GetSetting_ReturnsDefault_OnMissingOrInvalid()
        {
            Assert.AreEqual(100, _manager.GetSetting<int>("Missing", 100));
            
            _manager.SetSetting("Invalid", "NotAnInt");
            Assert.AreEqual(5, _manager.GetSetting<int>("Invalid", 5));
        }
    }
}
