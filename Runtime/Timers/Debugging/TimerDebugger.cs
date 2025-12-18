using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Timers.Debugging
{
    /// <summary>
    /// Runtime debug overlay for the Timer System.
    /// Shows a list of active timers and their status.
    /// </summary>
    public class TimerDebugger : MonoBehaviour
    {
        private List<Timer> _snapshot = new List<Timer>();
        private Vector2 _scrollPos;
        private bool _expanded = true;
        
        private void Awake() 
        {
            DontDestroyOnLoad(gameObject);
            gameObject.name = "[TimerDebugger]";
        }

        private void OnGUI()
        {
            if (!PackageSettings.Instance.EnableDebugOverlay) return;

            // Box style
            var bgStyle = new GUIStyle(GUI.skin.box);
            bgStyle.normal.background = Texture2D.whiteTexture; // Use default skin mostly
            
            GUILayout.BeginArea(new Rect(10, 10, 250, Screen.height - 20));
            
            GUILayout.BeginVertical("box");
            
            GUILayout.BeginHorizontal();
            var headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label("⏱ Timer System", headerStyle);
            if (GUILayout.Button(_expanded ? "H" : "S", GUILayout.Width(25)))
            {
                _expanded = !_expanded;
            }
            GUILayout.EndHorizontal();

            if (_expanded)
            {
                DrawContent();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawContent()
        {
            _snapshot = TimerManager.GetAllTimers();
            
            GUILayout.Label($"Active: {_snapshot.Count} | Pool: {TimerPool.TotalPooledCount}");
            
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            foreach (var timer in _snapshot)
            {
                if (timer == null) continue;
                DrawTimer(timer);
            }

            GUILayout.EndScrollView();
        }

        private void DrawTimer(Timer timer)
        {
            GUILayout.BeginVertical("box");
            
            // Header: Type and State
            string icon = timer.IsRunning ? "▶" : "⏸";
            Color color = timer.IsRunning ? Color.green : Color.yellow;
            if (timer.IsFinished) { icon = "⏹"; color = Color.gray; }
            
            var oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Label($"{icon} {timer.GetType().Name}");
            GUI.color = oldColor;

            // Progress
            float p = timer.Progress;
            GUILayout.HorizontalSlider(p, 0f, 1f);
            
            // Details
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{timer.CurrentTime:F1}s", GUILayout.Width(60));
            GUILayout.Label($"x{timer.TimeScale:F1}");
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
    }
}
