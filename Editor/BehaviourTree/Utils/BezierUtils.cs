using UnityEngine;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Utils
{
    /// <summary>
    /// Utility class for Bezier curve calculations used by edge elements.
    /// </summary>
    public static class BezierUtils
    {
        /// <summary>
        /// Calculates a point on a cubic Bezier curve.
        /// </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">First control point</param>
        /// <param name="p2">Second control point</param>
        /// <param name="p3">End point</param>
        /// <param name="t">Parameter (0-1)</param>
        /// <returns>Point on the curve at parameter t</returns>
        public static Vector2 GetBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1 - t;
            float uu = u * u;
            float uuu = uu * u;
            float tt = t * t;
            float ttt = tt * t;
            
            return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
        }
        
        /// <summary>
        /// Calculates the squared distance from a point to a line segment.
        /// Uses squared distance to avoid expensive sqrt operation.
        /// </summary>
        public static float DistancePointToSegmentSq(Vector2 p, Vector2 a, Vector2 b)
        {
            float l2 = (a - b).sqrMagnitude;
            if (l2 == 0) return (p - a).sqrMagnitude;
            
            float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / l2);
            Vector2 projection = a + t * (b - a);
            return (p - projection).sqrMagnitude;
        }
        
        /// <summary>
        /// Calculates vertical control points for a top-to-bottom edge (parent-child connection).
        /// </summary>
        /// <param name="startPos">Start position (parent output)</param>
        /// <param name="endPos">End position (child input)</param>
        /// <param name="maxOffset">Maximum control point offset</param>
        /// <returns>Tuple of (cp1, cp2) control points</returns>
        public static (Vector2 cp1, Vector2 cp2) GetVerticalControlPoints(
            Vector2 startPos, Vector2 endPos, float maxOffset = 50f)
        {
            float yDistance = Mathf.Abs(endPos.y - startPos.y);
            float controlOffset = Mathf.Min(yDistance * 0.5f, maxOffset);
            
            var cp1 = new Vector2(startPos.x, startPos.y + controlOffset);
            var cp2 = new Vector2(endPos.x, endPos.y - controlOffset);
            return (cp1, cp2);
        }
        
        /// <summary>
        /// Calculates horizontal control points for a left-to-right edge (data flow connection).
        /// </summary>
        /// <param name="startPos">Start position (output port)</param>
        /// <param name="endPos">End position (input port)</param>
        /// <param name="maxOffset">Maximum tangent strength</param>
        /// <returns>Tuple of (cp1, cp2) control points</returns>
        public static (Vector2 cp1, Vector2 cp2) GetHorizontalControlPoints(
            Vector2 startPos, Vector2 endPos, float maxOffset = 100f)
        {
            float dist = Mathf.Abs(endPos.x - startPos.x);
            float tangentStrength = Mathf.Min(dist * 0.5f, maxOffset);
            
            var cp1 = startPos + new Vector2(tangentStrength, 0);
            var cp2 = endPos - new Vector2(tangentStrength, 0);
            return (cp1, cp2);
        }
        
        /// <summary>
        /// Checks if a point is near a Bezier curve by sampling.
        /// </summary>
        /// <param name="point">Point to test</param>
        /// <param name="p0">Curve start</param>
        /// <param name="p1">First control point</param>
        /// <param name="p2">Second control point</param>
        /// <param name="p3">Curve end</param>
        /// <param name="thresholdSq">Squared distance threshold</param>
        /// <param name="samples">Number of samples along the curve</param>
        /// <returns>True if point is within threshold distance of the curve</returns>
        public static bool IsPointNearCurve(
            Vector2 point, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, 
            float thresholdSq, int samples = 10)
        {
            float minDistanceSq = float.MaxValue;
            Vector2 lastPoint = p0;
            
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 currentPoint = GetBezierPoint(p0, p1, p2, p3, t);
                
                float distSq = DistancePointToSegmentSq(point, lastPoint, currentPoint);
                if (distSq < minDistanceSq) minDistanceSq = distSq;
                
                lastPoint = currentPoint;
            }
            
            return minDistanceSq < thresholdSq;
        }
        
        /// <summary>
        /// Calculates the bounding box for a Bezier curve with padding.
        /// </summary>
        public static (float xMin, float yMin, float width, float height) GetCurveBounds(
            Vector2 startPos, Vector2 endPos, float padding = 20f)
        {
            float xMin = Mathf.Min(startPos.x, endPos.x) - padding;
            float yMin = Mathf.Min(startPos.y, endPos.y) - padding;
            float xMax = Mathf.Max(startPos.x, endPos.x) + padding;
            float yMax = Mathf.Max(startPos.y, endPos.y) + padding;
            
            return (xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
