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
            
            // Limit dots to prevent vertex overflow (max ~10000 dots = 40000 vertices for circles)
            float effectiveGridSize = GridSize;
            int maxDotsX = (int)(rect.width / effectiveGridSize);
            int maxDotsY = (int)(rect.height / effectiveGridSize);
            int totalDots = maxDotsX * maxDotsY;
            
            // If too many dots, increase grid spacing
            while (totalDots > 8000 && effectiveGridSize < 200)
            {
                effectiveGridSize *= 2f;
                maxDotsX = (int)(rect.width / effectiveGridSize);
                maxDotsY = (int)(rect.height / effectiveGridSize);
                totalDots = maxDotsX * maxDotsY;
            }
            
            // Don't draw if still too many
            if (totalDots > 10000) return;
            
            var painter = ctx.painter2D;
            
            // Draw dots
            var dotColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);
            var thickDotColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            float dotSize = 1f;
            float startX = 0;
            float startY = 0;
            
            float thickInterval = effectiveGridSize * ThickLineInterval;
            
            for (float x = startX; x < rect.width; x += effectiveGridSize)
            {
                for (float y = startY; y < rect.height; y += effectiveGridSize)
                {
                    bool isThick = (Mathf.Abs(x % thickInterval) < 0.1f) && (Mathf.Abs(y % thickInterval) < 0.1f);
                    
                    painter.BeginPath();
                    float currentDotSize = isThick ? dotSize * 1.5f : dotSize;
                    painter.fillColor = isThick ? thickDotColor : dotColor;
                    
                    painter.Arc(new Vector2(x, y), currentDotSize, 0, 360);
                    painter.Fill();
                }
            }
        }
    }
}
