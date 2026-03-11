using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Editor.Core
{
    /// <summary>
    /// Custom inspector for <see cref="SaveableEntity"/>.
    /// Shows GUID management and, in play mode, per-<see cref="ISaveable"/>
    /// state preview as formatted JSON.
    /// </summary>
    [CustomEditor(typeof(SaveableEntity))]
    public class SaveableEntityEditor : UnityEditor.Editor
    {
        private readonly Dictionary<int, bool> _foldouts = new Dictionary<int, bool>();
        private readonly Dictionary<int, Vector2> _scrollPositions = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, string> _cachedJson = new Dictionary<int, string>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var entity = (SaveableEntity)target;

            DrawGuidSection(entity);

            EditorGUILayout.Space(4);

            var saveables = entity.GetComponents<ISaveable>();

            if (!Application.isPlaying)
            {
                DrawEditModeSaveables(saveables.Length);
            }
            else
            {
                DrawPlayModeSaveables(saveables);
            }
        }

        // ── GUID section ───────────────────────────────────────────────────────────

        private void DrawGuidSection(SaveableEntity entity)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Saveable Entity", EditorStyles.boldLabel);

            // GUID display (read-only, selectable for copying)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("GUID", GUILayout.Width(44));
            EditorGUILayout.SelectableLabel(
                entity.Guid,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();

            // Action buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Regenerate GUID", EditorStyles.miniButton))
            {
                if (EditorUtility.DisplayDialog(
                    "Regenerate GUID",
                    "Regenerating the GUID will break any existing save data linked to this entity. Continue?",
                    "Regenerate", "Cancel"))
                {
                    var guidProp = serializedObject.FindProperty("_guid");
                    guidProp.stringValue = Guid.NewGuid().ToString();
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                    _cachedJson.Clear();
                }
            }

            if (GUILayout.Button("Copy GUID to Clipboard", EditorStyles.miniButton))
            {
                GUIUtility.systemCopyBuffer = entity.Guid;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ── Edit-mode ISaveable section ────────────────────────────────────────────

        private void DrawEditModeSaveables(int count)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"ISaveable Components: {count}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Play mode required to preview state.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        // ── Play-mode ISaveable section ────────────────────────────────────────────

        private void DrawPlayModeSaveables(ISaveable[] saveables)
        {
            EditorGUILayout.LabelField($"ISaveable Components ({saveables.Length})", EditorStyles.boldLabel);

            if (saveables.Length == 0)
            {
                EditorGUILayout.HelpBox("No ISaveable components found on this GameObject.", MessageType.Info);
                return;
            }

            for (int i = 0; i < saveables.Length; i++)
            {
                DrawSaveableEntry(saveables[i], i);
            }
        }

        private void DrawSaveableEntry(ISaveable saveable, int index)
        {
            string typeName = saveable.GetType().Name;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool expanded = _foldouts.TryGetValue(index, out bool f) && f;
            bool newExpanded = EditorGUILayout.Foldout(expanded, typeName, true, EditorStyles.foldoutHeader);

            if (newExpanded != expanded)
            {
                _foldouts[index] = newExpanded;
                // Capture state the first time the foldout is opened
                if (newExpanded)
                    RefreshJson(saveable, index);
            }

            if (newExpanded)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60)))
                    RefreshJson(saveable, index);
                EditorGUILayout.EndHorizontal();

                string json = _cachedJson.TryGetValue(index, out string cached) ? cached : "{}";

                if (!_scrollPositions.TryGetValue(index, out var scrollPos))
                    scrollPos = Vector2.zero;

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(160));
                _scrollPositions[index] = scrollPos;

                GUI.enabled = false;
                EditorGUILayout.TextArea(json, GUILayout.ExpandHeight(true));
                GUI.enabled = true;

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private void RefreshJson(ISaveable saveable, int index)
        {
            try
            {
                var state = saveable.SaveState();
                _cachedJson[index] = state != null
                    ? JsonConvert.SerializeObject(state, Formatting.Indented)
                    : "null";
            }
            catch (Exception ex)
            {
                _cachedJson[index] = $"// Error capturing state: {ex.Message}";
            }
        }
    }
}
