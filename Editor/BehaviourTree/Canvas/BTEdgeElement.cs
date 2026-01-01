using UnityEngine;
using UnityEngine.UIElements;
using Eraflo.Catalyst.Editor.BehaviourTree.Utils;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    /// <summary>
    /// Custom edge element with arrow.
    /// Draws a curved line from parent output port to child input.
    /// </summary>
    public class BTEdgeElement : VisualElement
    {
        public BTNodeElement FromNode { get; private set; }
        public BTNodeElement ToNode { get; private set; }
        public bool IsSelected { get; private set; }
        public System.Action<BTEdgeElement> OnSelected;
        
        private Color _edgeColor = new Color(0.5f, 0.5f, 0.5f);
        private const float EdgeWidth = 2f;
        private const float ArrowSize = 8f;
        
        private Label _indexLabel;
        
        public BTEdgeElement(BTNodeElement from, BTNodeElement to)
        {
            FromNode = from;
            ToNode = to;
            
            name = "bt-edge";
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            pickingMode = PickingMode.Position;
            
            // Index label
            _indexLabel = new Label("") { name = "edge-index" };
            _indexLabel.AddToClassList("edge-index");
            _indexLabel.style.position = Position.Absolute;
            _indexLabel.style.display = DisplayStyle.None; // Hide by default
            _indexLabel.pickingMode = PickingMode.Ignore;
            Add(_indexLabel);
            
            generateVisualContent += OnGenerateVisualContent;
            
            // Update when geometry changes
            RegisterCallback<GeometryChangedEvent>(evt => MarkDirtyRepaint());
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            
            // Listen to geometry changes in the nodes to update edge position
            RegisterNodeCallbacks();
        }

        private void RegisterNodeCallbacks()
        {
            if (FromNode != null)
                FromNode.RegisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
            if (ToNode != null)
                ToNode.RegisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
        }

        private void OnNodeGeometryChanged(GeometryChangedEvent evt)
        {
            UpdatePath();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                OnSelected?.Invoke(this);
                evt.StopPropagation();
            }
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            _edgeColor = selected ? new Color(0.2f, 0.6f, 1f) : new Color(0.5f, 0.5f, 0.5f);
            MarkDirtyRepaint();
        }
        
        public void UpdatePath()
        {
            if (FromNode == null || ToNode == null || parent == null) return;
            
            var startPos = FromNode.GetOutputPortCenter();
            var endPos = ToNode.GetInputCenter();
            
            var (xMin, yMin, width, height) = BezierUtils.GetCurveBounds(startPos, endPos);
            
            style.left = xMin;
            style.top = yMin;
            style.width = width;
            style.height = height;
            
            // Update index label position (middle of the curve)
            if (_indexLabel.resolvedStyle.display == DisplayStyle.Flex)
            {
                var (cp1, cp2) = BezierUtils.GetVerticalControlPoints(startPos, endPos);
                var center = BezierUtils.GetBezierPoint(startPos, cp1, cp2, endPos, 0.5f);
                
                // Position relative to edge element
                var localCenter = center - new Vector2(xMin, yMin);
                float labelWidth = _indexLabel.layout.width > 0 ? _indexLabel.layout.width : 16f;
                float labelHeight = _indexLabel.layout.height > 0 ? _indexLabel.layout.height : 16f;
                _indexLabel.style.left = localCenter.x - (labelWidth / 2);
                _indexLabel.style.top = localCenter.y - (labelHeight / 2);
            }
            
            MarkDirtyRepaint();
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (FromNode == null || ToNode == null) return false;
            
            // Convert local back to parent space for calculations
            Vector2 worldPos = localPoint + new Vector2(resolvedStyle.left, resolvedStyle.top);
            
            var startPos = FromNode.GetOutputPortCenter();
            var endPos = ToNode.GetInputCenter();
            
            var (cp1, cp2) = BezierUtils.GetVerticalControlPoints(startPos, endPos);
            
            return BezierUtils.IsPointNearCurve(worldPos, startPos, cp1, cp2, endPos, 400f);
        }

        // Bezier methods moved to BezierUtils
        
        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (FromNode == null || ToNode == null) return;
            
            // Positions relative to THIS element
            Vector2 offset = new Vector2(resolvedStyle.left, resolvedStyle.top);
            var startPos = FromNode.GetOutputPortCenter() - offset;
            var endPos = ToNode.GetInputCenter() - offset;
            
            var painter = ctx.painter2D;
            painter.strokeColor = _edgeColor;
            painter.lineWidth = EdgeWidth;
            painter.lineCap = LineCap.Round;
            
            // Draw curved line
            painter.BeginPath();
            painter.MoveTo(startPos);
            
            var (cp1, cp2) = BezierUtils.GetVerticalControlPoints(startPos, endPos);
            
            painter.BezierCurveTo(cp1, cp2, endPos);
            painter.Stroke();
            
            // Draw arrow
            DrawArrow(painter, endPos);
        }
        
        private void DrawArrow(Painter2D painter, Vector2 tip)
        {
            painter.fillColor = _edgeColor;
            
            // Arrow pointing down
            var left = new Vector2(tip.x - ArrowSize / 2, tip.y - ArrowSize);
            var right = new Vector2(tip.x + ArrowSize / 2, tip.y - ArrowSize);
            
            painter.BeginPath();
            painter.MoveTo(tip);
            painter.LineTo(left);
            painter.LineTo(right);
            painter.ClosePath();
            painter.Fill();
        }
        
        public void SetColor(Color color)
        {
            _edgeColor = color;
            MarkDirtyRepaint();
        }

        public void SetIndex(int index)
        {
            if (index < 0)
            {
                _indexLabel.style.display = DisplayStyle.None;
            }
            else
            {
                _indexLabel.text = (index + 1).ToString(); // 1-based for users
                _indexLabel.style.display = DisplayStyle.Flex;
                UpdatePath();
            }
        }

        public void ClearCallbacks()
        {
            if (FromNode != null)
                FromNode.UnregisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
            if (ToNode != null)
                ToNode.UnregisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
        }

        public void UpdateDebugState()
        {
            if (ToNode == null) return;
            
            bool isRunning = ToNode.ClassListContains("running");
            bool isSuccess = ToNode.ClassListContains("success");
            bool isFailure = ToNode.ClassListContains("failure");
            bool isActive = isRunning || isSuccess || isFailure;
            
            if (isActive)
            {
                if (isRunning) _edgeColor = new Color(0.95f, 0.77f, 0.06f); // Yellow
                else if (isSuccess) _edgeColor = new Color(0.18f, 0.8f, 0.44f); // Green
                else if (isFailure) _edgeColor = new Color(0.91f, 0.3f, 0.24f); // Red
                
                style.opacity = 1.0f;
            }
            else
            {
                _edgeColor = IsSelected ? new Color(0.2f, 0.6f, 1f) : new Color(0.4f, 0.4f, 0.4f, 0.5f);
                style.opacity = ToNode.style.opacity;
            }
            
            MarkDirtyRepaint();
        }
    }
}
