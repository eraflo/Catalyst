using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Eraflo.Catalyst.HFSM;
using Eraflo.Catalyst.HFSM.Scheduling;

namespace Eraflo.Catalyst.Editor.HFSM
{
    /// <summary>
    /// Editor window to monitor and debug registered HFSM state machines.
    /// Open via Tools > Catalyst > HFSM Debugger.
    /// </summary>
    public class HFSMDebuggerWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.2; // 200ms
        private List<(StateMachine StateMachine, Transform Owner)> _cachedMachines
            = new List<(StateMachine, Transform)>();

        [MenuItem("Tools/Catalyst/HFSM Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<HFSMDebuggerWindow>("HFSM Debugger");
            window.minSize = new Vector2(420, 300);
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
                RefreshData();
                Repaint();
            }
        }

        private void RefreshData()
        {
            var scheduler = App.Get<HFSMSchedulerService>();
            _cachedMachines = scheduler != null
                ? scheduler.GetRegisteredMachines()
                : new List<(StateMachine, Transform)>();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter play mode to debug state machines.", MessageType.Info);
                return;
            }

            DrawSchedulerStats();
            DrawMachineList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Refresh Now", EditorStyles.toolbarButton))
                {
                    RefreshData();
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSchedulerStats()
        {
            var scheduler = App.Get<HFSMSchedulerService>();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Scheduler Stats", EditorStyles.boldLabel);

            if (scheduler == null)
            {
                EditorGUILayout.LabelField("HFSMSchedulerService not found.");
                EditorGUILayout.EndVertical();
                return;
            }

            int total = _cachedMachines.Count;
            int t0 = 0, t1 = 0, t2 = 0;
            foreach (var (sm, owner) in _cachedMachines)
            {
                int tier = scheduler.GetTier(owner);
                if (tier == 0) t0++;
                else if (tier == 1) t1++;
                else t2++;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Total: {total}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"T0: {t0}", GUILayout.Width(50));
            EditorGUILayout.LabelField($"T1: {t1}", GUILayout.Width(50));
            EditorGUILayout.LabelField($"T2: {t2}", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Last Frame: {scheduler.LastFrameMs:F2} ms / {scheduler.MaxMsPerFrame} ms");

            EditorGUILayout.EndVertical();
        }

        private void DrawMachineList()
        {
            var scheduler = App.Get<HFSMSchedulerService>();

            if (scheduler == null)
            {
                EditorGUILayout.HelpBox(
                    "Register state machines with HFSMSchedulerService to see them here.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"State Machines ({_cachedMachines.Count})", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_cachedMachines.Count == 0)
            {
                EditorGUILayout.HelpBox("No registered state machines.", MessageType.Info);
            }
            else
            {
                foreach (var (sm, owner) in _cachedMachines)
                {
                    DrawMachineEntry(sm, owner, scheduler);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawMachineEntry(StateMachine sm, Transform owner, HFSMSchedulerService scheduler)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            // Owner GameObject name — check both C# null and Unity-destroyed null
            bool isOwnerValid = !ReferenceEquals(owner, null) && owner != null;
            string ownerName = isOwnerValid ? owner.gameObject.name : "\u2014";
            EditorGUILayout.LabelField(ownerName, EditorStyles.boldLabel, GUILayout.Width(160));

            GUILayout.FlexibleSpace();

            // Tier badge
            int tier = scheduler.GetTier(owner);
            var tierColor = tier == 0 ? Color.green : (tier == 1 ? Color.yellow : new Color(1f, 0.5f, 0f));
            var tierStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = tierColor },
                fontStyle = FontStyle.Bold
            };
            GUILayout.Label($"T{tier}", tierStyle, GUILayout.Width(24));

            EditorGUILayout.EndHorizontal();

            // Active path breadcrumb
            var activePath = sm.ActivePath;
            if (activePath != null && activePath.Count > 0)
            {
                var parts = new string[activePath.Count];
                for (int i = 0; i < activePath.Count; i++)
                    parts[i] = activePath[i].Name;

                // U+203A = ›
                string breadcrumb = string.Join(" \u203a ", parts);
                EditorGUILayout.LabelField("Path:", breadcrumb);

                // Duration of the current leaf state (deepest in active path)
                float leafDuration = activePath[activePath.Count - 1].StateDuration;
                EditorGUILayout.LabelField("Duration:", $"{leafDuration:F2} s");
            }
            else
            {
                EditorGUILayout.LabelField("Path:", "(no active state)");
            }

            EditorGUILayout.EndVertical();
        }
    }
}
