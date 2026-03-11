using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Eraflo.Catalyst.InputSystem.Combos;

namespace Eraflo.Catalyst.Editor.Input
{
    /// <summary>
    /// Custom inspector for <see cref="ComboDatabase"/> that renders each combo as a compact
    /// visual row, detects sequence overlaps, and provides an Add Combo button.
    /// </summary>
    [CustomEditor(typeof(ComboDatabase))]
    public class ComboDatabaseEditor : UnityEditor.Editor
    {
        private readonly Dictionary<int, bool> _foldouts = new Dictionary<int, bool>();
        private GUIStyle _actionBoxStyle;
        private GUIStyle _arrowStyle;

        private void EnsureStyles()
        {
            if (_actionBoxStyle != null) return;

            _actionBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { textColor = Color.white, background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f)) },
                padding = new RectOffset(6, 6, 2, 2),
                margin = new RectOffset(2, 2, 2, 2),
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };

            _arrowStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(2, 2, 2, 2),
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            var db = (ComboDatabase)target;

            // Header row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Combo Definitions", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {db.Combos?.Count ?? 0}", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (db.Combos == null || db.Combos.Count == 0)
            {
                EditorGUILayout.HelpBox("No combos defined. Add one below.", MessageType.Info);
            }
            else
            {
                var overlaps = FindOverlaps(db.Combos);
                var overlappingIndices = new HashSet<int>();
                foreach (var (a, b) in overlaps)
                {
                    overlappingIndices.Add(a);
                    overlappingIndices.Add(b);
                }

                for (int i = 0; i < db.Combos.Count; i++)
                {
                    var combo = db.Combos[i];
                    if (combo == null) continue;
                    DrawComboEntry(combo, i, overlappingIndices.Contains(i));
                }

                // Overlap warnings
                if (overlaps.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    foreach (var (a, b) in overlaps)
                    {
                        string nameA = db.Combos[a]?.ComboId ?? $"[{a}]";
                        string nameB = db.Combos[b]?.ComboId ?? $"[{b}]";
                        EditorGUILayout.HelpBox(
                            $"Overlapping combos detected: {nameA} is a prefix of {nameB}.",
                            MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.Space(8);
            DrawAddComboButton(db);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawComboEntry(ComboDefinition combo, int index, bool hasOverlap)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // --- Row 1: warning icon + foldout label + play-mode test button ---
            EditorGUILayout.BeginHorizontal();

            if (hasOverlap)
            {
                var warnStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.yellow },
                    fontSize = 13
                };
                GUILayout.Label("\u26a0", warnStyle, GUILayout.Width(20));
            }
            else
            {
                GUILayout.Space(4);
            }

            bool expanded = _foldouts.TryGetValue(index, out bool f) && f;
            string comboLabel = string.IsNullOrEmpty(combo.ComboId) ? "(unnamed)" : combo.ComboId;
            expanded = EditorGUILayout.Foldout(expanded, comboLabel, true, EditorStyles.foldoutHeader);
            _foldouts[index] = expanded;

            GUILayout.FlexibleSpace();

            if (Application.isPlaying)
            {
                if (GUILayout.Button("\u25b6 Test", EditorStyles.miniButton, GUILayout.Width(58)))
                {
                    var comboSystem = App.Get<ComboSystem>();
                    if (comboSystem != null)
                        Debug.Log($"[ComboDatabaseEditor] Testing: {combo.ComboId}");
                    else
                        Debug.Log($"[ComboDatabaseEditor] Testing: {combo.ComboId} (ComboSystem not registered via App.Get)");
                }
            }

            EditorGUILayout.EndHorizontal();

            // --- Row 2: visual action sequence ---
            if (combo.Sequence != null && combo.Sequence.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                for (int j = 0; j < combo.Sequence.Count; j++)
                {
                    GUILayout.Box(combo.Sequence[j], _actionBoxStyle);
                    if (j < combo.Sequence.Count - 1)
                        GUILayout.Label("\u25b6", _arrowStyle, GUILayout.Width(18));
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.LabelField("(empty sequence)", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // --- Foldout detail: full SerializedProperty fields of the ComboDefinition asset ---
            if (expanded)
            {
                EditorGUI.indentLevel++;
                var comboSO = new SerializedObject(combo);
                comboSO.Update();

                var prop = comboSO.GetIterator();
                prop.NextVisible(true); // skip m_Script
                while (prop.NextVisible(false))
                {
                    EditorGUILayout.PropertyField(prop, true);
                }

                if (comboSO.hasModifiedProperties)
                {
                    comboSO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(combo);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAddComboButton(ComboDatabase db)
        {
            if (GUILayout.Button("+ Add Combo"))
            {
                string dbPath = AssetDatabase.GetAssetPath(target);
                string dir = System.IO.Path.GetDirectoryName(dbPath)?.Replace("\\", "/") ?? "Assets";
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/NewComboDefinition.asset");

                var newCombo = ScriptableObject.CreateInstance<ComboDefinition>();
                newCombo.ComboId = "NewCombo";
                AssetDatabase.CreateAsset(newCombo, assetPath);
                AssetDatabase.SaveAssets();

                Undo.RecordObject(db, "Add Combo");
                db.Combos.Add(newCombo);
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }

        // ── Overlap detection ──────────────────────────────────────────────────────

        private static List<(int a, int b)> FindOverlaps(List<ComboDefinition> combos)
        {
            var result = new List<(int, int)>();
            for (int i = 0; i < combos.Count; i++)
            for (int j = 0; j < combos.Count; j++)
            {
                if (i == j) continue;
                if (combos[i] == null || combos[j] == null) continue;
                if (combos[i].Sequence == null || combos[j].Sequence == null) continue;
                // combos[i].Sequence is a prefix of combos[j].Sequence
                if (IsPrefix(combos[j].Sequence, combos[i].Sequence))
                    result.Add((i, j));
            }
            return result;
        }

        /// <summary>Returns true if <paramref name="prefix"/> is a strict prefix of <paramref name="sequence"/>.</summary>
        private static bool IsPrefix(List<string> sequence, List<string> prefix)
        {
            if (prefix.Count == 0 || prefix.Count >= sequence.Count) return false;
            for (int i = 0; i < prefix.Count; i++)
                if (sequence[i] != prefix[i]) return false;
            return true;
        }
    }
}
