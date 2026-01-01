using UnityEngine;
using UnityEditor;
using Eraflo.Catalyst.BehaviourTree;
using System.Linq;
using BTNode = Eraflo.Catalyst.BehaviourTree.Node;

namespace Eraflo.Catalyst.Editor.BehaviourTree
{
    /// <summary>
    /// Custom inspector for Node ScriptableObjects.
    /// Shows node-type-specific editing and child management.
    /// </summary>
    [CustomEditor(typeof(BTNode), true)]
    public class NodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var node = target as BTNode;
            
            // Header
            EditorGUILayout.Space();
            string nodeType = GetNodeTypeName(node);
            EditorGUILayout.LabelField($"{nodeType}: {node.name}", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Description
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Description"));
            
            EditorGUILayout.Space();
            
            // Node-specific properties
            DrawNodeSpecificProperties(node);
            
            // Children management for composite nodes
            if (node is CompositeNode composite)
            {
                DrawCompositeChildren(composite);
            }
            
            // Child management for decorator nodes
            if (node is DecoratorNode decorator)
            {
                DrawDecoratorChild(decorator);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawNodeSpecificProperties(BTNode node)
        {
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                
                // Skip base class properties we handle separately
                if (iterator.name == "m_Script") continue;
                if (iterator.name == "State") continue;
                if (iterator.name == "Started") continue;
                if (iterator.name == "Guid") continue;
                if (iterator.name == "Position") continue;
                if (iterator.name == "Description") continue;
                if (iterator.name == "Children") continue;
                if (iterator.name == "Child") continue;
                
                EditorGUILayout.PropertyField(iterator, true);
            }
        }
        
        private void DrawCompositeChildren(CompositeNode composite)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
            
            if (composite.Children.Count == 0)
            {
                EditorGUILayout.HelpBox("No children. Drag nodes here or use the Add button.", MessageType.Info);
            }
            
            for (int i = 0; i < composite.Children.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                var child = composite.Children[i];
                string childName = child != null ? child.name : "(null)";
                EditorGUILayout.LabelField($"[{i}] {childName}");
                
                // Move up/down
                GUI.enabled = i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(25)))
                {
                    Undo.RecordObject(composite, "Move Child Up");
                    (composite.Children[i], composite.Children[i - 1]) = (composite.Children[i - 1], composite.Children[i]);
                    EditorUtility.SetDirty(composite);
                }
                
                GUI.enabled = i < composite.Children.Count - 1;
                if (GUILayout.Button("↓", GUILayout.Width(25)))
                {
                    Undo.RecordObject(composite, "Move Child Down");
                    (composite.Children[i], composite.Children[i + 1]) = (composite.Children[i + 1], composite.Children[i]);
                    EditorUtility.SetDirty(composite);
                }
                
                GUI.enabled = true;
                
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = child;
                }
                
                GUI.color = new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(composite, "Remove Child");
                    composite.Children.RemoveAt(i);
                    EditorUtility.SetDirty(composite);
                    i--;
                }
                GUI.color = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Add child dropdown
            EditorGUILayout.Space();
            
            var tree = FindTreeForNode(composite);
            if (tree != null)
            {
                var availableNodes = tree.Nodes
                    .Where(n => n != composite && !composite.Children.Contains(n) && !IsAncestor(n, composite))
                    .ToArray();
                
                if (availableNodes.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Add Child:");
                    
                    if (EditorGUILayout.DropdownButton(new GUIContent("Select Node..."), FocusType.Keyboard))
                    {
                        var menu = new GenericMenu();
                        foreach (var node in availableNodes)
                        {
                            var capturedNode = node;
                            menu.AddItem(new GUIContent(node.name), false, () =>
                            {
                                Undo.RecordObject(composite, "Add Child");
                                composite.Children.Add(capturedNode);
                                EditorUtility.SetDirty(composite);
                            });
                        }
                        menu.ShowAsContext();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        private void DrawDecoratorChild(DecoratorNode decorator)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Child", EditorStyles.boldLabel);
            
            if (decorator.Child == null)
            {
                EditorGUILayout.HelpBox("No child assigned.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(decorator.Child.name);
                
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = decorator.Child;
                }
                
                GUI.color = new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    Undo.RecordObject(decorator, "Clear Child");
                    decorator.Child = null;
                    EditorUtility.SetDirty(decorator);
                }
                GUI.color = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Set child dropdown
            var tree = FindTreeForNode(decorator);
            if (tree != null && decorator.Child == null)
            {
                var availableNodes = tree.Nodes
                    .Where(n => n != decorator && !IsAncestor(n, decorator))
                    .ToArray();
                
                if (availableNodes.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Set Child:");
                    
                    if (EditorGUILayout.DropdownButton(new GUIContent("Select Node..."), FocusType.Keyboard))
                    {
                        var menu = new GenericMenu();
                        foreach (var node in availableNodes)
                        {
                            var capturedNode = node;
                            menu.AddItem(new GUIContent(node.name), false, () =>
                            {
                                Undo.RecordObject(decorator, "Set Child");
                                decorator.Child = capturedNode;
                                EditorUtility.SetDirty(decorator);
                            });
                        }
                        menu.ShowAsContext();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        private Eraflo.Catalyst.BehaviourTree.BehaviourTree FindTreeForNode(BTNode node)
        {
            string assetPath = AssetDatabase.GetAssetPath(node);
            if (string.IsNullOrEmpty(assetPath)) return null;
            
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return mainAsset as Eraflo.Catalyst.BehaviourTree.BehaviourTree;
        }
        
        private bool IsAncestor(BTNode potentialAncestor, BTNode node)
        {
            if (potentialAncestor is CompositeNode composite)
            {
                foreach (var child in composite.Children)
                {
                    if (child == node || IsAncestor(child, node))
                        return true;
                }
            }
            else if (potentialAncestor is DecoratorNode decorator)
            {
                if (decorator.Child == node || (decorator.Child != null && IsAncestor(decorator.Child, node)))
                    return true;
            }
            
            return false;
        }
        
        private string GetNodeTypeName(BTNode node)
        {
            if (node is CompositeNode) return "Composite";
            if (node is DecoratorNode) return "Decorator";
            if (node is ActionNode) return "Action";
            if (node is ConditionNode) return "Condition";
            return "Node";
        }
    }
}
