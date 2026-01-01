using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.Timers;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Editor.Debugging
{
    /// <summary>
    /// Editor window to monitor and debug active timers.
    /// Open via Tools > Unity Import Package > Timer Debugger.
    /// </summary>
    public class TimerDebuggerWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.1; // 100ms
        private List<TimerDebugInfo> _cachedTimers = new List<TimerDebugInfo>();

        [MenuItem("Tools/Eraflo Catalyst/Timer Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<TimerDebuggerWindow>("Timer Debugger");
            window.minSize = new Vector2(350, 250);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying) return;
            
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                _cachedTimers = Timer.GetActiveTimers();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see active timers.", MessageType.Info);
                return;
            }

            DrawStats();
            DrawTimerList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
            
            GUILayout.FlexibleSpace();
            
            if (Application.isPlaying)
            {
                if (GUILayout.Button("Clear All", EditorStyles.toolbarButton))
                {
                    Timer.Clear();
                    _cachedTimers.Clear();
                }
                
                GUI.enabled = false;
                GUILayout.Label(Timer.IsBurstMode ? "Burst" : "Standard", EditorStyles.toolbarButton);
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStats()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Active Timers: {_cachedTimers.Count}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Backend: {(Timer.IsBurstMode ? "Burst" : "Standard")}");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTimerList()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_cachedTimers.Count == 0)
            {
                EditorGUILayout.HelpBox("No active timers.", MessageType.Info);
            }
            else
            {
                foreach (var info in _cachedTimers)
                {
                    DrawTimerEntry(info);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTimerEntry(TimerDebugInfo info)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Row 1: Icon + Type + ID
            EditorGUILayout.BeginHorizontal();
            
            // Status icon
            string icon = info.IsRunning ? "▶" : (info.IsFinished ? "⏹" : "⏸");
            var iconStyle = new GUIStyle(EditorStyles.label) { 
                fontSize = 14,
                normal = { textColor = info.IsRunning ? Color.green : (info.IsFinished ? Color.gray : Color.yellow) }
            };
            GUILayout.Label(icon, iconStyle, GUILayout.Width(20));
            
            // Type name
            EditorGUILayout.LabelField(info.TypeName, EditorStyles.boldLabel, GUILayout.Width(120));
            
            // ID
            EditorGUILayout.LabelField($"ID: {info.Id}", GUILayout.Width(60));
            
            GUILayout.FlexibleSpace();
            
            // Time scale badge
            if (Mathf.Abs(info.TimeScale - 1f) > 0.01f)
            {
                var badge = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.cyan } };
                GUILayout.Label($"x{info.TimeScale:F1}", badge);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Row 2: Progress bar
            var rect = EditorGUILayout.GetControlRect(false, 16);
            float displayProgress = Mathf.Clamp01(info.Progress);
            EditorGUI.ProgressBar(rect, displayProgress, $"{info.CurrentTime:F2}s / {displayProgress:P0}");
            
            EditorGUILayout.EndVertical();
        }
    }
}
