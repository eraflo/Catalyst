using System;
using System.Collections.Generic;
using System.Linq;
using Eraflo.Catalyst.Networking;
using UnityEditor;
using UnityEngine;

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
        private GUIStyle _headerStyle;

        private static readonly string[] _backendIdOptions = { "mock", "netcode" };

        private SerializedProperty _threadMode;
        private SerializedProperty _networkBackendId;
        private SerializedProperty _networkDebugMode;
        private SerializedProperty _handlerMode;
        private SerializedProperty _enabledHandlers;
        private SerializedProperty _allowDiscoveryPortSharing;
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

        // Discovery Security
        private SerializedProperty _discoveryMaxMessageSize;
        private SerializedProperty _discoveryMaxNameLength;
        private SerializedProperty _discoveryRateLimitPerSecond;

        // Discovery Transport
        private SerializedProperty _discoveryTransportType;
        private SerializedProperty _discoveryRelayUrl;
        private SerializedProperty _discoveryPort;

        // Lobby
        private SerializedProperty _lobbySearchTimeoutMs;
        private SerializedProperty _enableRoomPasswords;

        // Connection Security
        private SerializedProperty _enableSecureConnections;
        private SerializedProperty _maxConnectionPayloadAge;
        private SerializedProperty _maxConnectionAttemptsPerMinute;
        private SerializedProperty _connectionBanDurationSeconds;

        private void OnEnable()
        {
            _threadMode = serializedObject.FindProperty("_threadMode");
            _networkBackendId = serializedObject.FindProperty("_networkBackendId");
            _networkDebugMode = serializedObject.FindProperty("_networkDebugMode");
            _handlerMode = serializedObject.FindProperty("_handlerMode");
            _enabledHandlers = serializedObject.FindProperty("_enabledHandlers");
            _allowDiscoveryPortSharing = serializedObject.FindProperty("_allowDiscoveryPortSharing");
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

            _discoveryMaxMessageSize = serializedObject.FindProperty("_discoveryMaxMessageSize");
            _discoveryMaxNameLength = serializedObject.FindProperty("_discoveryMaxNameLength");
            _discoveryRateLimitPerSecond = serializedObject.FindProperty("_discoveryRateLimitPerSecond");

            _discoveryTransportType = serializedObject.FindProperty("_discoveryTransportType");
            _discoveryRelayUrl = serializedObject.FindProperty("_discoveryRelayUrl");
            _discoveryPort = serializedObject.FindProperty("_discoveryPort");

            _lobbySearchTimeoutMs = serializedObject.FindProperty("_lobbySearchTimeoutMs");
            _enableRoomPasswords = serializedObject.FindProperty("_enableRoomPasswords");

            _enableSecureConnections = serializedObject.FindProperty("_enableSecureConnections");
            _maxConnectionPayloadAge = serializedObject.FindProperty("_maxConnectionPayloadAge");
            _maxConnectionAttemptsPerMinute = serializedObject.FindProperty("_maxConnectionAttemptsPerMinute");
            _connectionBanDurationSeconds = serializedObject.FindProperty("_connectionBanDurationSeconds");

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

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
            int backendIndex = System.Array.IndexOf(_backendIdOptions, _networkBackendId.stringValue);
            if (backendIndex < 0) backendIndex = 0;
            int newBackendIndex = EditorGUILayout.Popup(new GUIContent("Backend ID", "mock or netcode"), backendIndex, _backendIdOptions);
            if (newBackendIndex != backendIndex)
                _networkBackendId.stringValue = _backendIdOptions[newBackendIndex];
            EditorGUILayout.PropertyField(_networkDebugMode, new GUIContent("Debug Mode"));
            EditorGUILayout.PropertyField(_defaultAuthorityMode, new GUIContent("Default Authority", "Global authority model for messages and handlers"));
            EditorGUILayout.PropertyField(_handlerMode, new GUIContent("Handler Mode"));
            EditorGUILayout.PropertyField(_allowDiscoveryPortSharing, new GUIContent("Allow Discovery Port Sharing", "Enable multiple instances on the same machine to share the discovery port (47777)."));

            EditorGUILayout.Space(10);

            DrawHeader("📡 Discovery Transport");
            EditorGUILayout.PropertyField(_discoveryTransportType, new GUIContent("Transport Type"));
            
            // Show relay URL only for WebSocket
            if ((DiscoveryTransportType)_discoveryTransportType.enumValueIndex == DiscoveryTransportType.WebSocket)
            {
                EditorGUILayout.PropertyField(_discoveryRelayUrl, new GUIContent("Relay URL"));
                EditorGUILayout.HelpBox("Enter your WebSocket relay server URL (e.g., wss://relay.example.com)", MessageType.Info);
            }
            
            // Show port only for UDP
            if ((DiscoveryTransportType)_discoveryTransportType.enumValueIndex == DiscoveryTransportType.UdpBroadcast)
            {
                EditorGUILayout.PropertyField(_discoveryPort, new GUIContent("Discovery Port"));
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Security", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_discoveryMaxMessageSize, new GUIContent("Max Message Size", "Maximum packet size to accept (default: 512 bytes)"));
            EditorGUILayout.PropertyField(_discoveryMaxNameLength, new GUIContent("Max Name Length", "Maximum lobby name length (default: 64 chars)"));
            EditorGUILayout.PropertyField(_discoveryRateLimitPerSecond, new GUIContent("Rate Limit/sec", "Max packets per second per IP (default: 10)"));

            EditorGUILayout.Space(10);

            DrawHeader("🚪 Lobby");
            EditorGUILayout.PropertyField(_lobbySearchTimeoutMs, new GUIContent("Search Timeout (ms)"));
            EditorGUILayout.PropertyField(_enableRoomPasswords, new GUIContent("Enable Passwords"));

            EditorGUILayout.Space(10);

            DrawHeader("🔐 Connection Security");
            EditorGUILayout.PropertyField(_enableSecureConnections, new GUIContent("Enable Secure Connections", "Sign and validate connection payloads with HMAC"));
            if (_enableSecureConnections.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_maxConnectionPayloadAge, new GUIContent("Max Payload Age (sec)", "Reject payloads older than this"));
                EditorGUILayout.PropertyField(_maxConnectionAttemptsPerMinute, new GUIContent("Max Attempts/min", "Before temporary ban"));
                EditorGUILayout.PropertyField(_connectionBanDurationSeconds, new GUIContent("Ban Duration (sec)"));
                EditorGUI.indentLevel--;
            }

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
            if (_headerStyle == null)
                _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            EditorGUILayout.LabelField(title, _headerStyle);
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
