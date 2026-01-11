using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.Core.Settings;
using Eraflo.Catalyst.UI.Settings;

namespace Eraflo.Catalyst.Editor.Settings
{
    [CustomEditor(typeof(UISettingBinder))]
    public class UISettingBinderEditor : UnityEditor.Editor
    {
        private SerializedProperty _settingKeyProp;
        private SerializedProperty _pageIdProp;

        private string[] _availableKeys;
        private string[] _availablePages;
        private int _keyIndex;
        private int _pageIndex;

        private void OnEnable()
        {
            _settingKeyProp = serializedObject.FindProperty("_settingKey");
            _pageIdProp = serializedObject.FindProperty("_pageId");

            RefreshAvailableOptions();
        }

        private void RefreshAvailableOptions()
        {
            // We need to find available keys from SettingsData reflexivity 
            // and from registered pages if possible.
            // Since we are in Editor, we can't easily access the runtime SettingsManager.
            // But we can scan the assembly for ISettingsPage implementations.

            var keys = new HashSet<string>();
            var pages = new HashSet<string>();

            // 1. Get fields from SettingsData
            var fields = typeof(SettingsData).GetFields()
                .Where(f => f.IsPublic && !f.IsStatic)
                .Select(f => f.Name);
            foreach (var f in fields) keys.Add(f);

            // 2. Scan for ISettingsPage types to find their keys
            var pageTypes = TypeCache.GetTypesDerivedFrom<ISettingsPage>();
            foreach (var type in pageTypes)
            {
                if (type.IsAbstract || type.IsInterface) continue;
                try 
                {
                    var page = (ISettingsPage)System.Activator.CreateInstance(type);
                    pages.Add(page.Id);
                    foreach (var key in page.GetSettingKeys()) keys.Add(key);
                }
                catch { }
            }

            _availableKeys = keys.OrderBy(k => k).ToArray();
            _availablePages = pages.OrderBy(p => p).Prepend("All Pages").ToArray();

            _keyIndex = System.Array.IndexOf(_availableKeys, _settingKeyProp.stringValue);
            if (_keyIndex < 0) _keyIndex = 0;

            _pageIndex = System.Array.IndexOf(_availablePages, _pageIdProp.stringValue);
            if (_pageIndex < 0) _pageIndex = 0;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Binding Configuration", EditorStyles.boldLabel);

            // Setting Key Dropdown
            EditorGUI.BeginChangeCheck();
            _keyIndex = EditorGUILayout.Popup("Setting Key", _keyIndex, _availableKeys);
            if (EditorGUI.EndChangeCheck())
            {
                _settingKeyProp.stringValue = _availableKeys[_keyIndex];
            }

            if (_keyIndex == 0 && string.IsNullOrEmpty(_settingKeyProp.stringValue))
            {
                EditorGUILayout.PropertyField(_settingKeyProp, new GUIContent("Custom Key"));
            }

            // Page ID Dropdown (to know which page to re-apply)
            EditorGUI.BeginChangeCheck();
            _pageIndex = EditorGUILayout.Popup("Target Page", _pageIndex, _availablePages);
            if (EditorGUI.EndChangeCheck())
            {
                _pageIdProp.stringValue = _pageIndex == 0 ? string.Empty : _availablePages[_pageIndex];
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Meta-Data"))
            {
                RefreshAvailableOptions();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
