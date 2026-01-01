using UnityEngine;
using UnityEngine.UIElements;
using Eraflo.Catalyst.BehaviourTree;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    public class BTDataTempEdgeElement : VisualElement
    {
        public BTNodeElement FromNode { get; private set; }
        public NodePort FromPort { get; private set; }
        public Vector2 EndPoints { get; set; }
        
        private Color _edgeColor = Color.white;
        
        public BTDataTempEdgeElement(BTNodeElement from, NodePort fromPort)
        {
            FromNode = from;
            FromPort = fromPort;
            
            name = "bt-temp-data-edge";
            style.position = Position.Absolute;
            pickingMode = PickingMode.Ignore;
            
            generateVisualContent += OnGenerateVisualContent;
            
             // Colors
            if (fromPort != null)
            {
                if (fromPort.DataType == typeof(bool)) _edgeColor = new Color(1f, 0.4f, 0.4f);
                else if (fromPort.DataType == typeof(Vector3)) _edgeColor = new Color(0.6f, 1f, 0.6f);
                else if (fromPort.DataType == typeof(float)) _edgeColor = new Color(0.4f, 0.8f, 1f);
                else _edgeColor = Color.white;
            }
        }
        
        public void Update(Vector2 startPos, Vector2 endPos)
        {
            style.left = 0;
            style.top = 0;
            style.right = float.NaN;
            style.bottom = float.NaN;
            style.width = 0;
            style.height = 0;
            
            _startPos = startPos;
            _endPos = endPos;
            MarkDirtyRepaint();
        }
        
        private Vector2 _startPos;
        private Vector2 _endPos;
        
        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            painter.strokeColor = _edgeColor;
            painter.lineWidth = 2f;
            painter.lineCap = LineCap.Round;
            
            painter.BeginPath();
            painter.MoveTo(_startPos);
            
            // Curve logic
            float dist = Mathf.Abs(_endPos.x - _startPos.x);
            float tangentStrength = Mathf.Min(dist * 0.5f, 100f);
            
            var cp1 = _startPos + new Vector2(tangentStrength, 0);
            var cp2 = _endPos - new Vector2(tangentStrength, 0);
            
            painter.BezierCurveTo(cp1, cp2, _endPos);
            painter.Stroke();
        }
    }
}
