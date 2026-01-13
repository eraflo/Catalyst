using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Eraflo.Catalyst.Core.Settings;
using Eraflo.Catalyst.UI.Settings;
using TMPro;

namespace Eraflo.Catalyst.Tests
{
    public class UISettingBinderTests
    {
        private GameObject _go;
        private UISettingBinder _binder;
        private Slider _slider;
        private Toggle _toggle;
        private SettingsManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SettingsManager();
            App.Register(_manager);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            App.Shutdown();
        }

        [UnityTest]
        public IEnumerator Binder_Reflection_UpdatesStronglyTypedField()
        {
            _go = new GameObject("TestSlider");
            _slider = _go.AddComponent<Slider>();
            _binder = _go.AddComponent<UISettingBinder>();

            // Manually inject dependencies
            var managerField = typeof(UISettingBinder).GetField("_manager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            managerField.SetValue(_binder, _manager);
            
            var sliderField = typeof(UISettingBinder).GetField("_slider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sliderField.SetValue(_binder, _slider);

            var keyField = typeof(UISettingBinder).GetField("_settingKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            keyField.SetValue(_binder, "MasterVolume");

            // Test value update from manager to UI
            _manager.Data.MasterVolume = 0.75f;
            _binder.RefreshUI();

            Assert.AreEqual(0.75f, _slider.value);

            // Test value update from UI to manager
            _slider.value = 0.25f;
            var method = typeof(UISettingBinder).GetMethod("OnSliderChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_binder, new object[] { 0.25f });

            Assert.AreEqual(0.25f, _manager.Data.MasterVolume);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Binder_Fallback_UpdatesCustomDictionary()
        {
            _go = new GameObject("TestToggle");
            _toggle = _go.AddComponent<Toggle>();
            _binder = _go.AddComponent<UISettingBinder>();

            // Manually inject dependencies
            var managerField = typeof(UISettingBinder).GetField("_manager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            managerField.SetValue(_binder, _manager);
            
            var toggleField = typeof(UISettingBinder).GetField("_toggle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            toggleField.SetValue(_binder, _toggle);

            var keyField = typeof(UISettingBinder).GetField("_settingKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            keyField.SetValue(_binder, "CustomToggle");

            _manager.SetSetting("CustomToggle", true);
            _binder.RefreshUI();

            Assert.IsTrue(_toggle.isOn);

            _toggle.isOn = false;
            var method = typeof(UISettingBinder).GetMethod("OnToggleChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(_binder, new object[] { false });

            Assert.IsFalse(_manager.GetSetting<bool>("CustomToggle"));
            yield return null;
        }
    }
}
