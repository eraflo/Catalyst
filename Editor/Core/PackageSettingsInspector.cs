using UnityEditor;
using UnityEngine;

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
        private SerializedProperty _timerThreadMode;
        private SerializedProperty _enableTimerDebugLogs;
        private SerializedProperty _enableTimerPooling;
        private SerializedProperty _timerPoolDefaultCapacity;
        private SerializedProperty _timerPoolMaxCapacity;
        private SerializedProperty _timerPoolPrewarmCount;

        private bool _showNetworkSettings = true;
        private bool _showTimerSettings = true;
        private bool _showPoolSettings = true;

        private void OnEnable()
        {
            _enableNetworking = serializedObject.FindProperty("_enableNetworking");
            _networkDebugMode = serializedObject.FindProperty("_networkDebugMode");
            _timerThreadMode = serializedObject.FindProperty("_timerThreadMode");
            _enableTimerDebugLogs = serializedObject.FindProperty("_enableTimerDebugLogs");
            _enableTimerPooling = serializedObject.FindProperty("_enableTimerPooling");
            _timerPoolDefaultCapacity = serializedObject.FindProperty("_timerPoolDefaultCapacity");
            _timerPoolMaxCapacity = serializedObject.FindProperty("_timerPoolMaxCapacity");
            _timerPoolPrewarmCount = serializedObject.FindProperty("_timerPoolPrewarmCount");
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

            // Timer Settings
            _showTimerSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showTimerSettings, "Timer System");
            if (_showTimerSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_timerThreadMode, new GUIContent("Thread Mode", 
                    "SingleThread = faster, ThreadSafe = safe from any thread"));
                EditorGUILayout.PropertyField(_enableTimerDebugLogs, new GUIContent("Debug Logs"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            // Timer Pool Settings
            _showPoolSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showPoolSettings, "Timer Pool");
            if (_showPoolSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_enableTimerPooling, new GUIContent("Enable Pooling"));
                
                using (new EditorGUI.DisabledScope(!_enableTimerPooling.boolValue))
                {
                    EditorGUILayout.Space(3);
                    
                    EditorGUILayout.PropertyField(_timerPoolDefaultCapacity, 
                        new GUIContent("Default Capacity", "Initial pool size per timer type"));
                    
                    EditorGUILayout.PropertyField(_timerPoolMaxCapacity, 
                        new GUIContent("Max Capacity", "Maximum pooled timers per type"));
                    
                    EditorGUILayout.PropertyField(_timerPoolPrewarmCount, 
                        new GUIContent("Prewarm Count", "Timers to prewarm on startup (0 = disabled)"));

                    EditorGUILayout.Space(5);
                    
                    // Pool stats in play mode
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField("Pool Status", EditorStyles.boldLabel);
                        
                        var totalPooled = Timers.TimerPool.TotalPooledCount;
                        EditorGUILayout.LabelField("Total Pooled: " + totalPooled);
                        
                        // Use reflection to display all timer types
                        DisplayPoolSizes();
                        
                        EditorGUILayout.EndVertical();

                        if (GUILayout.Button("Clear Pool"))
                        {
                            Timers.TimerPool.Clear();
                        }
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(10);

            // Validation
            if (_timerPoolDefaultCapacity.intValue > _timerPoolMaxCapacity.intValue)
            {
                EditorGUILayout.HelpBox("Default Capacity cannot exceed Max Capacity", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Displays pool sizes for all Timer types using reflection.
        /// </summary>
        private void DisplayPoolSizes()
        {
            var timerBaseType = typeof(Timers.Timer);
            var getPoolSizeMethod = typeof(Timers.TimerPool).GetMethod("GetPoolSize");

            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface || type == timerBaseType)
                            continue;

                        if (!timerBaseType.IsAssignableFrom(type))
                            continue;

                        // Call GetPoolSize<T>() via reflection
                        var genericMethod = getPoolSizeMethod.MakeGenericMethod(type);
                        var count = (int)genericMethod.Invoke(null, null);
                        
                        if (count > 0)
                        {
                            EditorGUILayout.LabelField(type.Name + ": " + count);
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be reflected
                }
            }
        }
    }
}
