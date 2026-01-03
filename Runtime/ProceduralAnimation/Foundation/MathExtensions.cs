using Unity.Mathematics;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Extension methods for Unity.Mathematics types.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Smoothly interpolates between two values using second-order dynamics.
        /// </summary>
        public static float SpringLerp(this float current, float target, ref float velocity, 
                                        float frequency, float damping, float deltaTime)
        {
            float3 coeffs = SpringMath.ComputeCoefficients(frequency, damping, 0f);
            float k1 = coeffs.x;
            float k2 = coeffs.y;
            
            float k2Stable = SpringMath.ComputeStableK2(k1, k2, deltaTime);
            
            float newPos = current + deltaTime * velocity;
            velocity += deltaTime * (target - newPos - k1 * velocity) / k2Stable;
            
            return newPos;
        }
        
        /// <summary>
        /// Returns the shortest rotation from 'from' to 'to'.
        /// </summary>
        public static quaternion ShortestRotation(quaternion from, quaternion to)
        {
            float dot = math.dot(from.value, to.value);
            return dot < 0 ? new quaternion(-to.value) : to;
        }
        
        /// <summary>
        /// Safe division that returns zero when dividing by zero.
        /// </summary>
        public static float SafeDivide(float numerator, float denominator, float fallback = 0f)
        {
            return math.abs(denominator) < 0.0001f ? fallback : numerator / denominator;
        }
        
        /// <summary>
        /// Safe division for float3.
        /// </summary>
        public static float3 SafeDivide(float3 numerator, float denominator, float3 fallback = default)
        {
            return math.abs(denominator) < 0.0001f ? fallback : numerator / denominator;
        }
        
        /// <summary>
        /// Remaps a value from one range to another.
        /// </summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = math.saturate((value - fromMin) / (fromMax - fromMin));
            return math.lerp(toMin, toMax, t);
        }
        
        /// <summary>
        /// Applies exponential decay to a value.
        /// </summary>
        public static float ExponentialDecay(float current, float target, float halfLife, float deltaTime)
        {
            float decay = math.pow(0.5f, deltaTime / math.max(halfLife, 0.0001f));
            return target + (current - target) * decay;
        }
        
        /// <summary>
        /// Applies exponential decay to a float3.
        /// </summary>
        public static float3 ExponentialDecay(float3 current, float3 target, float halfLife, float deltaTime)
        {
            float decay = math.pow(0.5f, deltaTime / math.max(halfLife, 0.0001f));
            return target + (current - target) * decay;
        }
        
        /// <summary>
        /// Projects a vector onto a plane defined by its normal.
        /// </summary>
        public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
        {
            float3 normalizedNormal = math.normalizesafe(planeNormal);
            return vector - math.dot(vector, normalizedNormal) * normalizedNormal;
        }
        
        /// <summary>
        /// Returns the signed angle between two vectors around an axis.
        /// </summary>
        public static float SignedAngle(float3 from, float3 to, float3 axis)
        {
            float3 fromNorm = math.normalizesafe(from);
            float3 toNorm = math.normalizesafe(to);
            
            float angle = math.acos(math.clamp(math.dot(fromNorm, toNorm), -1f, 1f));
            float3 cross = math.cross(fromNorm, toNorm);
            float sign = math.sign(math.dot(axis, cross));
            
            return angle * sign;
        }
        
        /// <summary>
        /// Clamps a float3 to a maximum length.
        /// </summary>
        public static float3 ClampMagnitude(float3 vector, float maxLength)
        {
            float sqrMagnitude = math.lengthsq(vector);
            if (sqrMagnitude > maxLength * maxLength)
            {
                return math.normalize(vector) * maxLength;
            }
            return vector;
        }
        
        /// <summary>
        /// Smoothly damps a float3 toward a target.
        /// Similar to Unity's Vector3.SmoothDamp but for float3.
        /// </summary>
        public static float3 SmoothDamp(float3 current, float3 target, ref float3 velocity, 
                                         float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = math.max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            
            float3 change = current - target;
            float3 originalTo = target;
            
            float maxChange = maxSpeed * smoothTime;
            change = ClampMagnitude(change, maxChange);
            target = current - change;
            
            float3 temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            
            float3 output = target + (change + temp) * exp;
            
            // Prevent overshooting
            if (math.dot(originalTo - current, output - originalTo) > 0)
            {
                output = originalTo;
                velocity = (output - originalTo) / deltaTime;
            }
            
            return output;
        }
    }
}
