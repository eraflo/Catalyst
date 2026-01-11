using System;
using UnityEngine;
using UnityEngine.UI;
using Eraflo.Catalyst.Core.Settings;
using TMPro;

namespace Eraflo.Catalyst.UI.Settings
{
    /// <summary>
    /// Component to bind a UI element to a specific setting key in the SettingsManager.
    /// Supports Sliders (f), Toggles (b), and Dropdowns (i).
    /// </summary>
    [AddComponentMenu("Catalyst/UI/Settings/UI Setting Binder")]
    public class UISettingBinder : MonoBehaviour
    {
        [SerializeField] private string _settingKey;
        [SerializeField] private string _pageId;

        private Slider _slider;
        private Toggle _toggle;
        private TMP_Dropdown _dropdown;

        private SettingsManager _manager;

        private void Start()
        {
            _manager = App.Get<SettingsManager>();
            if (_manager == null) return;

            // Cache components
            _slider = GetComponent<Slider>();
            _toggle = GetComponent<Toggle>();
            _dropdown = GetComponent<TMP_Dropdown>();

            RefreshUI();

            // Register listeners
            if (_slider != null) _slider.onValueChanged.AddListener(OnSliderChanged);
            if (_toggle != null) _toggle.onValueChanged.AddListener(OnToggleChanged);
            if (_dropdown != null) _dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        public void RefreshUI()
        {
            if (_manager?.Data == null || string.IsNullOrEmpty(_settingKey)) return;

            if (_slider != null) _slider.value = GetValue<float>();
            if (_toggle != null) _toggle.isOn = GetValue<bool>();
            if (_dropdown != null) _dropdown.value = GetValue<int>();
        }

        private T GetValue<T>()
        {
            // First check strongly typed fields via reflection if they match the key
            var field = typeof(SettingsData).GetField(_settingKey);
            if (field != null && field.FieldType == typeof(T))
            {
                return (T)field.GetValue(_manager.Data);
            }

            // Fallback to custom dictionary
            return _manager.GetSetting<T>(_settingKey);
        }

        private void SetValue<T>(T value)
        {
            var field = typeof(SettingsData).GetField(_settingKey);
            if (field != null && field.FieldType == typeof(T))
            {
                field.SetValue(_manager.Data, value);
            }
            else
            {
                _manager.SetSetting(_settingKey, value);
            }

            // Apply specific page if provided, or all
            if (!string.IsNullOrEmpty(_pageId))
                _manager.ApplyPage(_pageId);
            else
                _manager.ApplyAll();
        }

        private void OnSliderChanged(float value) => SetValue(value);
        private void OnToggleChanged(bool value) => SetValue(value);
        private void OnDropdownChanged(int value) => SetValue(value);
    }
}
