using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.Command;
using Eraflo.Catalyst.Events;
using System.Collections.Generic;
using System.Text;

namespace Eraflo.Catalyst.Editor.Command
{
    /// <summary>
    /// Combined editor window for command history monitoring and replay inspection.
    /// Open via Tools > Catalyst > Command Inspector.
    ///
    /// Tab 0 — Command History : live feed of executed / undone / redone commands.
    /// Tab 1 — Replay Inspector : controls a ReplayRecorder and optional ReplayPlayer.
    /// </summary>
    public class CommandInspectorWindow : EditorWindow
    {
        // ─────────────────────────────────────────────────────────────────────
        // Private helper MonoBehaviour used as coroutine host for ReplayPlayer.
        // Placed in an editor assembly; AddComponent works fine during play mode.
        // ─────────────────────────────────────────────────────────────────────
        [UnityEngine.AddComponentMenu("")]
        private class ReplayCoroutineHost : MonoBehaviour { }

        // ── Tab selection ─────────────────────────────────────────────────────
        private int _selectedTab;
        private readonly string[] _tabLabels = { "Command History", "Replay Inspector" };

        // ── Shared state ──────────────────────────────────────────────────────
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.25;

        // ── Cached styles (lazy-init) ────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _whiteLabel;
        private GUIStyle _grayLabel;
        private GUIStyle _cyanLabel;
        private GUIStyle _greenLabel;
        private GUIStyle _redLabel;
        private GUIStyle _yellowLabel;

        // ─────────────────────────────────────────────────────────────────────
        // TAB 0 — Command History
        // ─────────────────────────────────────────────────────────────────────

        private enum CommandStatus { Executed, Undone, Redone }

        private struct CommandHistoryEntry
        {
            public float Timestamp;
            public string TypeName;
            public CommandStatus Status;
            public string DataPreview; // first 80 chars of command.ToString()
        }

        private const int MAX_HISTORY = 200;
        private readonly List<CommandHistoryEntry> _history = new List<CommandHistoryEntry>(MAX_HISTORY);
        private Vector2 _historyScroll;

        private bool _eventBusSubscribed;
        private int _cachedUndoCount;
        private int _cachedRedoCount;

        // Cached delegates to allow clean unsubscription
        private System.Action<CommandExecutedEvent> _onExecuted;
        private System.Action<CommandUndoneEvent> _onUndone;
        private System.Action<CommandRedoneEvent> _onRedone;

        // ─────────────────────────────────────────────────────────────────────
        // TAB 1 — Replay Inspector
        // ─────────────────────────────────────────────────────────────────────

        private ReplayRecorder _recorder;
        private ReplayPlayer _player;
        private GameObject _playerHostGO;

        private Vector2 _trackScroll;

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Catalyst/Command Inspector")]
        public static void ShowWindow()
        {
            var window = GetWindow<CommandInspectorWindow>("Command Inspector");
            window.minSize = new Vector2(420, 380);
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            // Persist delegate references so Unsubscribe can match them
            _onExecuted = OnCommandExecuted;
            _onUndone = OnCommandUndone;
            _onRedone = OnCommandRedone;

            if (Application.isPlaying)
                SubscribeToEventBus();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnsubscribeFromEventBus();
            CleanUpPlayerHost();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Reset history so it begins fresh each play session
                _history.Clear();
                _cachedUndoCount = 0;
                _cachedRedoCount = 0;
                _recorder = null;
                CleanUpPlayerHost();
                SubscribeToEventBus();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                UnsubscribeFromEventBus();
                _recorder = null;
                CleanUpPlayerHost();
            }
        }

        // ── EventBus subscription ─────────────────────────────────────────────

        private void SubscribeToEventBus()
        {
            if (_eventBusSubscribed) return;
            var bus = App.Get<EventBus>();
            if (bus == null) return;
            bus.Subscribe(_onExecuted);
            bus.Subscribe(_onUndone);
            bus.Subscribe(_onRedone);
            _eventBusSubscribed = true;
        }

        private void UnsubscribeFromEventBus()
        {
            if (!_eventBusSubscribed) return;
            var bus = App.Get<EventBus>();
            bus?.Unsubscribe(_onExecuted);
            bus?.Unsubscribe(_onUndone);
            bus?.Unsubscribe(_onRedone);
            _eventBusSubscribed = false;
        }

        // ── EventBus callbacks (called on main thread) ─────────────────────

        private void OnCommandExecuted(CommandExecutedEvent evt)
        {
            AddHistoryEntry(evt.Command, evt.Timestamp, CommandStatus.Executed);
        }

        private void OnCommandUndone(CommandUndoneEvent evt)
        {
            AddHistoryEntry(evt.Command, evt.Timestamp, CommandStatus.Undone);
        }

        private void OnCommandRedone(CommandRedoneEvent evt)
        {
            AddHistoryEntry(evt.Command, evt.Timestamp, CommandStatus.Redone);
        }

        private void AddHistoryEntry(ICommand command, float timestamp, CommandStatus status)
        {
            if (_history.Count >= MAX_HISTORY)
                _history.RemoveAt(0);

            string preview = command != null ? Truncate(command.ToString(), 80) : "(null)";

            _history.Add(new CommandHistoryEntry
            {
                Timestamp = timestamp,
                TypeName = command?.GetType().Name ?? "Unknown",
                Status = status,
                DataPreview = preview
            });

            Repaint();
        }

        // ── Polling ───────────────────────────────────────────────────────────

        private void OnEditorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < REFRESH_INTERVAL) return;
            _lastRefreshTime = EditorApplication.timeSinceStartup;

            // Lazy subscribe (window may have opened before App was ready)
            if (!_eventBusSubscribed) SubscribeToEventBus();

            var mgr = App.Get<CommandManager>();
            if (mgr != null)
            {
                _cachedUndoCount = mgr.UndoCount;
                _cachedRedoCount = mgr.RedoCount;
            }

            Repaint();
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();

            // Tab bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabLabels, EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            if (_selectedTab == 0)
                DrawHistoryTab();
            else
                DrawReplayTab();
        }

        // ─────────────────────────────────────────────────────────────────────
        // TAB 0 — Command History
        // ─────────────────────────────────────────────────────────────────────

        private void DrawHistoryTab()
        {
            DrawHistoryToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter play mode to record command history.", MessageType.Info);
                return;
            }

            DrawHistoryStats();
            EditorGUILayout.Space(4);
            DrawHistoryList();
        }

        private void DrawHistoryToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto-Refresh", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            if (Application.isPlaying)
            {
                if (GUILayout.Button("Clear History", EditorStyles.toolbarButton))
                    _history.Clear();
                GUI.enabled = _cachedUndoCount > 0;
                if (GUILayout.Button("Undo", EditorStyles.toolbarButton))
                    _ = App.Get<CommandManager>()?.Undo();
                GUI.enabled = _cachedRedoCount > 0;
                if (GUILayout.Button("Redo", EditorStyles.toolbarButton))
                    _ = App.Get<CommandManager>()?.Redo();
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHistoryStats()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Entries: {_history.Count} / {MAX_HISTORY}", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField($"Undo stack: {_cachedUndoCount}", GUILayout.Width(110));
            EditorGUILayout.LabelField($"Redo stack: {_cachedRedoCount}");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHistoryList()
        {
            EditorGUILayout.LabelField("Command History", _headerStyle);

            if (_history.Count == 0)
            {
                EditorGUILayout.HelpBox("No commands recorded yet.", MessageType.Info);
                return;
            }

            // Column header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time", EditorStyles.miniLabel, GUILayout.Width(55));
            EditorGUILayout.LabelField("Type", EditorStyles.miniLabel, GUILayout.Width(130));
            EditorGUILayout.LabelField("Status", EditorStyles.miniLabel, GUILayout.Width(65));
            EditorGUILayout.LabelField("Preview", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            DrawSeparator();

            _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll);
            // Newest first
            for (int i = _history.Count - 1; i >= 0; i--)
                DrawHistoryEntry(_history[i]);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHistoryEntry(CommandHistoryEntry entry)
        {
            GUIStyle labelStyle = entry.Status switch
            {
                CommandStatus.Undone => _grayLabel,
                CommandStatus.Redone => _cyanLabel,
                _ => _whiteLabel,
            };

            string statusText = entry.Status switch
            {
                CommandStatus.Undone => "Undone",
                CommandStatus.Redone => "Redone",
                _ => "Executed",
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{entry.Timestamp:F2}s", labelStyle, GUILayout.Width(55));
            EditorGUILayout.LabelField(entry.TypeName, labelStyle, GUILayout.Width(130));
            EditorGUILayout.LabelField(statusText, labelStyle, GUILayout.Width(65));
            EditorGUILayout.LabelField(entry.DataPreview, labelStyle);
            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────────────────────────────
        // TAB 1 — Replay Inspector
        // ─────────────────────────────────────────────────────────────────────

        private void DrawReplayTab()
        {
            DrawReplayToolbar();

            if (!Application.isPlaying)
            {
                DrawReplayEditModeContent();
                return;
            }

            DrawReplayState();
            EditorGUILayout.Space(4);
            DrawTrackFrameList();
        }

        private void DrawReplayToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Replay", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();

            if (Application.isPlaying)
            {
                // Record / Stop
                bool isRecording = _recorder?.IsRecording ?? false;
                GUI.enabled = !isRecording;
                if (GUILayout.Button("Record", EditorStyles.toolbarButton))
                    StartRecording();
                GUI.enabled = isRecording;
                if (GUILayout.Button("Stop", EditorStyles.toolbarButton))
                    StopRecording();
                GUI.enabled = true;

                EditorGUILayout.Space(6);

                // Playback
                bool hasTrack = _recorder?.Track != null && _recorder.Track.Frames.Count > 0;
                bool isPlaying = _player?.IsPlaying ?? false;

                GUI.enabled = hasTrack && !isPlaying;
                if (GUILayout.Button("Play From Start", EditorStyles.toolbarButton))
                    StartPlayback();

                GUI.enabled = hasTrack;
                if (GUILayout.Button("Step Fwd", EditorStyles.toolbarButton))
                    StepForward();

                GUI.enabled = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawReplayEditModeContent()
        {
            EditorGUILayout.HelpBox(
                "Replay controls require Play Mode.\n\n" +
                "Note: ReplayTrack is a plain C# class, not a ScriptableObject, so it cannot be " +
                "selected from the Project window. Record a track during play mode to inspect its frames here.",
                MessageType.Info);
        }

        private void DrawReplayState()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Recorder state
            string recState = "Idle";
            GUIStyle recStyle = _grayLabel;
            if (_recorder != null && _recorder.IsRecording)
            {
                recState = "Recording";
                recStyle = _redLabel;
            }

            // Player state
            string playState = "Idle";
            GUIStyle playStyle = _grayLabel;
            if (_player != null && _player.IsPlaying)
            {
                playState = "Playing";
                playStyle = _greenLabel;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Recorder:", GUILayout.Width(70));
            EditorGUILayout.LabelField(recState, recStyle, GUILayout.Width(80));
            EditorGUILayout.LabelField("Player:", GUILayout.Width(55));
            EditorGUILayout.LabelField(playState, playStyle, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            // Track summary
            int frameCount = _recorder?.Track?.Frames.Count ?? 0;
            EditorGUILayout.LabelField($"Track: \"{_recorder?.Track?.Name ?? "—"}\"   Frames: {frameCount}");

            // Playback progress slider
            if (_player != null && _player.TotalFrames > 0)
            {
                EditorGUILayout.Space(2);
                float progress = _player.TotalFrames > 0
                    ? (float)_player.CurrentFrameIndex / _player.TotalFrames
                    : 0f;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Slider(
                    $"Frame {_player.CurrentFrameIndex} / {_player.TotalFrames}",
                    progress, 0f, 1f);
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTrackFrameList()
        {
            var track = _recorder?.Track;
            int count = track?.Frames.Count ?? 0;

            EditorGUILayout.LabelField($"Track Frames ({count})", _headerStyle);

            if (count == 0)
            {
                EditorGUILayout.HelpBox("No frames recorded yet. Press Record and execute commands.", MessageType.Info);
                return;
            }

            // Column header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("#", EditorStyles.miniLabel, GUILayout.Width(35));
            EditorGUILayout.LabelField("Timestamp", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Command Type", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            DrawSeparator();

            _trackScroll = EditorGUILayout.BeginScrollView(_trackScroll);
            for (int i = 0; i < track.Frames.Count; i++)
            {
                var frame = track.Frames[i];
                bool isCurrent = _player != null && i == _player.CurrentFrameIndex;

                // Highlight the frame that is about to execute during playback
                if (isCurrent)
                {
                    var highlightRect = EditorGUILayout.GetControlRect(false, 18f);
                    EditorGUI.DrawRect(highlightRect, new Color(0.25f, 0.40f, 0.25f));
                    GUI.Label(new Rect(highlightRect.x + 35f + 80f + 4f, highlightRect.y, highlightRect.width, highlightRect.height),
                        ExtractShortTypeName(frame.CommandType), _greenLabel);
                    GUI.Label(new Rect(highlightRect.x, highlightRect.y, 35f, highlightRect.height), i.ToString(), _greenLabel);
                    GUI.Label(new Rect(highlightRect.x + 35f, highlightRect.y, 80f, highlightRect.height), $"{frame.Timestamp:F3}s", _greenLabel);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(35));
                    EditorGUILayout.LabelField($"{frame.Timestamp:F3}s", GUILayout.Width(80));
                    EditorGUILayout.LabelField(ExtractShortTypeName(frame.CommandType));
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // ── Replay controls ───────────────────────────────────────────────────

        private void StartRecording()
        {
            // Stop any previous recorder
            _recorder?.Stop();

            _recorder = new ReplayRecorder("EditorCapture");
            _recorder.Start();
        }

        private void StopRecording()
        {
            _recorder?.Stop();
        }

        private void StartPlayback()
        {
            if (_recorder == null || _recorder.Track == null || _recorder.Track.Frames.Count == 0)
            {
                Debug.LogWarning("[CommandInspector] No recorded track to play back.");
                return;
            }

            // Destroy previous host
            CleanUpPlayerHost();

            // Create a minimal coroutine host in the scene (valid during play mode)
            _playerHostGO = new GameObject("[ReplayCoroutineHost]");
            Object.DontDestroyOnLoad(_playerHostGO);
            var host = _playerHostGO.AddComponent<ReplayCoroutineHost>();

            _player = new ReplayPlayer(_recorder.Track, host);
            _player.OnPlaybackFinished += OnPlaybackFinished;
            _player.Play();
        }

        private void OnPlaybackFinished()
        {
            // Keep the host alive briefly so the last frame's async work completes,
            // then schedule destruction on the next editor update.
            EditorApplication.delayCall += CleanUpPlayerHost;
        }

        private void StepForward()
        {
            if (_recorder == null || _recorder.Track == null || _recorder.Track.Frames.Count == 0)
            {
                Debug.LogWarning("[CommandInspector] No recorded track to step through.");
                return;
            }

            // If no player exists yet, create a manual step-only player (no coroutine started)
            if (_player == null)
            {
                if (_playerHostGO == null)
                {
                    _playerHostGO = new GameObject("[ReplayCoroutineHost]");
                    Object.DontDestroyOnLoad(_playerHostGO);
                    _playerHostGO.AddComponent<ReplayCoroutineHost>();
                }
                var host = _playerHostGO.GetComponent<ReplayCoroutineHost>();
                _player = new ReplayPlayer(_recorder.Track, host);
            }

            _player.StepForward();
            Repaint();
        }

        private void CleanUpPlayerHost()
        {
            if (_playerHostGO != null)
            {
                Object.DestroyImmediate(_playerHostGO);
                _playerHostGO = null;
            }
            _player = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_headerStyle != null) return;
            _headerStyle = new GUIStyle(EditorStyles.boldLabel);
            _whiteLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white } };
            _grayLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.gray } };
            _cyanLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.cyan } };
            _greenLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
            _redLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
            _yellowLabel = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } };
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f));
        }

        private static string Truncate(string s, int maxLen) =>
            s.Length <= maxLen ? s : s.Substring(0, maxLen) + "...";

        /// <summary>
        /// Extracts just the class name from an assembly-qualified type name.
        /// "My.Namespace.MoveCommand, Assembly, ..." → "MoveCommand"
        /// </summary>
        private static string ExtractShortTypeName(string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName)) return "(unknown)";
            int comma = assemblyQualifiedName.IndexOf(',');
            string full = comma >= 0 ? assemblyQualifiedName.Substring(0, comma) : assemblyQualifiedName;
            int dot = full.LastIndexOf('.');
            return dot >= 0 ? full.Substring(dot + 1) : full;
        }
    }
}
