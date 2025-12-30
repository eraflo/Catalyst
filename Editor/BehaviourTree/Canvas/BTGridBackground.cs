using UnityEngine;
using UnityEngine.UIElements;

namespace Eraflo.UnityImportPackage.Editor.BehaviourTree.Canvas
{
    /// <summary>
    /// Grid background for the canvas.
    /// </summary>
    public class BTGridBackground : VisualElement
    {
        private const float GridSize = 20f;
        private const float ThickLineInterval = 5;
        
        public BTGridBackground()
        {
            name = "grid-background";
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Ignore;
            
            generateVisualContent += OnGenerateVisualContent;
        }
        
        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var rect = contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;
            
            var painter = ctx.painter2D;
            
            // Draw thin lines
            painter.strokeColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            painter.lineWidth = 1f;
            
            float startX = 0;
            float startY = 0;
            
            // Vertical lines
            for (float x = startX; x < rect.width; x += GridSize)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, rect.height));
                painter.Stroke();
            }
            
            // Horizontal lines
            for (float y = startY; y < rect.height; y += GridSize)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(rect.width, y));
                painter.Stroke();
            }
            
            // Draw thick lines
            painter.strokeColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
            painter.lineWidth = 2f;
            
            float thickInterval = GridSize * ThickLineInterval;
            
            for (float x = startX; x < rect.width; x += thickInterval)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, rect.height));
                painter.Stroke();
            }
            
            for (float y = startY; y < rect.height; y += thickInterval)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(rect.width, y));
                painter.Stroke();
            }
        }
    }
}
