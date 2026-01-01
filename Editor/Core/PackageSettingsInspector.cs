using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.Editor
{
    /// <summary>
    /// Custom inspector for PackageSettings.
    /// </summary>
    [CustomEditor(typeof(PackageSettings))]
    public class PackageSettingsInspector : UnityEditor.Editor
    {
        private List<Type> _availableHandlers;
        private bool _handlersFoldout = true;

        private SerializedProperty _threadMode;
        private SerializedProperty _networkBackendId;
        private SerializedProperty _networkDebugMode;
        private SerializedProperty _handlerMode;
        private SerializedProperty _enabledHandlers;
        private SerializedProperty _useBurstTimers;
        private SerializedProperty _enableTimerDebugLogs;
        private SerializedProperty _enableDebugOverlay;

        private void OnEnable()
        {
            _threadMode = serializedObject.FindProperty("_threadMode");
            _networkBackendId = serializedObject.FindProperty("_networkBackendId");
            _networkDebugMode = serializedObject.FindProperty("_networkDebugMode");
            _handlerMode = serializedObject.FindProperty("_handlerMode");
            _enabledHandlers = serializedObject.FindProperty("_enabledHandlers");
            _useBurstTimers = serializedObject.FindProperty("_useBurstTimers");
            _enableTimerDebugLogs = serializedObject.FindProperty("_enableTimerDebugLogs");
            _enableDebugOverlay = serializedObject.FindProperty("_enableDebugOverlay");
            
            RefreshHandlerList();
        }

        private void RefreshHandlerList()
        {
            _availableHandlers = NetworkBootstrapper.FindAllHandlerTypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader("⚙ Global Settings");
            EditorGUILayout.PropertyField(_threadMode, new GUIContent("Thread Mode"));
            
            EditorGUILayout.Space(10);

            DrawHeader("🌐 Networking");
            EditorGUILayout.PropertyField(_networkBackendId, new GUIContent("Backend ID", "mock, netcode, or custom"));
            EditorGUILayout.PropertyField(_networkDebugMode, new GUIContent("Debug Mode"));
            EditorGUILayout.PropertyField(_handlerMode, new GUIContent("Handler Mode"));

            if ((NetworkHandlerMode)_handlerMode.enumValueIndex == NetworkHandlerMode.Manual)
            {
                DrawHandlerList();
            }
            else
            {
                EditorGUILayout.HelpBox("All INetworkMessageHandler implementations will be auto-registered.", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);

            DrawHeader("⏱ Timers");
            EditorGUILayout.PropertyField(_useBurstTimers, new GUIContent("Use Burst"));
            EditorGUILayout.PropertyField(_enableTimerDebugLogs, new GUIContent("Debug Logs"));
            EditorGUILayout.PropertyField(_enableDebugOverlay, new GUIContent("Debug Overlay"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(string title)
        {
            EditorGUILayout.Space(5);
            var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            EditorGUILayout.LabelField(title, style);
            var rect = GUILayoutUtility.GetRect(1, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(3);
        }

        private void DrawHandlerList()
        {
            EditorGUILayout.Space(5);
            _handlersFoldout = EditorGUILayout.Foldout(_handlersFoldout, "Enabled Handlers", true);
            
            if (!_handlersFoldout) return;

            EditorGUI.indentLevel++;

            if (_availableHandlers == null || _availableHandlers.Count == 0)
            {
                EditorGUILayout.HelpBox("No handlers found.", MessageType.Warning);
                if (GUILayout.Button("Refresh")) RefreshHandlerList();
            }
            else
            {
                var enabledSet = new HashSet<string>();
                for (int i = 0; i < _enabledHandlers.arraySize; i++)
                    enabledSet.Add(_enabledHandlers.GetArrayElementAtIndex(i).stringValue);

                foreach (var type in _availableHandlers)
                {
                    var typeName = type.FullName;
                    var isEnabled = enabledSet.Contains(typeName);
                    var newEnabled = EditorGUILayout.ToggleLeft(type.Name, isEnabled);
                    
                    if (newEnabled != isEnabled)
                    {
                        if (newEnabled)
                        {
                            _enabledHandlers.arraySize++;
                            _enabledHandlers.GetArrayElementAtIndex(_enabledHandlers.arraySize - 1).stringValue = typeName;
                        }
                        else
                        {
                            for (int i = 0; i < _enabledHandlers.arraySize; i++)
                            {
                                if (_enabledHandlers.GetArrayElementAtIndex(i).stringValue == typeName)
                                {
                                    _enabledHandlers.DeleteArrayElementAtIndex(i);
                                    break;
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("All", GUILayout.Width(50)))
                {
                    _enabledHandlers.ClearArray();
                    foreach (var t in _availableHandlers)
                    {
                        _enabledHandlers.arraySize++;
                        _enabledHandlers.GetArrayElementAtIndex(_enabledHandlers.arraySize - 1).stringValue = t.FullName;
                    }
                }
                if (GUILayout.Button("None", GUILayout.Width(50))) _enabledHandlers.ClearArray();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("↻", GUILayout.Width(25))) RefreshHandlerList();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
}
