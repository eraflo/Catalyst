using UnityEngine;
using UnityEngine.UIElements;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Utils
{
    /// <summary>
    /// Reusable behavior for draggable and resizable panels.
    /// Attach to any VisualElement to enable drag/resize functionality.
    /// </summary>
    public class DraggablePanelBehavior
    {
        private readonly VisualElement _panel;
        private readonly VisualElement _header;
        private readonly VisualElement _resizeHandle;
        
        private bool _isDragging;
        private bool _isResizing;
        private Vector2 _dragStartPos;
        private Vector2 _panelStartPos;
        private Vector2 _panelStartSize;
        
        /// <summary>Minimum width constraint for the panel.</summary>
        public float MinWidth { get; set; } = 180f;
        
        /// <summary>Minimum height constraint for the panel.</summary>
        public float MinHeight { get; set; } = 100f;
        
        /// <summary>Maximum width constraint for the panel (0 = no limit).</summary>
        public float MaxWidth { get; set; } = 0f;
        
        /// <summary>Maximum height constraint for the panel (0 = no limit).</summary>
        public float MaxHeight { get; set; } = 0f;
        
        /// <summary>
        /// Creates a new draggable panel behavior.
        /// </summary>
        /// <param name="panel">The panel element to move/resize</param>
        /// <param name="header">The header element that acts as drag handle</param>
        /// <param name="resizeHandle">Optional resize handle element</param>
        public DraggablePanelBehavior(
            VisualElement panel, 
            VisualElement header, 
            VisualElement resizeHandle = null)
        {
            _panel = panel;
            _header = header;
            _resizeHandle = resizeHandle;
            
            RegisterCallbacks();
        }
        
        private void RegisterCallbacks()
        {
            _header.RegisterCallback<MouseDownEvent>(OnHeaderMouseDown);
            _header.RegisterCallback<MouseMoveEvent>(OnHeaderMouseMove);
            _header.RegisterCallback<MouseUpEvent>(OnHeaderMouseUp);
            
            if (_resizeHandle != null)
            {
                _resizeHandle.RegisterCallback<MouseDownEvent>(OnResizeMouseDown);
                _resizeHandle.RegisterCallback<MouseMoveEvent>(OnResizeMouseMove);
                _resizeHandle.RegisterCallback<MouseUpEvent>(OnResizeMouseUp);
            }
        }
        
        /// <summary>
        /// Unregisters all callbacks. Call this when disposing the panel.
        /// </summary>
        public void Dispose()
        {
            _header.UnregisterCallback<MouseDownEvent>(OnHeaderMouseDown);
            _header.UnregisterCallback<MouseMoveEvent>(OnHeaderMouseMove);
            _header.UnregisterCallback<MouseUpEvent>(OnHeaderMouseUp);
            
            if (_resizeHandle != null)
            {
                _resizeHandle.UnregisterCallback<MouseDownEvent>(OnResizeMouseDown);
                _resizeHandle.UnregisterCallback<MouseMoveEvent>(OnResizeMouseMove);
                _resizeHandle.UnregisterCallback<MouseUpEvent>(OnResizeMouseUp);
            }
        }
        
        #region Header Drag Callbacks
        
        private void OnHeaderMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;
            
            _isDragging = true;
            _dragStartPos = evt.mousePosition;
            _panelStartPos = new Vector2(
                _panel.resolvedStyle.left, 
                _panel.resolvedStyle.top);
            _header.CaptureMouse();
            evt.StopPropagation();
        }
        
        private void OnHeaderMouseMove(MouseMoveEvent evt)
        {
            if (!_isDragging) return;
            
            var delta = evt.mousePosition - _dragStartPos;
            
            float newLeft = _panelStartPos.x + delta.x;
            float newTop = _panelStartPos.y + delta.y;
            
            // Clamp to parent bounds if available
            if (_panel.parent != null)
            {
                var parentBounds = _panel.parent.contentRect;
                newLeft = Mathf.Max(0, Mathf.Min(newLeft, parentBounds.width - _panel.resolvedStyle.width));
                newTop = Mathf.Max(0, Mathf.Min(newTop, parentBounds.height - _panel.resolvedStyle.height));
            }
            
            _panel.style.left = newLeft;
            _panel.style.top = newTop;
            evt.StopPropagation();
        }
        
        private void OnHeaderMouseUp(MouseUpEvent evt)
        {
            if (!_isDragging) return;
            
            _isDragging = false;
            _header.ReleaseMouse();
            evt.StopPropagation();
        }
        
        #endregion
        
        #region Resize Callbacks
        
        private void OnResizeMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;
            
            _isResizing = true;
            _dragStartPos = evt.mousePosition;
            _panelStartSize = new Vector2(
                _panel.resolvedStyle.width, 
                _panel.resolvedStyle.height);
            _resizeHandle.CaptureMouse();
            evt.StopPropagation();
        }
        
        private void OnResizeMouseMove(MouseMoveEvent evt)
        {
            if (!_isResizing) return;
            
            var delta = evt.mousePosition - _dragStartPos;
            
            float newWidth = _panelStartSize.x + delta.x;
            float newHeight = _panelStartSize.y + delta.y;
            
            // Apply constraints
            newWidth = Mathf.Max(MinWidth, newWidth);
            newHeight = Mathf.Max(MinHeight, newHeight);
            
            if (MaxWidth > 0) newWidth = Mathf.Min(MaxWidth, newWidth);
            if (MaxHeight > 0) newHeight = Mathf.Min(MaxHeight, newHeight);
            
            _panel.style.width = newWidth;
            _panel.style.height = newHeight;
            evt.StopPropagation();
        }
        
        private void OnResizeMouseUp(MouseUpEvent evt)
        {
            if (!_isResizing) return;
            
            _isResizing = false;
            _resizeHandle.ReleaseMouse();
            evt.StopPropagation();
        }
        
        #endregion
    }
}
