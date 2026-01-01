using UnityEngine;
using UnityEngine.UIElements;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    /// <summary>
    /// Visual element used to preview an edge during creation.
    /// Follows the mouse cursor.
    /// </summary>
    public class BTTempEdgeElement : VisualElement
    {
        private BTNodeElement _fromNode;
        private Vector2 _targetPos;
        private Color _edgeColor = new Color(1f, 1f, 1f, 0.5f);
        
        public BTTempEdgeElement(BTNodeElement from)
        {
            _fromNode = from;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            
            generateVisualContent += OnGenerateVisualContent;
        }
        
        public void UpdateTarget(Vector2 localMousePos)
        {
            _targetPos = localMousePos;
            MarkDirtyRepaint();
        }
        
        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (_fromNode == null) return;
            
            var startPos = _fromNode.GetOutputPortCenter();
            var endPos = _targetPos;
            
            var painter = ctx.painter2D;
            painter.strokeColor = _edgeColor;
            painter.lineWidth = 2f;
            
            painter.BeginPath();
            painter.MoveTo(startPos);
            
            float yDistance = Mathf.Abs(endPos.y - startPos.y);
            float controlOffset = Mathf.Min(yDistance * 0.5f, 50f);
            
            var cp1 = new Vector2(startPos.x, startPos.y + controlOffset);
            var cp2 = new Vector2(endPos.x, endPos.y - controlOffset);
            
            painter.BezierCurveTo(cp1, cp2, endPos);
            painter.Stroke();
        }
    }
}
