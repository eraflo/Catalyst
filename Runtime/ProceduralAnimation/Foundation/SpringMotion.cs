using Unity.Mathematics;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Second Order Dynamics system for smooth, physically-based motion.
    /// Replaces Mathf.Lerp with a mass-spring-damper simulation.
    /// </summary>
    /// <remarks>
    /// Based on the math from t3ssel8r's "Giving Personality to Procedural Animations".
    /// The system simulates a critically/under/over-damped spring.
    /// 
    /// Parameters:
    /// - Frequency (f): Natural frequency of the system in Hz. Higher = faster response.
    /// - Damping (ζ): Damping ratio. 0 = no damping (oscillates forever), 1 = critical damping, >1 = overdamped.
    /// - Response (r): Initial response. Negative values cause anticipation, >1 causes overshoot.
    /// </remarks>
    public struct SpringMotion
    {
        // Current state
        private float3 _position;
        private float3 _velocity;
        private float3 _previousTarget;
        
        // Pre-computed coefficients (computed once in Configure)
        private float _k1; // Velocity coefficient
        private float _k2; // Acceleration coefficient  
        private float _k3; // Response coefficient
        
        private bool _initialized;
        
        /// <summary>
        /// Current position of the spring system.
        /// </summary>
        public float3 Position => _position;
        
        /// <summary>
        /// Current velocity of the spring system.
        /// </summary>
        public float3 Velocity => _velocity;
        
        /// <summary>
        /// Creates a new SpringMotion with default parameters (critically damped, 1Hz).
        /// </summary>
        public static SpringMotion Default => Create(1f, 1f, 0f);
        
        /// <summary>
        /// Creates a SpringMotion pre-configured with the given parameters.
        /// </summary>
        /// <param name="frequency">Natural frequency in Hz. Range: 0.01 to 100.</param>
        /// <param name="damping">Damping ratio. 0 = oscillates, 1 = critical, >1 = overdamped.</param>
        /// <param name="response">Initial response. Negative = anticipation, >1 = overshoot.</param>
        public static SpringMotion Create(float frequency, float damping, float response)
        {
            var spring = new SpringMotion();
            spring.Configure(frequency, damping, response);
            return spring;
        }
        
        /// <summary>
        /// Creates a SpringMotion pre-configured with a preset.
        /// </summary>
        public static SpringMotion Create(SpringPreset preset)
        {
            return preset switch
            {
                SpringPreset.Snappy => Create(4f, 0.5f, 2f),
                SpringPreset.Smooth => Create(1f, 1f, 0f),
                SpringPreset.Bouncy => Create(2f, 0.3f, 0f),
                SpringPreset.Sluggish => Create(0.5f, 1.5f, 0f),
                SpringPreset.Anticipate => Create(2f, 1f, -0.5f),
                _ => Default
            };
        }
        
        /// <summary>
        /// Configures the spring dynamics coefficients.
        /// </summary>
        /// <param name="frequency">Natural frequency in Hz. Higher = faster response.</param>
        /// <param name="damping">Damping ratio. 0 = oscillates, 1 = critical, >1 = overdamped.</param>
        /// <param name="response">Initial response. Negative = anticipation, >1 = overshoot.</param>
        public void Configure(float frequency, float damping, float response)
        {
            // Use centralized coefficient calculation
            float3 coeffs = SpringMath.ComputeCoefficients(frequency, damping, response);
            _k1 = coeffs.x;
            _k2 = coeffs.y;
            _k3 = coeffs.z;
        }
        
        /// <summary>
        /// Resets the spring to a specific position with zero velocity.
        /// </summary>
        public void Reset(float3 position)
        {
            _position = position;
            _velocity = float3.zero;
            _previousTarget = position;
            _initialized = true;
        }
        
        /// <summary>
        /// Updates the spring simulation toward the target position.
        /// </summary>
        /// <param name="target">The target position to move toward.</param>
        /// <param name="deltaTime">Time step in seconds.</param>
        /// <returns>The new position after the update.</returns>
        public float3 Update(float3 target, float deltaTime)
        {
            // Initialize on first call
            if (!_initialized)
            {
                Reset(target);
                return _position;
            }
            
            // Clamp deltaTime to avoid instability
            deltaTime = math.clamp(deltaTime, 0f, 0.1f);
            
            if (deltaTime <= 0f)
                return _position;
            
            // Estimate target velocity using finite difference
            float3 targetVelocity = (target - _previousTarget) / deltaTime;
            _previousTarget = target;
            
            return Update(target, targetVelocity, deltaTime);
        }
        
        /// <summary>
        /// Updates the spring simulation with explicit target velocity.
        /// Use this variant when you know the target's velocity for smoother tracking.
        /// </summary>
        /// <param name="target">The target position to move toward.</param>
        /// <param name="targetVelocity">The velocity of the target.</param>
        /// <param name="deltaTime">Time step in seconds.</param>
        /// <returns>The new position after the update.</returns>
        public float3 Update(float3 target, float3 targetVelocity, float deltaTime)
        {
            // Initialize on first call
            if (!_initialized)
            {
                Reset(target);
                return _position;
            }
            
            // Clamp deltaTime to avoid instability
            deltaTime = math.clamp(deltaTime, 0.0001f, 0.1f);
            
            // Use centralized spring update
            SpringMath.UpdateSpring(ref _position, ref _velocity, target, targetVelocity, _k1, _k2, _k3, deltaTime);
            
            return _position;
        }
    }
    
    /// <summary>
    /// Common spring presets for quick configuration.
    /// </summary>
    public enum SpringPreset
    {
        /// <summary>Fast response with slight overshoot. Good for UI, quick reactions.</summary>
        Snappy,
        /// <summary>Critically damped, smooth approach. Good for cameras, following.</summary>
        Smooth,
        /// <summary>Underdamped with oscillation. Good for bouncy objects, jelly.</summary>
        Bouncy,
        /// <summary>Overdamped, slow response. Good for heavy objects, lazy following.</summary>
        Sluggish,
        /// <summary>Anticipation before moving. Good for cartoon-style animation wind-up.</summary>
        Anticipate
    }
}
