using Unity.Mathematics;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Quaternion variant of SpringMotion for smooth rotation interpolation.
    /// Uses the same second-order dynamics as SpringMotion but operates on rotations.
    /// </summary>
    public struct SpringMotionQuaternion
    {
        private quaternion _rotation;
        private float3 _angularVelocity;
        private quaternion _previousTarget;
        
        private float _k1;
        private float _k2;
        private float _k3;
        
        private bool _initialized;
        
        /// <summary>
        /// Current rotation of the spring system.
        /// </summary>
        public quaternion Rotation => _rotation;
        
        /// <summary>
        /// Current angular velocity in radians per second.
        /// </summary>
        public float3 AngularVelocity => _angularVelocity;
        
        /// <summary>
        /// Creates a SpringMotionQuaternion with default parameters.
        /// </summary>
        public static SpringMotionQuaternion Default => Create(1f, 1f, 0f);
        
        /// <summary>
        /// Creates a SpringMotionQuaternion pre-configured with the given parameters.
        /// </summary>
        public static SpringMotionQuaternion Create(float frequency, float damping, float response)
        {
            var spring = new SpringMotionQuaternion();
            spring.Configure(frequency, damping, response);
            return spring;
        }
        
        /// <summary>
        /// Creates a SpringMotionQuaternion from a preset.
        /// </summary>
        public static SpringMotionQuaternion Create(SpringPreset preset)
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
        public void Configure(float frequency, float damping, float response)
        {
            // Use centralized coefficient calculation
            float3 coeffs = SpringMath.ComputeCoefficients(frequency, damping, response);
            _k1 = coeffs.x;
            _k2 = coeffs.y;
            _k3 = coeffs.z;
        }
        
        /// <summary>
        /// Resets the spring to a specific rotation with zero angular velocity.
        /// </summary>
        public void Reset(quaternion rotation)
        {
            _rotation = math.normalizesafe(rotation);
            _angularVelocity = float3.zero;
            _previousTarget = _rotation;
            _initialized = true;
        }
        
        /// <summary>
        /// Updates the spring simulation toward the target rotation.
        /// </summary>
        /// <param name="target">The target rotation to move toward.</param>
        /// <param name="deltaTime">Time step in seconds.</param>
        /// <returns>The new rotation after the update.</returns>
        public quaternion Update(quaternion target, float deltaTime)
        {
            if (!_initialized)
            {
                Reset(target);
                return _rotation;
            }
            
            deltaTime = math.clamp(deltaTime, 0f, 0.1f);
            
            if (deltaTime <= 0f)
                return _rotation;
            
            // Estimate target angular velocity
            float3 targetAngularVelocity = QuaternionToAngularVelocity(_previousTarget, target, deltaTime);
            _previousTarget = target;
            
            return Update(target, targetAngularVelocity, deltaTime);
        }
        
        /// <summary>
        /// Updates the spring simulation with explicit target angular velocity.
        /// </summary>
        public quaternion Update(quaternion target, float3 targetAngularVelocity, float deltaTime)
        {
            if (!_initialized)
            {
                Reset(target);
                return _rotation;
            }
            
            deltaTime = math.clamp(deltaTime, 0.0001f, 0.1f);
            
            // Ensure we take the shortest path
            target = EnsureShortestPath(_rotation, target);
            
            // Convert rotation difference to angular error
            float3 angularError = QuaternionToAngularVelocity(_rotation, target, 1f);
            
            // Semi-implicit Euler integration
            float k2Stable = SpringMath.ComputeStableK2(_k1, _k2, deltaTime);
            
            float3 angularAcceleration = (angularError + _k3 * targetAngularVelocity - _k1 * _angularVelocity) / k2Stable;
            
            _angularVelocity += deltaTime * angularAcceleration;
            
            // Apply angular velocity as rotation
            quaternion deltaRotation = AngularVelocityToQuaternion(_angularVelocity * deltaTime);
            _rotation = math.normalizesafe(math.mul(deltaRotation, _rotation));
            
            return _rotation;
        }
        
        /// <summary>
        /// Converts the difference between two quaternions to angular velocity.
        /// </summary>
        private static float3 QuaternionToAngularVelocity(quaternion from, quaternion to, float deltaTime)
        {
            quaternion delta = math.mul(to, math.conjugate(from));
            delta = EnsureShortestPath(quaternion.identity, delta);
            delta = math.normalizesafe(delta);
            
            // Handle the case where delta is close to identity
            float sinHalfAngle = math.length(delta.value.xyz);
            if (sinHalfAngle < 0.0001f)
                return float3.zero;
            
            float halfAngle = math.asin(math.clamp(sinHalfAngle, -1f, 1f));
            float angle = 2f * halfAngle;
            
            float3 axis = delta.value.xyz / sinHalfAngle;
            
            return axis * angle / math.max(deltaTime, 0.0001f);
        }
        
        /// <summary>
        /// Converts angular velocity (axis * angle) to a quaternion rotation.
        /// </summary>
        private static quaternion AngularVelocityToQuaternion(float3 angularVelocity)
        {
            float angle = math.length(angularVelocity);
            if (angle < 0.0001f)
                return quaternion.identity;
            
            float3 axis = angularVelocity / angle;
            return quaternion.AxisAngle(axis, angle);
        }
        
        /// <summary>
        /// Ensures the quaternion takes the shortest path.
        /// </summary>
        private static quaternion EnsureShortestPath(quaternion from, quaternion to)
        {
            float dot = math.dot(from.value, to.value);
            return dot < 0 ? new quaternion(-to.value) : to;
        }
    }
}
