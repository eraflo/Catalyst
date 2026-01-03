using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Studio
{
    /// <summary>
    /// A 3D spatial curve for defining movement trajectories.
    /// Unlike AnimationCurve which maps time to value, this maps parameter t to 3D position.
    /// </summary>
    [Serializable]
    public class SpatialCurve
    {
        [SerializeField] private List<CurvePoint> _points = new List<CurvePoint>();
        [SerializeField] private CurveType _type = CurveType.CatmullRom;
        [SerializeField] private bool _closed;
        
        /// <summary>
        /// Number of control points.
        /// </summary>
        public int PointCount => _points.Count;
        
        /// <summary>
        /// Whether the curve is closed (loops back).
        /// </summary>
        public bool IsClosed => _closed;
        
        /// <summary>
        /// Type of interpolation.
        /// </summary>
        public CurveType Type
        {
            get => _type;
            set => _type = value;
        }
        
        #region Construction
        
        public SpatialCurve() { }
        
        public SpatialCurve(CurveType type)
        {
            _type = type;
        }
        
        /// <summary>
        /// Creates a curve from an array of positions.
        /// </summary>
        public static SpatialCurve FromPoints(float3[] positions, CurveType type = CurveType.CatmullRom)
        {
            var curve = new SpatialCurve(type);
            foreach (var pos in positions)
            {
                curve.AddPoint(pos);
            }
            return curve;
        }
        
        /// <summary>
        /// Creates a circular arc.
        /// </summary>
        public static SpatialCurve Arc(float3 center, float radius, float startAngle, float endAngle, float3 axis)
        {
            var curve = new SpatialCurve(CurveType.Bezier);
            
            // Generate points along arc
            int segments = math.max(4, (int)(math.abs(endAngle - startAngle) / 30f));
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = math.lerp(startAngle, endAngle, t);
                
                // Rotate around axis
                quaternion rot = quaternion.AxisAngle(math.normalizesafe(axis), math.radians(angle));
                float3 offset = math.mul(rot, new float3(radius, 0, 0));
                curve.AddPoint(center + offset);
            }
            
            return curve;
        }
        
        /// <summary>
        /// Creates a spiral.
        /// </summary>
        public static SpatialCurve Spiral(float3 center, float startRadius, float endRadius, 
            float height, float turns, int segments = 32)
        {
            var curve = new SpatialCurve(CurveType.CatmullRom);
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * turns * math.PI * 2f;
                float radius = math.lerp(startRadius, endRadius, t);
                float y = t * height;
                
                float3 point = center + new float3(
                    math.cos(angle) * radius,
                    y,
                    math.sin(angle) * radius
                );
                curve.AddPoint(point);
            }
            
            return curve;
        }
        
        /// <summary>
        /// Creates a figure-8 pattern.
        /// </summary>
        public static SpatialCurve Figure8(float3 center, float width, float height, int segments = 32)
        {
            var curve = new SpatialCurve(CurveType.CatmullRom) { _closed = true };
            
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments * math.PI * 2f;
                float3 point = center + new float3(
                    math.sin(t) * width,
                    0,
                    math.sin(t * 2f) * height * 0.5f
                );
                curve.AddPoint(point);
            }
            
            return curve;
        }
        
        #endregion
        
        #region Point Management
        
        /// <summary>
        /// Adds a point at the end of the curve.
        /// </summary>
        public void AddPoint(float3 position, float3 tangentIn = default, float3 tangentOut = default)
        {
            _points.Add(new CurvePoint
            {
                Position = position,
                TangentIn = tangentIn,
                TangentOut = tangentOut
            });
        }
        
        /// <summary>
        /// Inserts a point at the specified index.
        /// </summary>
        public void InsertPoint(int index, float3 position)
        {
            _points.Insert(index, new CurvePoint { Position = position });
        }
        
        /// <summary>
        /// Removes a point at the specified index.
        /// </summary>
        public void RemovePoint(int index)
        {
            if (index >= 0 && index < _points.Count)
            {
                _points.RemoveAt(index);
            }
        }
        
        /// <summary>
        /// Gets the position of a point.
        /// </summary>
        public float3 GetPoint(int index)
        {
            return _points[index].Position;
        }
        
        /// <summary>
        /// Sets the position of a point.
        /// </summary>
        public void SetPoint(int index, float3 position)
        {
            var point = _points[index];
            point.Position = position;
            _points[index] = point;
        }
        
        /// <summary>
        /// Closes or opens the curve.
        /// </summary>
        public void SetClosed(bool closed)
        {
            _closed = closed;
        }
        
        #endregion
        
        #region Evaluation
        
        /// <summary>
        /// Evaluates the curve at parameter t (0 to 1).
        /// </summary>
        public float3 Evaluate(float t)
        {
            if (_points.Count == 0) return float3.zero;
            if (_points.Count == 1) return _points[0].Position;
            
            t = _closed ? t % 1f : math.saturate(t);
            if (t < 0) t += 1f;
            
            int segmentCount = _closed ? _points.Count : _points.Count - 1;
            float scaledT = t * segmentCount;
            int segmentIndex = math.min((int)scaledT, segmentCount - 1);
            float localT = scaledT - segmentIndex;
            
            return EvaluateSegment(segmentIndex, localT);
        }
        
        /// <summary>
        /// Evaluates the tangent (direction) at parameter t.
        /// </summary>
        public float3 EvaluateTangent(float t)
        {
            const float epsilon = 0.001f;
            float3 p1 = Evaluate(t - epsilon);
            float3 p2 = Evaluate(t + epsilon);
            return math.normalizesafe(p2 - p1);
        }
        
        /// <summary>
        /// Evaluates position and tangent as a frame.
        /// </summary>
        public void EvaluateFrame(float t, out float3 position, out float3 tangent, out float3 up)
        {
            position = Evaluate(t);
            tangent = EvaluateTangent(t);
            
            // Use Frenet-Serret frame
            float3 worldUp = new float3(0, 1, 0);
            if (math.abs(math.dot(tangent, worldUp)) > 0.99f)
            {
                worldUp = new float3(0, 0, 1);
            }
            
            float3 right = math.normalizesafe(math.cross(worldUp, tangent));
            up = math.cross(tangent, right);
        }
        
        /// <summary>
        /// Calculates the approximate length of the curve.
        /// </summary>
        public float CalculateLength(int samples = 64)
        {
            float length = 0f;
            float3 prevPoint = Evaluate(0f);
            
            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                float3 point = Evaluate(t);
                length += math.length(point - prevPoint);
                prevPoint = point;
            }
            
            return length;
        }
        
        /// <summary>
        /// Gets a uniform parameter from arc length.
        /// </summary>
        public float GetUniformParameter(float distance, int samples = 64)
        {
            float totalLength = CalculateLength(samples);
            if (totalLength <= 0f) return 0f;
            
            float targetLength = distance;
            float length = 0f;
            float3 prevPoint = Evaluate(0f);
            
            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                float3 point = Evaluate(t);
                float segmentLength = math.length(point - prevPoint);
                
                if (length + segmentLength >= targetLength)
                {
                    float localT = (targetLength - length) / segmentLength;
                    return math.lerp((float)(i - 1) / samples, t, localT);
                }
                
                length += segmentLength;
                prevPoint = point;
            }
            
            return 1f;
        }
        
        private float3 EvaluateSegment(int segment, float t)
        {
            int count = _points.Count;
            
            int i0 = GetWrappedIndex(segment - 1, count);
            int i1 = GetWrappedIndex(segment, count);
            int i2 = GetWrappedIndex(segment + 1, count);
            int i3 = GetWrappedIndex(segment + 2, count);
            
            float3 p0 = _points[i0].Position;
            float3 p1 = _points[i1].Position;
            float3 p2 = _points[i2].Position;
            float3 p3 = _points[i3].Position;
            
            return _type switch
            {
                CurveType.Linear => math.lerp(p1, p2, t),
                CurveType.CatmullRom => CatmullRom(p0, p1, p2, p3, t),
                CurveType.Bezier => CubicBezier(p1, p1 + _points[i1].TangentOut, 
                                                 p2 + _points[i2].TangentIn, p2, t),
                _ => math.lerp(p1, p2, t)
            };
        }
        
        private int GetWrappedIndex(int index, int count)
        {
            if (_closed)
            {
                return ((index % count) + count) % count;
            }
            return math.clamp(index, 0, count - 1);
        }
        
        private static float3 CatmullRom(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
        
        private static float3 CubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            
            return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
        }
        
        #endregion
    }
    
    /// <summary>
    /// A single point on a spatial curve.
    /// </summary>
    [Serializable]
    public struct CurvePoint
    {
        public float3 Position;
        public float3 TangentIn;
        public float3 TangentOut;
    }
    
    /// <summary>
    /// Type of curve interpolation.
    /// </summary>
    public enum CurveType
    {
        Linear,
        CatmullRom,
        Bezier
    }
}
