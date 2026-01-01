using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.BehaviourTree;

namespace Eraflo.Catalyst.Editor.BehaviourTree
{
    /// <summary>
    /// Custom editor for RaiseEvent node.
    /// Shows the channel field and blackboard key.
    /// </summary>
    [CustomEditor(typeof(RaiseEvent))]
    public class RaiseEventEditor : UnityEditor.Editor
    {
        private SerializedProperty _channel;
        private SerializedProperty _blackboardKey;
        private SerializedProperty _description;
        
        private void OnEnable()
        {
            _channel = serializedObject.FindProperty("Channel");
            _blackboardKey = serializedObject.FindProperty("BlackboardKey");
            _description = serializedObject.FindProperty("Description");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Raise Event", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.Space();
            
            // Channel field
            EditorGUILayout.PropertyField(_channel, new GUIContent("Event Channel"));
            
            // Show type info
            var channel = _channel.objectReferenceValue;
            if (channel != null)
            {
                var channelType = channel.GetType();
                bool isVoid = channelType == typeof(Events.EventChannel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Channel Type");
                
                if (isVoid)
                {
                    EditorGUILayout.LabelField("Void (no parameters)", EditorStyles.miniLabel);
                }
                else
                {
                    // Try to find the generic parameter type
                    var baseType = channelType.BaseType;
                    while (baseType != null && !baseType.IsGenericType)
                    {
                        baseType = baseType.BaseType;
                    }
                    
                    if (baseType != null && baseType.IsGenericType)
                    {
                        var genericArgs = baseType.GetGenericArguments();
                        if (genericArgs.Length > 0)
                        {
                            EditorGUILayout.LabelField($"{genericArgs[0].Name}", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(channelType.Name, EditorStyles.miniLabel);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Only show blackboard key for typed events
                if (!isVoid)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(_blackboardKey, new GUIContent("Blackboard Key"));
                    
                    if (string.IsNullOrEmpty(_blackboardKey.stringValue))
                    {
                        EditorGUILayout.HelpBox(
                            "No blackboard key set. Will use the channel's debug value if available.",
                            MessageType.Info
                        );
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            $"Value will be read from blackboard key \"{_blackboardKey.stringValue}\".",
                            MessageType.Info
                        );
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Assign any EventChannel asset. Supports all types including custom EventChannel<T>.",
                    MessageType.Info
                );
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
