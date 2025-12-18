using UnityEditor;
using UnityEngine;
using Eraflo.UnityImportPackage.Timers;

namespace Eraflo.UnityImportPackage.Editor
{
    /// <summary>
    /// Custom inspector for PackageSettings with organized sections.
    /// </summary>
    [CustomEditor(typeof(PackageSettings))]
    public class PackageSettingsInspector : UnityEditor.Editor
    {
        private SerializedProperty _enableNetworking;
        private SerializedProperty _networkDebugMode;
        private SerializedProperty _threadMode;
        private SerializedProperty _enableTimerDebugLogs;
        private SerializedProperty _enableDebugOverlay;
        private SerializedProperty _useBurstTimers;

        private bool _showNetworkSettings = true;
        private bool _showTimerSettings = true;

        private void OnEnable()
        {
            _enableNetworking = serializedObject.FindProperty("_enableNetworking");
            _networkDebugMode = serializedObject.FindProperty("_networkDebugMode");
            _threadMode = serializedObject.FindProperty("_threadMode");
            _enableTimerDebugLogs = serializedObject.FindProperty("_enableTimerDebugLogs");
            _enableDebugOverlay = serializedObject.FindProperty("_enableDebugOverlay");
            _useBurstTimers = serializedObject.FindProperty("_useBurstTimers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Unity Import Package Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Network Settings
            _showNetworkSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showNetworkSettings, "Network Events");
            if (_showNetworkSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_enableNetworking, new GUIContent("Enable Networking"));
                
                using (new EditorGUI.DisabledScope(!_enableNetworking.boolValue))
                {
                    EditorGUILayout.PropertyField(_networkDebugMode, new GUIContent("Debug Mode"));
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            // Global Settings
            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_threadMode, new GUIContent("Thread Mode", 
                "SingleThread = faster, ThreadSafe = safe from any thread"));
            EditorGUILayout.Space(5);

            // Timer Settings
            _showTimerSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showTimerSettings, "Timer System");
            if (_showTimerSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_useBurstTimers, new GUIContent("Use Optimized Backend", 
                    "Uses array-based backend for better performance"));
                EditorGUILayout.PropertyField(_enableTimerDebugLogs, new GUIContent("Debug Logs"));
                EditorGUILayout.PropertyField(_enableDebugOverlay, new GUIContent("Debug Overlay", 
                    "Show runtime overlay with active timers"));

                // Timer stats in play mode
                if (Application.isPlaying)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Timer Status", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Active Timers: {Timer.Count}");
                    EditorGUILayout.LabelField($"Backend: {(Timer.IsBurstMode ? "Optimized" : "Standard")}");
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Clear All Timers"))
                    {
                        Timer.Clear();
                    }
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
