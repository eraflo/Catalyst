using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.InputSystem;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Editor.Input
{
    /// <summary>
    /// Editor window to inspect real-time input state, the input buffer, and combo progress.
    /// Open via Tools > Catalyst > Input Debugger.
    /// </summary>
    public class InputDebuggerWindow : EditorWindow
    {
        // ── Toolbar ──────────────────────────────────────────────────────────
        private bool _autoRefresh = true;

        // ── Refresh ──────────────────────────────────────────────────────────
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.1;

        // ── Cached data ───────────────────────────────────────────────────────
        private IReadOnlyList<string> _registeredActions;
        private IReadOnlyList<BufferedInput> _buffer;
        private float _currentTime;

        // ── Scroll positions ─────────────────────────────────────────────────
        private Vector2 _scrollActions;
        private Vector2 _scrollBuffer;

        // ── Cached styles (lazy-init) ────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _greenLabel;
        private GUIStyle _grayLabel;
        private GUIStyle _yellowLabel;
        private GUIStyle _whiteLabel;

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Catalyst/Input Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<InputDebuggerWindow>("Input Debugger");
            window.minSize = new Vector2(400, 450);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        // ── Polling ───────────────────────────────────────────────────────────

        private void OnEditorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < REFRESH_INTERVAL) return;
            _lastRefreshTime = EditorApplication.timeSinceStartup;

            var im = App.Get<InputManager>();
            if (im != null)
            {
                _registeredActions = im.GetRegisteredActions();
                _buffer = im.GetBuffer();
                _currentTime = Time.unscaledTime;
            }

            Repaint();
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to debug input.", MessageType.Info);
                return;
            }

            var im = App.Get<InputManager>();
            if (im == null)
            {
                EditorGUILayout.HelpBox("InputManager service not found. Ensure App is initialised.", MessageType.Warning);
                return;
            }

            DrawSectionA();
            EditorGUILayout.Space(6);
            DrawSectionB();
            EditorGUILayout.Space(6);
            DrawComboStatus();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto-Refresh", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            if (Application.isPlaying && GUILayout.Button("Clear Buffer", EditorStyles.toolbarButton))
                App.Get<InputManager>()?.ClearBuffer();
            EditorGUILayout.EndHorizontal();
        }

        // ── Section A — Action States ─────────────────────────────────────────

        private void DrawSectionA()
        {
            EditorGUILayout.LabelField("Section A — Action States", _headerStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_registeredActions == null || _registeredActions.Count == 0)
            {
                EditorGUILayout.HelpBox("No registered actions. Call InputManager.RegisterAction() to track actions.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Column header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Action ID", EditorStyles.miniLabel, GUILayout.Width(170));
            EditorGUILayout.LabelField("In Buffer", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Consumed", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            DrawSeparator();

            _scrollActions = EditorGUILayout.BeginScrollView(_scrollActions, GUILayout.MaxHeight(130));
            foreach (var actionId in _registeredActions)
                DrawActionRow(actionId);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawActionRow(string actionId)
        {
            // Determine buffer state for this action (most recent entry wins)
            bool inBuffer = false;
            bool consumed = false;
            if (_buffer != null)
            {
                for (int i = _buffer.Count - 1; i >= 0; i--)
                {
                    if (_buffer[i].ActionId != actionId) continue;
                    inBuffer = true;
                    consumed = _buffer[i].IsConsumed;
                    break;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(actionId, GUILayout.Width(170));
            EditorGUILayout.LabelField(inBuffer ? "Yes" : "No", inBuffer ? _greenLabel : _grayLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField(consumed ? "Yes" : "No", consumed ? _yellowLabel : _grayLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
        }

        // ── Section B — Input Buffer ──────────────────────────────────────────

        private void DrawSectionB()
        {
            int count = _buffer?.Count ?? 0;
            EditorGUILayout.LabelField($"Section B — Input Buffer  ({count})", _headerStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (count == 0)
            {
                EditorGUILayout.HelpBox("Buffer is empty.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Column header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Age (s)", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Action ID", EditorStyles.miniLabel, GUILayout.Width(170));
            EditorGUILayout.LabelField("Status", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            DrawSeparator();

            _scrollBuffer = EditorGUILayout.BeginScrollView(_scrollBuffer, GUILayout.MaxHeight(160));
            // Newest first
            for (int i = _buffer.Count - 1; i >= 0; i--)
                DrawBufferEntry(_buffer[i]);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawBufferEntry(BufferedInput entry)
        {
            float age = _currentTime - entry.Timestamp;
            bool consumed = entry.IsConsumed;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{age:F2}s", GUILayout.Width(60));
            EditorGUILayout.LabelField(entry.ActionId, consumed ? _grayLabel : _whiteLabel, GUILayout.Width(170));
            EditorGUILayout.LabelField(consumed ? "Consumed" : "Available", consumed ? _yellowLabel : _greenLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        // ── Combo Status ──────────────────────────────────────────────────────

        private void DrawComboStatus()
        {
            EditorGUILayout.LabelField("Combo Status", _headerStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ComboSystem is not an IGameService — instances are created per-feature
            // and are not registered with App. App.Get<ComboSystem>() returns null.
            EditorGUILayout.HelpBox(
                "ComboSystem instances are created per-feature (not registered as services). " +
                "To inspect combo progress, expose a public accessor on the MonoBehaviour that " +
                "owns your ComboSystem and add a custom inspector or reference it here.",
                MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_headerStyle != null) return;
            _headerStyle = new GUIStyle(EditorStyles.boldLabel);
            _greenLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
            _grayLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };
            _yellowLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } };
            _whiteLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white } };
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f));
        }
    }
}
