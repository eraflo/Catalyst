using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Eraflo.Catalyst.Pooling;

namespace Eraflo.Catalyst.Editor.Pooling
{
    /// <summary>
    /// Editor window to monitor and debug active pools.
    /// Open via Tools > Unity Import Package > Pool Debugger.
    /// </summary>
    public class PoolDebuggerWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.2;
        private List<PoolDebugInfo> _cachedPools = new List<PoolDebugInfo>();

        [MenuItem("Tools/Eraflo Catalyst/Pool Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<PoolDebuggerWindow>("Pool Debugger");
            window.minSize = new Vector2(350, 300);
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
                _cachedPools = Pool.GetPoolDebugInfo();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see active pools.", MessageType.Info);
                return;
            }

            DrawMetrics();
            DrawPoolList();
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
                    Pool.ClearAll();
                    _cachedPools.Clear();
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    _cachedPools = Pool.GetPoolDebugInfo();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMetrics()
        {
            var metrics = Pool.Metrics;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pool Metrics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Spawned: {metrics.TotalSpawned}");
            EditorGUILayout.LabelField($"Despawned: {metrics.TotalDespawned}");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Active: {metrics.ActiveCount}");
            EditorGUILayout.LabelField($"Peak: {metrics.PeakActiveCount}");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPoolList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Active Pools ({_cachedPools.Count})", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_cachedPools.Count == 0)
            {
                EditorGUILayout.HelpBox("No active pools.", MessageType.Info);
            }
            else
            {
                foreach (var pool in _cachedPools)
                {
                    DrawPoolEntry(pool);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPoolEntry(PoolDebugInfo info)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            
            // Pool type icon
            string icon = info.Type == PoolType.Prefab ? "📦" : "🔷";
            GUILayout.Label(icon, GUILayout.Width(20));

            // Pool name
            EditorGUILayout.LabelField(info.PoolName, EditorStyles.boldLabel, GUILayout.Width(150));

            // Type
            EditorGUILayout.LabelField(info.Type.ToString(), GUILayout.Width(60));

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            // Counts
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Active: {info.ActiveCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"Available: {info.AvailableCount}", GUILayout.Width(100));
            
            // Progress bar showing active ratio
            float total = info.ActiveCount + info.AvailableCount;
            if (total > 0)
            {
                float ratio = info.ActiveCount / total;
                var rect = EditorGUILayout.GetControlRect(false, 16);
                EditorGUI.ProgressBar(rect, ratio, $"{ratio:P0} in use");
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
