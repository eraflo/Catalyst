using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.Demo
{
    /// <summary>
    /// Simple component that applies SpringMotion to follow a target.
    /// Can be used as a reference for using the SpringMotion system.
    /// </summary>
    [AddComponentMenu("Catalyst/Procedural Animation/Spring Follower")]
    public class SpringFollower : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The transform to follow. If null, follows its own starting position.")]
        [SerializeField] private Transform _target;
        
        [Header("Spring Settings")]
        [Tooltip("Preset spring behavior.")]
        [SerializeField] private SpringPreset _preset = SpringPreset.Smooth;
        
        [Tooltip("Use custom spring parameters instead of preset.")]
        [SerializeField] private bool _useCustomParameters;
        
        [Tooltip("Natural frequency in Hz. Higher = faster response.")]
        [SerializeField, Range(0.1f, 10f)] private float _frequency = 1f;
        
        [Tooltip("Damping ratio. 0 = oscillates, 1 = critical, >1 = overdamped.")]
        [SerializeField, Range(0f, 2f)] private float _damping = 1f;
        
        [Tooltip("Initial response. Negative = anticipation, >1 = overshoot.")]
        [SerializeField, Range(-2f, 3f)] private float _response = 0f;
        
        [Header("Options")]
        [Tooltip("Apply to rotation as well.")]
        [SerializeField] private bool _followRotation = true;
        
        [Tooltip("Use local space instead of world space.")]
        [SerializeField] private bool _useLocalSpace;
        
        private SpringMotion _positionSpring;
        private SpringMotionQuaternion _rotationSpring;
        private float3 _staticTarget;
        private quaternion _staticRotationTarget;
        private bool _initialized;
        
        private void Start()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (_initialized) return;
            
            ConfigureSprings();
            
            // Store static target if no target transform
            if (_target == null)
            {
                _staticTarget = _useLocalSpace ? (float3)transform.localPosition : (float3)transform.position;
                _staticRotationTarget = _useLocalSpace ? (quaternion)transform.localRotation : (quaternion)transform.rotation;
            }
            
            // Reset springs to current position
            float3 currentPos = _useLocalSpace ? (float3)transform.localPosition : (float3)transform.position;
            quaternion currentRot = _useLocalSpace ? (quaternion)transform.localRotation : (quaternion)transform.rotation;
            
            _positionSpring.Reset(currentPos);
            _rotationSpring.Reset(currentRot);
            
            _initialized = true;
        }
        
        private void ConfigureSprings()
        {
            if (_useCustomParameters)
            {
                _positionSpring = SpringMotion.Create(_frequency, _damping, _response);
                _rotationSpring = SpringMotionQuaternion.Create(_frequency, _damping, _response);
            }
            else
            {
                _positionSpring = SpringMotion.Create(_preset);
                _rotationSpring = SpringMotionQuaternion.Create(_preset);
            }
        }
        
        private void LateUpdate()
        {
            if (!_initialized) Initialize();
            
            float deltaTime = Time.deltaTime;
            
            // Get target
            float3 targetPos;
            quaternion targetRot;
            
            if (_target != null)
            {
                targetPos = _useLocalSpace ? (float3)_target.localPosition : (float3)_target.position;
                targetRot = _useLocalSpace ? (quaternion)_target.localRotation : (quaternion)_target.rotation;
            }
            else
            {
                targetPos = _staticTarget;
                targetRot = _staticRotationTarget;
            }
            
            // Update position spring
            float3 newPos = _positionSpring.Update(targetPos, deltaTime);
            
            if (_useLocalSpace)
                transform.localPosition = newPos;
            else
                transform.position = newPos;
            
            // Update rotation spring
            if (_followRotation)
            {
                quaternion newRot = _rotationSpring.Update(targetRot, deltaTime);
                
                if (_useLocalSpace)
                    transform.localRotation = newRot;
                else
                    transform.rotation = newRot;
            }
        }
        
        /// <summary>
        /// Sets a new target transform.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }
        
        /// <summary>
        /// Sets a static target position (world space).
        /// </summary>
        public void SetTargetPosition(Vector3 position)
        {
            _target = null;
            _staticTarget = position;
        }
        
        /// <summary>
        /// Applies a velocity impulse to the spring.
        /// </summary>
        public void ApplyImpulse(Vector3 impulse)
        {
            // Note: SpringMotion doesn't expose velocity setting directly
            // This is a limitation that could be addressed in a future version
        }
        
        /// <summary>
        /// Reconfigures the springs with new parameters.
        /// </summary>
        public void Reconfigure(float frequency, float damping, float response)
        {
            _frequency = frequency;
            _damping = damping;
            _response = response;
            _useCustomParameters = true;
            ConfigureSprings();
        }
        
        /// <summary>
        /// Reconfigures the springs with a preset.
        /// </summary>
        public void Reconfigure(SpringPreset preset)
        {
            _preset = preset;
            _useCustomParameters = false;
            ConfigureSprings();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _initialized)
            {
                ConfigureSprings();
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _target.position);
                Gizmos.DrawWireSphere(_target.position, 0.1f);
            }
        }
#endif
    }
}
