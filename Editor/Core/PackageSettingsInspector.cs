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
        private SerializedProperty _defaultAuthorityMode;
        private SerializedProperty _inputProvider;
        private SerializedProperty _inputActionAsset;
        private SerializedProperty _enableInputDebugger;
        private SerializedProperty _assetProviderType;
        private SerializedProperty _onTransitionStarted;
        private SerializedProperty _onTransitionCompleted;
        private SerializedProperty _settingsFilename;
        
        // Simulation
        private SerializedProperty _simulateLatencyMs;
        private SerializedProperty _simulatePacketLossPercent;
        private SerializedProperty _simulateJitterMs;
        
        // Culling
        private SerializedProperty _cullingCellSize;
        private SerializedProperty _cullingClientsPerFrame;
        private SerializedProperty _cullingHysteresis;

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
            _defaultAuthorityMode = serializedObject.FindProperty("_defaultAuthorityMode");
            _inputProvider = serializedObject.FindProperty("_inputProvider");
            _inputActionAsset = serializedObject.FindProperty("_inputActionAsset");
            _enableInputDebugger = serializedObject.FindProperty("_enableInputDebugger");
            _assetProviderType = serializedObject.FindProperty("_assetProviderType");
            _onTransitionStarted = serializedObject.FindProperty("_onTransitionStarted");
            _onTransitionCompleted = serializedObject.FindProperty("_onTransitionCompleted");
            _settingsFilename = serializedObject.FindProperty("_settingsFilename");
            
            _simulateLatencyMs = serializedObject.FindProperty("_simulateLatencyMs");
            _simulatePacketLossPercent = serializedObject.FindProperty("_simulatePacketLossPercent");
            _simulateJitterMs = serializedObject.FindProperty("_simulateJitterMs");
            
            _cullingCellSize = serializedObject.FindProperty("_cullingCellSize");
            _cullingClientsPerFrame = serializedObject.FindProperty("_cullingClientsPerFrame");
            _cullingHysteresis = serializedObject.FindProperty("_cullingHysteresis");
            
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
            EditorGUILayout.PropertyField(_defaultAuthorityMode, new GUIContent("Default Authority", "Global authority model for messages and handlers"));
            EditorGUILayout.PropertyField(_handlerMode, new GUIContent("Handler Mode"));

            if ((NetworkHandlerMode)_handlerMode.enumValueIndex == NetworkHandlerMode.Manual)
            {
                DrawHandlerList();
            }
            else
            {
                EditorGUILayout.HelpBox("All INetworkMessageHandler implementations will be auto-registered.", MessageType.Info);
            }
            
            if (_networkBackendId.stringValue == "netcode")
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Network Simulation (Editor/Dev)", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(_simulateLatencyMs, new GUIContent("Simulate Latency (ms)"));
                EditorGUILayout.PropertyField(_simulatePacketLossPercent, new GUIContent("Simulate Loss (%)"));
                EditorGUILayout.PropertyField(_simulateJitterMs, new GUIContent("Simulate Jitter (ms)"));
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Interest Management (Culling)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_cullingCellSize, new GUIContent("Cell Size"));
            EditorGUILayout.PropertyField(_cullingClientsPerFrame, new GUIContent("Clients Per Frame", "Number of clients to update culling for each frame"));
            EditorGUILayout.PropertyField(_cullingHysteresis, new GUIContent("Hysteresis", "Distance added to radius to prevent rapid toggling"));
            
            EditorGUILayout.Space(10);

            DrawHeader("⏱ Timers");
            EditorGUILayout.PropertyField(_useBurstTimers, new GUIContent("Use Burst"));
            EditorGUILayout.PropertyField(_enableTimerDebugLogs, new GUIContent("Debug Logs"));
            EditorGUILayout.PropertyField(_enableDebugOverlay, new GUIContent("Debug Overlay"));

            EditorGUILayout.Space(10);

            DrawHeader("⌨ Input System");
            EditorGUILayout.PropertyField(_inputProvider, new GUIContent("Provider", "Input backend (Legacy or New Input System)"));
            if ((InputProviderType)_inputProvider.enumValueIndex == InputProviderType.InputSystem)
            {
                EditorGUILayout.PropertyField(_inputActionAsset, new GUIContent("Action Asset", "Required for New Input System"));
            }
            EditorGUILayout.PropertyField(_enableInputDebugger, new GUIContent("Enable Debugger", "Show real-time input buffer in-game"));

            EditorGUILayout.Space(10);

            DrawHeader("📦 Assets");
            EditorGUILayout.PropertyField(_assetProviderType, new GUIContent("Provider Type", "Resources or Addressables"));

            EditorGUILayout.Space(10);

            DrawHeader("🎬 Scene Flow");
            EditorGUILayout.PropertyField(_onTransitionStarted, new GUIContent("On Transition Started"));
            EditorGUILayout.PropertyField(_onTransitionCompleted, new GUIContent("On Transition Completed"));

            EditorGUILayout.Space(10);

            DrawHeader("💾 Settings Manager");
            EditorGUILayout.PropertyField(_settingsFilename, new GUIContent("Settings Filename", "Name of the file used for settings persistence"));

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
