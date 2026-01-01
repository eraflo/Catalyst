using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.Pooling;

namespace Eraflo.Catalyst.Editor.Pooling
{
    /// <summary>
    /// Custom inspector for PooledObject component.
    /// Shows pool info and provides quick actions.
    /// </summary>
    [CustomEditor(typeof(PooledObject))]
    public class PooledObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var pooledObj = (PooledObject)target;

            EditorGUILayout.Space();

            // Status box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Active state with color
            GUI.color = pooledObj.IsSpawned ? Color.green : Color.gray;
            EditorGUILayout.LabelField(pooledObj.IsSpawned ? "● SPAWNED" : "○ POOLED", EditorStyles.boldLabel);
            GUI.color = Color.white;

            if (pooledObj.IsSpawned)
            {
                EditorGUILayout.LabelField("Handle ID", pooledObj.HandleId.ToString());
                EditorGUILayout.LabelField("Pool ID", pooledObj.PoolId.ToString());
                EditorGUILayout.LabelField("Time Since Spawn", $"{pooledObj.TimeSinceSpawn:F2}s");
            }

            EditorGUILayout.EndVertical();

            // Actions
            if (Application.isPlaying && pooledObj.IsSpawned)
            {
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Despawn Now"))
                {
                    pooledObj.Despawn();
                }

                if (GUILayout.Button("Despawn in 2s"))
                {
                    pooledObj.DespawnAfter(2f);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            // Draw default inspector at the end for any serialized fields
            EditorGUILayout.Space();
            if (DrawDefaultInspector())
            {
                // Any changes made
            }
        }
    }
}
