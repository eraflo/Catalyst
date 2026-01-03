using Unity.Burst;
using Unity.Mathematics;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Centralized spring dynamics math utilities.
    /// All spring-related calculations should use these methods to avoid duplication.
    /// </summary>
    public static class SpringMath
    {
        /// <summary>
        /// Computes numerically stable K2 coefficient for semi-implicit Euler integration.
        /// This prevents instability when deltaTime is large or K2 is small.
        /// </summary>
        /// <param name="k1">Velocity coefficient (damping / (PI * frequency))</param>
        /// <param name="k2">Acceleration coefficient (1 / omega^2)</param>
        /// <param name="deltaTime">Time step in seconds</param>
        /// <returns>Stabilized K2 value</returns>
        public static float ComputeStableK2(float k1, float k2, float deltaTime)
        {
            return math.max(k2,
                math.max(deltaTime * deltaTime / 2f + deltaTime * k1 / 2f,
                         deltaTime * k1));
        }
        
        /// <summary>
        /// Computes spring coefficients from frequency, damping, and response.
        /// </summary>
        /// <param name="frequency">Natural frequency in Hz (0.01-100)</param>
        /// <param name="damping">Damping ratio (0 = oscillates, 1 = critical, >1 = overdamped)</param>
        /// <param name="response">Initial response (negative = anticipation, >1 = overshoot)</param>
        /// <returns>Tuple of (K1, K2, K3) coefficients</returns>
        public static float3 ComputeCoefficients(float frequency, float damping, float response)
        {
            frequency = math.max(frequency, 0.01f);
            float omega = 2f * math.PI * frequency;
            
            float k1 = damping / (math.PI * frequency);  // = 2ζ/ω
            float k2 = 1f / (omega * omega);              // = 1/ω²
            float k3 = response * damping / omega;        // = r*ζ/ω
            
            return new float3(k1, k2, k3);
        }
        
        /// <summary>
        /// Performs a single spring update step.
        /// </summary>
        /// <param name="position">Current position (modified in place)</param>
        /// <param name="velocity">Current velocity (modified in place)</param>
        /// <param name="target">Target position</param>
        /// <param name="targetVelocity">Target velocity (can be zero)</param>
        /// <param name="k1">Velocity coefficient</param>
        /// <param name="k2">Acceleration coefficient</param>
        /// <param name="k3">Response coefficient</param>
        /// <param name="deltaTime">Time step</param>
        public static void UpdateSpring(
            ref float3 position,
            ref float3 velocity,
            in float3 target,
            in float3 targetVelocity,
            float k1, float k2, float k3,
            float deltaTime)
        {
            float k2Stable = ComputeStableK2(k1, k2, deltaTime);
            
            position += deltaTime * velocity;
            velocity += deltaTime * (target + k3 * targetVelocity - position - k1 * velocity) / k2Stable;
        }
        
        /// <summary>
        /// Performs a single spring update step (simplified, no target velocity).
        /// </summary>
        public static void UpdateSpring(
            ref float3 position,
            ref float3 velocity,
            in float3 target,
            float k1, float k2,
            float deltaTime)
        {
            float k2Stable = ComputeStableK2(k1, k2, deltaTime);
            
            position += deltaTime * velocity;
            velocity += deltaTime * (target - position - k1 * velocity) / k2Stable;
        }
    }
    
    /// <summary>
    /// Pre-computed spring coefficients for use in jobs.
    /// Use this struct to pass spring configuration to Burst jobs.
    /// </summary>
    public struct SpringCoefficients
    {
        public float K1;
        public float K2;
        public float K3;
        
        /// <summary>
        /// Creates coefficients from frequency, damping, and response parameters.
        /// </summary>
        public static SpringCoefficients Create(float frequency, float damping, float response)
        {
            float3 coeffs = SpringMath.ComputeCoefficients(frequency, damping, response);
            return new SpringCoefficients
            {
                K1 = coeffs.x,
                K2 = coeffs.y,
                K3 = coeffs.z
            };
        }
        
        /// <summary>
        /// Creates coefficients from a preset.
        /// </summary>
        public static SpringCoefficients FromPreset(SpringPreset preset)
        {
            return preset switch
            {
                SpringPreset.Snappy => Create(4f, 0.5f, 2f),
                SpringPreset.Smooth => Create(1f, 1f, 0f),
                SpringPreset.Bouncy => Create(2f, 0.3f, 0f),
                SpringPreset.Sluggish => Create(0.5f, 1.5f, 0f),
                SpringPreset.Anticipate => Create(2f, 1f, -0.5f),
                _ => Create(1f, 1f, 0f)
            };
        }
        
        /// <summary>
        /// Computes stable K2 for the given deltaTime.
        /// </summary>
        public float GetStableK2(float deltaTime)
        {
            return SpringMath.ComputeStableK2(K1, K2, deltaTime);
        }
    }
}
