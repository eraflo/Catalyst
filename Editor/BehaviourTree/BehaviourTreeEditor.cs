using UnityEngine;
using UnityEditor;
using Eraflo.Catalyst.BehaviourTree;
using Eraflo.Catalyst.Editor.BehaviourTree.Window;
using BTNode = Eraflo.Catalyst.BehaviourTree.Node;

namespace Eraflo.Catalyst.Editor.BehaviourTree
{
    /// <summary>
    /// Custom inspector for BehaviourTree ScriptableObjects.
    /// Shows tree structure and allows basic editing.
    /// </summary>
    [CustomEditor(typeof(Eraflo.Catalyst.BehaviourTree.BehaviourTree))]
    public class BehaviourTreeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var tree = target as Eraflo.Catalyst.BehaviourTree.BehaviourTree;
            
            EditorGUILayout.Space();
            
            // Open Editor button - prominent and at the top
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("Open Behaviour Tree Editor", GUILayout.Height(30)))
            {
                BehaviourTreeEditorWindow.OpenWindow(tree);
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Behaviour Tree", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Root node info
            if (tree.RootNode != null)
            {
                EditorGUILayout.LabelField("Root Node:", tree.RootNode.name);
                
                if (GUILayout.Button("Select Root Node"))
                {
                    Selection.activeObject = tree.RootNode;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No root node assigned. Open the editor to build your tree.", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Node count
            EditorGUILayout.LabelField($"Total Nodes: {tree.Nodes.Count}");
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            // Create node buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("+ Selector"))
            {
                CreateNode<Selector>(tree);
            }
            
            if (GUILayout.Button("+ Sequence"))
            {
                CreateNode<Sequence>(tree);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("+ Wait"))
            {
                CreateNode<Wait>(tree);
            }
            
            if (GUILayout.Button("+ Log"))
            {
                CreateNode<BTLog>(tree);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Node list
            if (tree.Nodes.Count > 0)
            {
                EditorGUILayout.LabelField("All Nodes", EditorStyles.boldLabel);
                
                EditorGUI.indentLevel++;
                foreach (var node in tree.Nodes)
                {
                    if (node == null) continue;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    string prefix = GetNodePrefix(node);
                    EditorGUILayout.LabelField($"{prefix} {node.name}");
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = node;
                    }
                    
                    if (tree.RootNode != node && GUILayout.Button("Set Root", GUILayout.Width(60)))
                    {
                        Undo.RecordObject(tree, "Set Root Node");
                        tree.RootNode = node;
                        EditorUtility.SetDirty(tree);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Danger zone
            EditorGUILayout.LabelField("Danger Zone", EditorStyles.boldLabel);
            GUI.color = new Color(1f, 0.7f, 0.7f);
            
            if (GUILayout.Button("Delete All Nodes"))
            {
                if (EditorUtility.DisplayDialog("Delete All Nodes", 
                    "Are you sure you want to delete all nodes? This cannot be undone.", 
                    "Delete", "Cancel"))
                {
                    DeleteAllNodes(tree);
                }
            }
            
            GUI.color = Color.white;
        }
        
        private void CreateNode<T>(Eraflo.Catalyst.BehaviourTree.BehaviourTree tree) where T : BTNode
        {
            Undo.RecordObject(tree, $"Create {typeof(T).Name}");
            var node = tree.CreateNode(typeof(T));
            
            if (tree.RootNode == null)
            {
                tree.RootNode = node;
            }
            
            Selection.activeObject = node;
        }
        
        private void DeleteAllNodes(Eraflo.Catalyst.BehaviourTree.BehaviourTree tree)
        {
            Undo.RecordObject(tree, "Delete All Nodes");
            
            foreach (var node in tree.Nodes.ToArray())
            {
                if (node != null)
                {
                    Undo.DestroyObjectImmediate(node);
                }
            }
            
            tree.Nodes.Clear();
            tree.RootNode = null;
            
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
        }
        
        private string GetNodePrefix(BTNode node)
        {
            if (node is CompositeNode) return "◇";
            if (node is DecoratorNode) return "◈";
            if (node is ActionNode) return "●";
            if (node is ConditionNode) return "◆";
            return "○";
        }
    }
}
