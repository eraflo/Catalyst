using UnityEngine;
using UnityEditor;
using Eraflo.Catalyst.BehaviourTree;
using Eraflo.Catalyst.Editor.BehaviourTree.Window;

namespace Eraflo.Catalyst.Editor.BehaviourTree
{
    /// <summary>
    /// Custom inspector for BehaviourTreeRunner MonoBehaviour.
    /// Shows runtime tree state during play mode.
    /// </summary>
    [CustomEditor(typeof(BehaviourTreeRunner))]
    public class BehaviourTreeRunnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var runner = target as BehaviourTreeRunner;
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            // Runtime info (play mode only)
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
                
                var state = runner.TreeState;
                GUI.color = state switch
                {
                    NodeState.Running => Color.yellow,
                    NodeState.Success => Color.green,
                    NodeState.Failure => Color.red,
                    _ => Color.white
                };
                EditorGUILayout.LabelField($"Tree State: {state}");
                GUI.color = Color.white;
                
                EditorGUILayout.Space();
                
                // Blackboard viewer
                if (runner.Blackboard != null)
                {
                    EditorGUILayout.LabelField("Blackboard (Runtime Values)", EditorStyles.boldLabel);
                    
                    var keys = runner.Blackboard.GetAllKeys();
                    if (keys.Length == 0)
                    {
                        EditorGUILayout.LabelField("(empty)");
                    }
                    else
                    {
                        EditorGUI.indentLevel++;
                        foreach (var key in keys)
                        {
                            DrawBlackboardEntry(runner.Blackboard, key);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                
                EditorGUILayout.Space();
                
                // Controls
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Manual Tick"))
                {
                    runner.Tick();
                }
                
                if (GUILayout.Button("Reset Tree"))
                {
                    runner.ResetTree();
                }
                
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button("Open Runtime Graph", GUILayout.Height(30)))
                {
                    BehaviourTreeEditorWindow.OpenWindow(runner.RuntimeTree);
                }
                GUI.backgroundColor = Color.white;
                
                // Force repaint for live updates
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime state.", MessageType.Info);
            }
        }

        private void DrawBlackboardEntry(Blackboard blackboard, string key)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key, GUILayout.Width(100));

            if (blackboard.TryGet<bool>(key, out bool boolVal))
            {
                var newVal = EditorGUILayout.Toggle(boolVal);
                if (newVal != boolVal) blackboard.Set(key, newVal);
            }
            else if (blackboard.TryGet<int>(key, out int intVal))
            {
                var newVal = EditorGUILayout.IntField(intVal);
                if (newVal != intVal) blackboard.Set(key, newVal);
            }
            else if (blackboard.TryGet<float>(key, out float floatVal))
            {
                var newVal = EditorGUILayout.FloatField(floatVal);
                if (newVal != floatVal) blackboard.Set(key, newVal);
            }
            else if (blackboard.TryGet<string>(key, out string strVal))
            {
                var newVal = EditorGUILayout.TextField(strVal);
                if (newVal != strVal) blackboard.Set(key, newVal);
            }
            else if (blackboard.TryGet<Vector3>(key, out Vector3 vecVal))
            {
                var newVal = EditorGUILayout.Vector3Field("", vecVal);
                if (newVal != vecVal) blackboard.Set(key, newVal);
            }
            else if (blackboard.TryGet<GameObject>(key, out GameObject goVal))
            {
                var newVal = EditorGUILayout.ObjectField(goVal, typeof(GameObject), true) as GameObject;
                if (newVal != goVal) blackboard.Set(key, newVal);
            }
            else if (blackboard.TryGet<Transform>(key, out Transform transVal))
            {
                var newVal = EditorGUILayout.ObjectField(transVal, typeof(Transform), true) as Transform;
                if (newVal != transVal) blackboard.Set(key, newVal);
            }
            else
            {
                EditorGUILayout.LabelField("(Unsupported Type)");
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
