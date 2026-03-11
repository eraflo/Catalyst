using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Features.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Eraflo.Catalyst.Editor.Networking
{
    /// <summary>
    /// Editor window to monitor real-time network diagnostics.
    /// Open via Tools > Catalyst > Network Debugger.
    /// </summary>
    public class NetworkDebuggerWindow : EditorWindow
    {
        // ── Toolbar ─────────────────────────────────────────────────────────
        private bool _autoRefresh = true;

        // ── Refresh ──────────────────────────────────────────────────────────
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.25;

        // ── Cached network state ─────────────────────────────────────────────
        private bool _isConnected;
        private ulong _localClientId;
        private bool _isServer;

        // ── Cached diagnostics ───────────────────────────────────────────────
        private float _rtt;
        private float _packetLoss;
        private float _bandwidthIn;   // KB/s (as reported by NetworkDiagnostics)
        private float _bandwidthOut;  // KB/s

        // ── Simulation ───────────────────────────────────────────────────────
        private bool _isSimulated;
        private int _simulatedLatencyMs;
        private float _simulatedPacketLossPercent;
        private int _simulatedJitterMs;

        // ── Connection counter (tracked via events) ──────────────────────────
        private int _connectionCount;
        private bool _eventsSubscribed;

        // ── RTT sparkline ────────────────────────────────────────────────────
        private const int RTT_HISTORY_SIZE = 60;
        private readonly Queue<float> _rttHistory = new Queue<float>();

        // ── Scroll ───────────────────────────────────────────────────────────
        private Vector2 _scrollPos;

        // ── Cached styles (lazy-init in OnGUI) ──────────────────────────────
        private GUIStyle _greenLabel;
        private GUIStyle _redLabel;
        private GUIStyle _yellowLabel;

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Catalyst/Network Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<NetworkDebuggerWindow>("Network Debugger");
            window.minSize = new Vector2(400, 320);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnsubscribeFromNetworkEvents();
        }

        // ── Play-mode lifecycle ───────────────────────────────────────────────

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _connectionCount = 0;
                _rttHistory.Clear();
                ResetMetrics();
                SubscribeToNetworkEvents();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                UnsubscribeFromNetworkEvents();
                _connectionCount = 0;
                ResetMetrics();
            }
        }

        private void SubscribeToNetworkEvents()
        {
            if (_eventsSubscribed) return;
            var nm = App.Get<NetworkManager>();
            if (nm == null) return;
            nm.OnClientConnected += OnClientConnected;
            nm.OnClientDisconnected += OnClientDisconnected;
            _eventsSubscribed = true;
        }

        private void UnsubscribeFromNetworkEvents()
        {
            if (!_eventsSubscribed) return;
            var nm = App.Get<NetworkManager>();
            if (nm != null)
            {
                nm.OnClientConnected -= OnClientConnected;
                nm.OnClientDisconnected -= OnClientDisconnected;
            }
            _eventsSubscribed = false;
        }

        private void OnClientConnected(ulong clientId) => _connectionCount++;
        private void OnClientDisconnected(ulong clientId) => _connectionCount = Mathf.Max(0, _connectionCount - 1);

        private void ResetMetrics()
        {
            _isConnected = false;
            _localClientId = 0;
            _isServer = false;
            _rtt = 0f;
            _packetLoss = 0f;
            _bandwidthIn = 0f;
            _bandwidthOut = 0f;
            _isSimulated = false;
            _simulatedLatencyMs = 0;
            _simulatedPacketLossPercent = 0f;
            _simulatedJitterMs = 0;
        }

        // ── Polling ───────────────────────────────────────────────────────────

        private void OnEditorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < REFRESH_INTERVAL) return;
            _lastRefreshTime = EditorApplication.timeSinceStartup;

            // Lazy subscribe in case the window was open before play mode started
            if (!_eventsSubscribed) SubscribeToNetworkEvents();

            var nm = App.Get<NetworkManager>();
            var diag = App.Get<NetworkDiagnostics>();

            _isConnected = nm?.IsConnected ?? false;
            _localClientId = nm?.LocalClientId ?? 0;
            _isServer = nm?.IsServer ?? false;

            if (diag != null)
            {
                diag.UpdateMetrics();
                _rtt = diag.RTT;
                _packetLoss = diag.PacketLoss;
                _bandwidthIn = diag.BandwidthIn;
                _bandwidthOut = diag.BandwidthOut;
                _isSimulated = diag.IsSimulationActive;
                _simulatedLatencyMs = diag.SimulatedLatencyMs;
                _simulatedPacketLossPercent = diag.SimulatedPacketLossPercent;
                _simulatedJitterMs = diag.SimulatedJitterMs;

                // Accumulate RTT sparkline sample
                if (_rttHistory.Count >= RTT_HISTORY_SIZE)
                    _rttHistory.Dequeue();
                _rttHistory.Enqueue(_rtt);
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
                EditorGUILayout.HelpBox("Enter play mode to debug network.", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawConnectionStatus();
            EditorGUILayout.Space(4);
            DrawMetrics();
            EditorGUILayout.Space(4);
            DrawSparkline();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto-Refresh", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            if (Application.isPlaying && GUILayout.Button("Copy Stats", EditorStyles.toolbarButton))
                GUIUtility.systemCopyBuffer = BuildStatsString();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConnectionStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Row 1: connection state + simulation badge
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                _isConnected ? "Connected" : "Disconnected",
                _isConnected ? _greenLabel : _redLabel,
                GUILayout.Width(100));

            if (_isServer)
                EditorGUILayout.LabelField("Server", EditorStyles.miniLabel, GUILayout.Width(50));

            if (_isSimulated)
            {
                EditorGUILayout.LabelField(
                    $"SIMULATED  {_simulatedLatencyMs}ms / {_simulatedPacketLossPercent:F1}% loss / {_simulatedJitterMs}ms jitter",
                    _yellowLabel);
            }
            EditorGUILayout.EndHorizontal();

            // Row 2: IDs / counts
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Local Client ID: {_localClientId}", GUILayout.Width(160));
            EditorGUILayout.LabelField($"Active Connections: {_connectionCount}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawMetrics()
        {
            EditorGUILayout.LabelField("Metrics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // RTT
            DrawMetricRow("RTT", $"{_rtt:F1} ms", RttColor(_rtt));

            // Packet loss
            DrawMetricRow("Packet Loss", $"{_packetLoss:F1}%", LossColor(_packetLoss));

            // Bandwidth
            DrawMetricRow("Bandwidth In", FormatBandwidth(_bandwidthIn), Color.white);
            DrawMetricRow("Bandwidth Out", FormatBandwidth(_bandwidthOut), Color.white);

            EditorGUILayout.EndVertical();
        }

        private void DrawMetricRow(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(110));
            var style = new GUIStyle(EditorStyles.label) { normal = { textColor = valueColor } };
            EditorGUILayout.LabelField(value, style);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSparkline()
        {
            EditorGUILayout.LabelField("RTT History (last 60 samples)", EditorStyles.boldLabel);

            // Fixed 200 x 30 area, left-aligned
            var outerRect = EditorGUILayout.GetControlRect(false, 32);
            var rect = new Rect(outerRect.x, outerRect.y, 200f, 30f);

            // Background
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

            if (_rttHistory.Count == 0)
            {
                var noDataStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                EditorGUI.LabelField(rect, "No data", noDataStyle);
                return;
            }

            var samples = _rttHistory.ToArray();

            float maxRtt = 1f;
            foreach (var s in samples)
                if (s > maxRtt) maxRtt = s;

            float barWidth = rect.width / RTT_HISTORY_SIZE;
            int startOffset = RTT_HISTORY_SIZE - samples.Length;

            for (int i = 0; i < samples.Length; i++)
            {
                float normalized = Mathf.Clamp01(samples[i] / maxRtt);
                float barHeight = normalized * rect.height;
                float x = rect.x + (startOffset + i) * barWidth;
                float y = rect.yMax - barHeight;
                EditorGUI.DrawRect(
                    new Rect(x, y, Mathf.Max(1f, barWidth - 1f), barHeight),
                    RttColor(samples[i]));
            }

            // Max label
            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
            EditorGUI.LabelField(new Rect(rect.xMax + 4f, rect.y, 60f, 16f), $"max {maxRtt:F0}ms", labelStyle);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_greenLabel != null) return;
            _greenLabel = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.green } };
            _redLabel = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.red } };
            _yellowLabel = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.yellow } };
        }

        private static Color RttColor(float rtt) =>
            rtt < 50f ? Color.green : rtt < 150f ? Color.yellow : Color.red;

        private static Color LossColor(float loss) =>
            loss < 1f ? Color.green : loss < 5f ? Color.yellow : Color.red;

        /// <summary>
        /// Formats a KB/s value as "X.X KB/s" or "X.XX MB/s".
        /// NetworkDiagnostics already reports bandwidth in KB/s.
        /// </summary>
        private static string FormatBandwidth(float kbPerSec)
        {
            if (kbPerSec >= 1024f)
                return $"{kbPerSec / 1024f:F2} MB/s";
            return $"{kbPerSec:F1} KB/s";
        }

        private string BuildStatsString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== Network Stats ({System.DateTime.Now:HH:mm:ss}) ===");
            sb.AppendLine($"Status      : {(_isConnected ? "Connected" : "Disconnected")}{(_isServer ? " (Server)" : "")}");
            sb.AppendLine($"Client ID   : {_localClientId}");
            sb.AppendLine($"Connections : {_connectionCount}");
            sb.AppendLine($"RTT         : {_rtt:F1} ms");
            sb.AppendLine($"Packet Loss : {_packetLoss:F1}%");
            sb.AppendLine($"Bandwidth In: {FormatBandwidth(_bandwidthIn)}");
            sb.AppendLine($"Bandwidth Out: {FormatBandwidth(_bandwidthOut)}");
            if (_isSimulated)
                sb.AppendLine($"[SIMULATED: {_simulatedLatencyMs}ms latency, {_simulatedPacketLossPercent:F1}% loss, {_simulatedJitterMs}ms jitter]");
            return sb.ToString();
        }
    }
}
