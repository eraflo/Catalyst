using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Diagnostics
{
    /// <summary>
    /// Lightweight debug overlay showing network metrics.
    /// Add this component to a GameObject to display network stats.
    /// </summary>
    [AddComponentMenu("Catalyst/Networking/Network Diagnostics Overlay")]
    public class NetworkDiagnosticsOverlay : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool _showOverlay = true;
        [SerializeField] private TextAnchor _anchor = TextAnchor.UpperLeft;
        [SerializeField] private int _fontSize = 14;
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Vector2 _padding = new Vector2(10, 10);
        [SerializeField] private Vector2 _margin = new Vector2(10, 10);
        
        [Header("Content")]
        [SerializeField] private bool _showRTT = true;
        [SerializeField] private bool _showPacketLoss = true;
        [SerializeField] private bool _showBandwidth = true;
        [SerializeField] private bool _showSimulationIndicator = true;
        
        [Inject] private NetworkDiagnostics _diagnostics;
        private GUIStyle _textStyle;
        private GUIStyle _boxStyle;
        private Texture2D _backgroundTexture;
        private string _cachedText = "";
        private float _lastUpdateTime;
        
        private void Start()
        {
            // Create background texture
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, _backgroundColor);
            _backgroundTexture.Apply();
        }
        
        private void OnDestroy()
        {
            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
            }
        }
        
        private void OnGUI()
        {
            if (!_showOverlay || _diagnostics == null)
                return;
            
            // Initialize styles
            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _fontSize,
                    alignment = _anchor,
                    wordWrap = false
                };
                _textStyle.normal.textColor = _textColor;
                
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = _backgroundTexture;
            }
            
            // Update text periodically
            if (Time.unscaledTime - _lastUpdateTime > 0.25f)
            {
                _cachedText = BuildDisplayText();
                _lastUpdateTime = Time.unscaledTime;
            }
            
            // Calculate position
            Vector2 size = _textStyle.CalcSize(new GUIContent(_cachedText));
            size += _padding * 2;
            
            Rect rect = CalculateRect(size);
            
            // Draw background
            GUI.Box(rect, GUIContent.none, _boxStyle);
            
            // Draw text
            Rect textRect = new Rect(
                rect.x + _padding.x,
                rect.y + _padding.y,
                rect.width - _padding.x * 2,
                rect.height - _padding.y * 2
            );
            GUI.Label(textRect, _cachedText, _textStyle);
        }
        
        private string BuildDisplayText()
        {
            _diagnostics.UpdateMetrics();
            
            var lines = new System.Text.StringBuilder();
            
            if (_showRTT)
            {
                lines.AppendLine($"RTT: {_diagnostics.RTT:F1}ms");
            }
            
            if (_showPacketLoss)
            {
                lines.AppendLine($"Loss: {_diagnostics.PacketLoss:F1}%");
            }
            
            if (_showBandwidth)
            {
                lines.AppendLine($"In: {_diagnostics.BandwidthIn:F1} KB/s");
                lines.AppendLine($"Out: {_diagnostics.BandwidthOut:F1} KB/s");
            }
            
            if (_showSimulationIndicator && _diagnostics.IsSimulationActive)
            {
                lines.AppendLine($"[SIM: {_diagnostics.SimulatedLatencyMs}ms, {_diagnostics.SimulatedPacketLossPercent}%]");
            }
            
            return lines.ToString().TrimEnd();
        }
        
        private Rect CalculateRect(Vector2 size)
        {
            float x, y;
            
            // Horizontal position
            if (_anchor == TextAnchor.UpperLeft || _anchor == TextAnchor.MiddleLeft || _anchor == TextAnchor.LowerLeft)
            {
                x = _margin.x;
            }
            else if (_anchor == TextAnchor.UpperRight || _anchor == TextAnchor.MiddleRight || _anchor == TextAnchor.LowerRight)
            {
                x = Screen.width - size.x - _margin.x;
            }
            else
            {
                x = (Screen.width - size.x) / 2;
            }
            
            // Vertical position
            if (_anchor == TextAnchor.UpperLeft || _anchor == TextAnchor.UpperCenter || _anchor == TextAnchor.UpperRight)
            {
                y = _margin.y;
            }
            else if (_anchor == TextAnchor.LowerLeft || _anchor == TextAnchor.LowerCenter || _anchor == TextAnchor.LowerRight)
            {
                y = Screen.height - size.y - _margin.y;
            }
            else
            {
                y = (Screen.height - size.y) / 2;
            }
            
            return new Rect(x, y, size.x, size.y);
        }
        
        /// <summary>
        /// Toggles overlay visibility.
        /// </summary>
        public void ToggleOverlay()
        {
            _showOverlay = !_showOverlay;
        }
        
        /// <summary>
        /// Sets overlay visibility.
        /// </summary>
        public void SetVisible(bool visible)
        {
            _showOverlay = visible;
        }
    }
}
