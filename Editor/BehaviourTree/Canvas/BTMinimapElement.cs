using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Collections.Generic;
using Eraflo.Catalyst.BehaviourTree;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    public class BTMinimapElement : VisualElement
    {
        private BTCanvas _canvas;
        private VisualElement _viewportRect;
        private VisualElement _container;
        
        private List<VisualElement> _nodeDots = new List<VisualElement>();
        
        private const float MapSize = 200f; // Matches CSS width
        private Rect _treeBounds;
        
        // OPTIMIZATION: Cache previous state to detect changes
        private Rect _lastTreeBounds;
        private int _lastNodeCount;
        private bool _isDirty = true;
        
        public BTMinimapElement(BTCanvas canvas)
        {
            _canvas = canvas;
            name = "minimap";
            AddToClassList("bt-minimap");
            
            _container = new VisualElement { name = "minimap-container" };
            _container.style.flexGrow = 1;
            _container.pickingMode = PickingMode.Ignore;
            Add(_container);
            
            _viewportRect = new VisualElement { name = "minimap-viewport" };
            _viewportRect.AddToClassList("bt-minimap-viewport");
            _viewportRect.pickingMode = PickingMode.Ignore;
            Add(_viewportRect);
            
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            
            // Periodically update to ensure sync
            schedule.Execute(() => UpdateMinimap()).Every(100);
        }
        
        private bool _isUpdating;
        public void UpdateMinimap()
        {
            if (_canvas == null || _isUpdating) return;
            
            // OPTIMIZATION: Quick check if full update is needed
            var currentBounds = _canvas.GetTreeBounds();
            var nodes = _canvas.GetNodes();
            int currentNodeCount = nodes.Count;
            
            // If nothing significant changed, just update viewport (lightweight)
            if (!_isDirty && 
                currentBounds == _lastTreeBounds && 
                currentNodeCount == _lastNodeCount)
            {
                UpdateViewportRect();
                return;
            }
            
            _isUpdating = true;
            try {
            
            // Cache current state
            _lastTreeBounds = currentBounds;
            _lastNodeCount = currentNodeCount;
            _isDirty = false;
            
            // 1. Calculate bounds with padding
            _treeBounds = currentBounds;
            float padding = 200f;
            _treeBounds.x -= padding;
            _treeBounds.y -= padding;
            _treeBounds.width += padding * 2;
            _treeBounds.height += padding * 2;
            
            // 2. Update node dots
            UpdateNodeDots();
            
            // 3. Update viewport rect
            UpdateViewportRect();
            } finally {
                _isUpdating = false;
            }
        }
        
        /// <summary>
        /// Marks the minimap as dirty, forcing a full refresh on next update.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }
        
        private void UpdateNodeDots()
        {
            var nodes = _canvas.GetNodes();
            
            // Reuse or create dots
            while (_nodeDots.Count < nodes.Count)
            {
                var dot = new VisualElement();
                dot.AddToClassList("bt-minimap-node");
                dot.pickingMode = PickingMode.Ignore;
                _container.Add(dot);
                _nodeDots.Add(dot);
            }
            while (_nodeDots.Count > nodes.Count)
            {
                var dot = _nodeDots[_nodeDots.Count - 1];
                _container.Remove(dot);
                _nodeDots.RemoveAt(_nodeDots.Count - 1);
            }
            
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i].Node;
                var dot = _nodeDots[i];
                
                // Map position
                float x = MapRange(node.Position.x, _treeBounds.xMin, _treeBounds.xMax, 0, MapSize);
                float y = MapRange(node.Position.y, _treeBounds.yMin, _treeBounds.yMax, 0, 150); // Matches CSS height
                
                dot.style.left = x;
                dot.style.top = y;
                dot.style.width = 10; // Miniature size
                dot.style.height = 6;
                
                // Optional: Color by node type
                if (node is ActionNode) dot.style.backgroundColor = new Color(0.2f, 0.6f, 1f); // Blue
                else if (node is CompositeNode) dot.style.backgroundColor = new Color(0.2f, 0.8f, 0.4f); // Green
                else if (node is DecoratorNode) dot.style.backgroundColor = new Color(1f, 0.6f, 0.2f); // Orange
            }
        }
        
        private void UpdateViewportRect()
        {
            // Current visible area in canvas space
            Rect viewport = _canvas.GetViewport();
            
            float xMin = MapRange(viewport.xMin, _treeBounds.xMin, _treeBounds.xMax, 0, MapSize);
            float yMin = MapRange(viewport.yMin, _treeBounds.yMin, _treeBounds.yMax, 0, 150);
            float xMax = MapRange(viewport.xMax, _treeBounds.xMin, _treeBounds.xMax, 0, MapSize);
            float yMax = MapRange(viewport.yMax, _treeBounds.yMin, _treeBounds.yMax, 0, 150);
            
            _viewportRect.style.left = Mathf.Max(0, xMin);
            _viewportRect.style.top = Mathf.Max(0, yMin);
            _viewportRect.style.width = Mathf.Min(MapSize, xMax - xMin);
            _viewportRect.style.height = Mathf.Min(150, yMax - yMin);
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            NavigateTo(evt.localMousePosition);
            this.CaptureMouse();
            evt.StopPropagation();
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (this.HasMouseCapture())
            {
                NavigateTo(evt.localMousePosition);
                evt.StopPropagation();
            }
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (this.HasMouseCapture())
            {
                this.ReleaseMouse();
                evt.StopPropagation();
            }
        }
        
        private void NavigateTo(Vector2 localPos)
        {
            // Reverse map from local minimap pos to canvas pos
            float canvasX = MapRange(localPos.x, 0, MapSize, _treeBounds.xMin, _treeBounds.xMax);
            float canvasY = MapRange(localPos.y, 0, 150, _treeBounds.yMin, _treeBounds.yMax);
            
            _canvas.CenterOnPosition(new Vector2(canvasX, canvasY));
        }
        
        private float MapRange(float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            if (Mathf.Abs(fromSource - toSource) < 0.001f) return fromTarget;
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }
    }
}
