using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Eraflo.Catalyst.BehaviourTree.Editor
{
    /// <summary>
    /// Custom property drawer for BlackboardKey fields.
    /// Shows a dropdown with available keys from the selected BehaviourTree's Blackboard.
    /// </summary>
    [CustomPropertyDrawer(typeof(BlackboardKeyAttribute))]
    public class BlackboardKeyDrawer : PropertyDrawer
    {
        private string[] _cachedKeys;
        private BehaviourTree _lastBTContext;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            // Get available keys from the current BehaviourTree context
            var keys = GetAvailableKeys(property);
            
            if (keys.Count == 0)
            {
                // No keys available, show text field with helper
                var fieldRect = new Rect(position.x, position.y, position.width - 60, position.height);
                var buttonRect = new Rect(position.x + position.width - 55, position.y, 55, position.height);
                
                EditorGUI.PropertyField(fieldRect, property, label);
                
                GUI.enabled = false;
                GUI.Button(buttonRect, "No Keys");
                GUI.enabled = true;
                return;
            }
            
            // Prepare dropdown options
            var options = new List<string> { "(None)" };
            options.AddRange(keys);
            
            int currentIndex = 0;
            string currentValue = property.stringValue;
            
            if (!string.IsNullOrEmpty(currentValue))
            {
                int foundIndex = options.IndexOf(currentValue);
                if (foundIndex >= 0)
                {
                    currentIndex = foundIndex;
                }
                else
                {
                    // Current value not in list, add it with warning
                    options.Add($"{currentValue} (Missing!)");
                    currentIndex = options.Count - 1;
                }
            }
            
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options.ToArray());
            
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                {
                    property.stringValue = "";
                }
                else if (newIndex < options.Count)
                {
                    string selected = options[newIndex];
                    // Remove " (Missing!)" suffix if present
                    if (selected.EndsWith(" (Missing!)"))
                    {
                        selected = selected.Replace(" (Missing!)", "");
                    }
                    property.stringValue = selected;
                }
            }
        }
        
        private List<string> GetAvailableKeys(SerializedProperty property)
        {
            var attr = attribute as BlackboardKeyAttribute;

            // Try to find the BehaviourTree context
            var targetObject = property.serializedObject.targetObject;
            BehaviourTree btContext = null;

            if (targetObject is Node node)
            {
                btContext = node.Tree;

                if (btContext == null)
                {
                    var path = AssetDatabase.GetAssetPath(node);
                    if (!string.IsNullOrEmpty(path))
                    {
                        btContext = AssetDatabase.LoadMainAssetAtPath(path) as BehaviourTree;
                    }
                }
            }

            // NOTE: This fallback is unreliable when a different BehaviourTree asset is selected.
            // The BlackboardKey popup may display keys from the wrong tree in that case.
            if (btContext == null && Selection.activeObject is BehaviourTree selectedTree)
            {
                btContext = selectedTree;
            }

            // Rebuild cache only when the tree context has changed.
            if (btContext != _lastBTContext)
            {
                _lastBTContext = btContext;

                var keysWithTypes = new Dictionary<string, System.Type>();
                if (btContext?.Blackboard != null)
                {
                    keysWithTypes = btContext.Blackboard.GetKeysAndTypes();
                }

                if (attr != null && attr.ExpectedType != null)
                {
                    _cachedKeys = keysWithTypes
                        .Where(kvp => kvp.Value == null || attr.ExpectedType.IsAssignableFrom(kvp.Value))
                        .Select(kvp => kvp.Key)
                        .OrderBy(k => k)
                        .ToArray();
                }
                else
                {
                    _cachedKeys = keysWithTypes.Keys.OrderBy(k => k).ToArray();
                }
            }

            return _cachedKeys != null ? new List<string>(_cachedKeys) : new List<string>();
        }
    }
}
