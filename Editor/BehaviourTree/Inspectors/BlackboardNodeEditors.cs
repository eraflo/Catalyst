using UnityEngine;
using UnityEditor;

namespace Eraflo.Catalyst.BehaviourTree.Editor
{
    /// <summary>
    /// Custom editor for BlackboardConditional to show only relevant value field.
    /// </summary>
    [CustomEditor(typeof(BlackboardConditional))]
    public class BlackboardConditionalEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Draw Key
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Key"));
            
            // Draw CompareOperator
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CompareOperator"));
            
            // Draw Type
            var typeProp = serializedObject.FindProperty("Type");
            EditorGUILayout.PropertyField(typeProp);
            
            // Draw only the relevant value field based on Type
            var valueType = (BlackboardConditional.ValueType)typeProp.enumValueIndex;
            
            switch (valueType)
            {
                case BlackboardConditional.ValueType.Bool:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("BoolValue"), new GUIContent("Value"));
                    break;
                case BlackboardConditional.ValueType.Int:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("IntValue"), new GUIContent("Value"));
                    break;
                case BlackboardConditional.ValueType.Float:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("FloatValue"), new GUIContent("Value"));
                    break;
                case BlackboardConditional.ValueType.String:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("StringValue"), new GUIContent("Value"));
                    break;
                case BlackboardConditional.ValueType.Exists:
                    // No value needed for Exists check
                    EditorGUILayout.HelpBox("Checks if the key exists in the blackboard.", MessageType.Info);
                    break;
            }
            
            // Draw Child (from DecoratorNode)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Child"));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
    
    /// <summary>
    /// Custom editor for BlackboardCondition to show only relevant value field.
    /// </summary>
    [CustomEditor(typeof(BlackboardCondition))]
    public class BlackboardConditionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Draw Key
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Key"));
            
            // Draw CompareOperator
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CompareOperator"));
            
            // Draw Type
            var typeProp = serializedObject.FindProperty("Type");
            EditorGUILayout.PropertyField(typeProp);
            
            // Draw only the relevant value field based on Type
            var valueType = (BlackboardCondition.ValueType)typeProp.enumValueIndex;
            
            switch (valueType)
            {
                case BlackboardCondition.ValueType.Bool:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("BoolValue"), new GUIContent("Value"));
                    break;
                case BlackboardCondition.ValueType.Int:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("IntValue"), new GUIContent("Value"));
                    break;
                case BlackboardCondition.ValueType.Float:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("FloatValue"), new GUIContent("Value"));
                    break;
                case BlackboardCondition.ValueType.String:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("StringValue"), new GUIContent("Value"));
                    break;
                case BlackboardCondition.ValueType.Exists:
                    // No value needed for Exists check
                    EditorGUILayout.HelpBox("Checks if the key exists in the blackboard.", MessageType.Info);
                    break;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
