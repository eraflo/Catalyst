using UnityEngine;
using System.Collections.Generic;

namespace Eraflo.UnityImportPackage.Timers.Debugging
{
    /// <summary>
    /// Runtime debug overlay showing all active timers.
    /// Enable via PackageSettings.EnableDebugOverlay.
    /// Toggle with F5 key.
    /// </summary>
    public class TimerDebugger : MonoBehaviour
    {
        [SerializeField] private bool _showOverlay = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F5;
        [SerializeField] private Vector2 _position = new Vector2(10, 10);
        [SerializeField] private Vector2 _size = new Vector2(320, 400);

        private Vector2 _scrollPosition;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _timerBoxStyle;
        private GUIStyle _progressBgStyle;
        private GUIStyle _progressFgStyle;
        
        private List<TimerDebugInfo> _cachedTimers = new List<TimerDebugInfo>();
        private float _lastRefresh;
        private const float REFRESH_INTERVAL = 0.1f;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _showOverlay = !_showOverlay;
            }

            // Refresh timer list periodically
            if (_showOverlay && Time.realtimeSinceStartup - _lastRefresh > REFRESH_INTERVAL)
            {
                _lastRefresh = Time.realtimeSinceStartup;
                _cachedTimers = Timer.GetActiveTimers();
            }
        }

        private void OnGUI()
        {
            if (!_showOverlay || !PackageSettings.Instance.EnableDebugOverlay) return;

            InitializeStyles();

            GUI.Box(new Rect(_position.x, _position.y, _size.x, _size.y), "", _boxStyle);

            GUILayout.BeginArea(new Rect(_position.x + 10, _position.y + 10, _size.x - 20, _size.y - 20));
            
            // Header
            GUILayout.Label("Timer Debugger (F5)", _headerStyle);
            GUILayout.Label($"Active: {_cachedTimers.Count} | {(Timer.IsBurstMode ? "Burst" : "Standard")}", _labelStyle);
            
            // Metrics
            var m = Timer.Metrics;
            GUILayout.Label($"Created: {m.TotalCreated} | Completed: {m.TotalCompleted} | Cancelled: {m.TotalCancelled}", _labelStyle);
            GUILayout.Label($"Update: {m.LastUpdateMs:F2}ms | Peak: {m.PeakActiveCount}", _labelStyle);
            GUILayout.Space(5);

            // Controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                Timer.Clear();
                _cachedTimers.Clear();
            }
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                _cachedTimers = Timer.GetActiveTimers();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Timer list
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            if (_cachedTimers.Count == 0)
            {
                GUILayout.Label("No active timers", _labelStyle);
            }
            else
            {
                foreach (var info in _cachedTimers)
                {
                    DrawTimerEntry(info);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawTimerEntry(TimerDebugInfo info)
        {
            GUILayout.BeginVertical(_timerBoxStyle);
            
            // Row 1: Icon + Type + Status
            GUILayout.BeginHorizontal();
            
            // Status icon with color
            string icon = info.IsRunning ? "▶" : (info.IsFinished ? "✓" : "⏸");
            Color iconColor = info.IsRunning ? Color.green : (info.IsFinished ? Color.gray : Color.yellow);
            GUI.color = iconColor;
            GUILayout.Label(icon, GUILayout.Width(18));
            GUI.color = Color.white;
            
            // Type name
            GUILayout.Label(info.TypeName, _labelStyle, GUILayout.Width(110));
            
            // ID
            GUILayout.Label($"#{info.Id}", _labelStyle, GUILayout.Width(40));
            
            GUILayout.FlexibleSpace();
            
            // Time scale
            if (Mathf.Abs(info.TimeScale - 1f) > 0.01f)
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"x{info.TimeScale:F1}", _labelStyle, GUILayout.Width(35));
                GUI.color = Color.white;
            }
            
            GUILayout.EndHorizontal();
            
            // Row 2: Progress bar
            DrawProgressBar(info.Progress, info.CurrentTime, info.InitialTime);
            
            GUILayout.EndVertical();
            GUILayout.Space(2);
        }

        private void DrawProgressBar(float progress, float current, float initial)
        {
            Rect rect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
            
            // Background
            GUI.Box(rect, "", _progressBgStyle);
            
            // Foreground (progress)
            float clampedProgress = Mathf.Clamp01(progress);
            Rect fgRect = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * clampedProgress, rect.height - 4);
            GUI.Box(fgRect, "", _progressFgStyle);
            
            // Text
            string text = $"{current:F2}s / {clampedProgress:P0}";
            GUI.Label(rect, text, _labelStyle);
        }

        private void InitializeStyles()
        {
            if (_boxStyle != null) return;

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTexture(1, 1, new Color(0.1f, 0.1f, 0.1f, 0.95f));

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 11;
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.alignment = TextAnchor.MiddleLeft;

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 14;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = Color.cyan;

            _timerBoxStyle = new GUIStyle(GUI.skin.box);
            _timerBoxStyle.normal.background = MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            _timerBoxStyle.padding = new RectOffset(5, 5, 3, 3);

            _progressBgStyle = new GUIStyle();
            _progressBgStyle.normal.background = MakeTexture(1, 1, new Color(0.15f, 0.15f, 0.15f, 1f));

            _progressFgStyle = new GUIStyle();
            _progressFgStyle.normal.background = MakeTexture(1, 1, new Color(0.2f, 0.6f, 0.9f, 1f));
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates the debugger if enabled in settings.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (PackageSettings.Instance.EnableDebugOverlay)
            {
                if (FindObjectOfType<TimerDebugger>() == null)
                {
                    var go = new GameObject("TimerDebugger");
                    go.AddComponent<TimerDebugger>();
                }
            }
        }
    }
}
