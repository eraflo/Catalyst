using UnityEngine;
using UnityEngine.UIElements;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    /// <summary>
    /// Grid background for the canvas.
    /// OPTIMIZED: Uses rectangles instead of arcs to drastically reduce vertex count.
    /// </summary>
    public class BTGridBackground : VisualElement
    {
        private const float BaseGridSize = 20f;
        private const float ThickLineInterval = 5;
        
        // Unity limit is 65535 vertices. Each rect path uses ~6 vertices.
        // Stay well under: 65535 / 6 = ~10922, use 2000 for safety margin
        private const int MaxDots = 2000;
        
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
            try
            {
                DrawGrid(ctx);
            }
            catch (System.Exception)
            {
                // Silently fail if we hit vertex limits - grid is not critical
            }
        }
        
        private void DrawGrid(MeshGenerationContext ctx)
        {
            var rect = contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;
            
            // Calculate effective grid size to stay well under vertex limit
            float effectiveGridSize = BaseGridSize;
            int maxDotsX = Mathf.Max(1, Mathf.FloorToInt(rect.width / effectiveGridSize));
            int maxDotsY = Mathf.Max(1, Mathf.FloorToInt(rect.height / effectiveGridSize));
            int totalDots = maxDotsX * maxDotsY;
            
            // Increase grid spacing until we're safely under the limit
            while (totalDots > MaxDots)
            {
                effectiveGridSize *= 2f;
                if (effectiveGridSize > 500f) return; // Give up if grid would be too sparse
                
                maxDotsX = Mathf.Max(1, Mathf.FloorToInt(rect.width / effectiveGridSize));
                maxDotsY = Mathf.Max(1, Mathf.FloorToInt(rect.height / effectiveGridSize));
                totalDots = maxDotsX * maxDotsY;
            }
            
            var painter = ctx.painter2D;
            
            // Colors
            var dotColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);
            var thickDotColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            float dotSize = 1.5f;
            float thickDotSize = 2.5f;
            float thickInterval = effectiveGridSize * ThickLineInterval;
            
            // Draw dots using simple rectangles
            for (float x = 0; x < rect.width; x += effectiveGridSize)
            {
                for (float y = 0; y < rect.height; y += effectiveGridSize)
                {
                    bool isThick = (x % thickInterval < 0.5f) && (y % thickInterval < 0.5f);
                    float size = isThick ? thickDotSize : dotSize;
                    float halfSize = size * 0.5f;
                    
                    painter.BeginPath();
                    painter.fillColor = isThick ? thickDotColor : dotColor;
                    
                    painter.MoveTo(new Vector2(x - halfSize, y - halfSize));
                    painter.LineTo(new Vector2(x + halfSize, y - halfSize));
                    painter.LineTo(new Vector2(x + halfSize, y + halfSize));
                    painter.LineTo(new Vector2(x - halfSize, y + halfSize));
                    painter.ClosePath();
                    painter.Fill();
                }
            }
        }
    }
}

