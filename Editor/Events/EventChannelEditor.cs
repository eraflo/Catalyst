using UnityEditor;
using UnityEngine;

namespace Eraflo.Catalyst.Events.Editor
{
    /// <summary>
    /// Custom editor for void EventChannel ScriptableObjects.
    /// Adds a "Raise" button for testing events in the editor.
    /// </summary>
    [CustomEditor(typeof(EventChannel))]
    public class EventChannelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EventChannel channel = target as EventChannel;
            
            EditorGUILayout.Space();
            DrawDebugSection(channel);
        }

        private void DrawDebugSection(EventChannel channel)
        {
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Subscribers:", GUILayout.Width(80));
            EditorGUILayout.LabelField(channel.SubscriberCount.ToString());
            EditorGUILayout.EndHorizontal();

            GUI.enabled = Application.isPlaying;
            
            if (GUILayout.Button("Raise Event"))
            {
                channel.Raise();
            }

            GUI.enabled = true;
        }
    }

    /// <summary>
    /// Custom editor for typed EventChannel ScriptableObjects.
    /// Automatically applies to ALL classes inheriting from EventChannel<T>.
    /// Can be overridden by creating a more specific [CustomEditor] for a particular type.
    /// </summary>
    [CustomEditor(typeof(EventChannel<>), true)]
    public class EventChannelGenericEditor : UnityEditor.Editor
    {
        private SerializedProperty _descriptionProperty;
        private SerializedProperty _debugValueProperty;

        protected virtual void OnEnable()
        {
            _descriptionProperty = serializedObject.FindProperty("_description");
            _debugValueProperty = serializedObject.FindProperty("_debugValue");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw description
            if (_descriptionProperty != null)
            {
                EditorGUILayout.PropertyField(_descriptionProperty);
            }

            EditorGUILayout.Space();
            
            // Draw debug section
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

            // Subscriber count
            var subscriberCountProperty = target.GetType().GetProperty("SubscriberCount");
            if (subscriberCountProperty != null)
            {
                int count = (int)subscriberCountProperty.GetValue(target);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Subscribers:", GUILayout.Width(80));
                EditorGUILayout.LabelField(count.ToString());
                EditorGUILayout.EndHorizontal();
            }

            // Debug value
            if (_debugValueProperty != null)
            {
                EditorGUILayout.PropertyField(_debugValueProperty, new GUIContent("Debug Value"));
            }

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Raise Event (with Debug Value)"))
            {
                // Call RaiseDebug via reflection
                var raiseDebugMethod = target.GetType().GetMethod("RaiseDebug");
                raiseDebugMethod?.Invoke(target, null);
            }

            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
