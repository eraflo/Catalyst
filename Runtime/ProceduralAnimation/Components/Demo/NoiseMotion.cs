using Unity.Mathematics;
using UnityEngine;
using Eraflo.Catalyst.Noise;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.Demo
{
    /// <summary>
    /// Applies noise-based displacement to a transform for idle/breathing animations.
    /// </summary>
    [AddComponentMenu("Catalyst/Procedural Animation/Noise Motion")]
    public class NoiseMotion : MonoBehaviour
    {
        [Header("Noise Settings")]
        [Tooltip("Type of noise motion.")]
        [SerializeField] private NoiseMotionType _motionType = NoiseMotionType.Breathing;
        
        [Tooltip("Frequency of the noise (lower = smoother, larger patterns).")]
        [SerializeField, Range(0.01f, 5f)] private float _frequency = 0.5f;
        
        [Tooltip("Amplitude of the displacement.")]
        [SerializeField, Range(0f, 2f)] private float _amplitude = 0.1f;
        
        [Tooltip("Speed of noise evolution over time.")]
        [SerializeField, Range(0f, 5f)] private float _timeScale = 0.5f;
        
        [Header("Axes")]
        [Tooltip("Apply noise to X axis.")]
        [SerializeField] private bool _applyX = true;
        
        [Tooltip("Apply noise to Y axis.")]
        [SerializeField] private bool _applyY = true;
        
        [Tooltip("Apply noise to Z axis.")]
        [SerializeField] private bool _applyZ = false;
        
        [Tooltip("Apply noise to rotation.")]
        [SerializeField] private bool _applyRotation = false;
        
        [Tooltip("Rotation amplitude in degrees.")]
        [SerializeField, Range(0f, 45f)] private float _rotationAmplitude = 5f;
        
        [Header("Options")]
        [Tooltip("Use local space.")]
        [SerializeField] private bool _useLocalSpace = true;
        
        [Tooltip("Unique seed offset for this instance.")]
        [SerializeField] private int _seed = 0;
        
        private float3 _originalPosition;
        private quaternion _originalRotation;
        private NoiseField _noiseField;
        private float3 _seedOffset;
        
        private void Start()
        {
            // Store original transform
            _originalPosition = _useLocalSpace ? (float3)transform.localPosition : (float3)transform.position;
            _originalRotation = _useLocalSpace ? (quaternion)transform.localRotation : (quaternion)transform.rotation;
            
            // Configure noise field based on motion type
            _noiseField = _motionType switch
            {
                NoiseMotionType.Breathing => NoiseField.Breathing,
                NoiseMotionType.Wind => NoiseField.Wind,
                NoiseMotionType.Trembling => NoiseField.Trembling,
                NoiseMotionType.Custom => NoiseField.Default,
                _ => NoiseField.Default
            };
            
            // Override with custom settings
            _noiseField.Frequency = _frequency;
            _noiseField.Amplitude = _amplitude;
            _noiseField.TimeScale = _timeScale;
            
            // Generate seed offset
            if (_seed == 0)
            {
                _seed = GetInstanceID();
            }
            var random = new Unity.Mathematics.Random((uint)_seed);
            _seedOffset = random.NextFloat3() * 1000f;
            _noiseField.Offset = _seedOffset;
        }
        
        private void Update()
        {
            _noiseField.Update(Time.deltaTime);
            
            // Sample noise
            float3 noise = _noiseField.Sample3D(_originalPosition);
            
            // Apply axis mask
            float3 displacement = float3.zero;
            if (_applyX) displacement.x = noise.x;
            if (_applyY) displacement.y = noise.y;
            if (_applyZ) displacement.z = noise.z;
            
            // Apply position
            float3 newPos = _originalPosition + displacement;
            if (_useLocalSpace)
                transform.localPosition = newPos;
            else
                transform.position = newPos;
            
            // Apply rotation
            if (_applyRotation)
            {
                float rotNoise = _noiseField.Sample(_originalPosition + new float3(500f, 0f, 0f));
                float3 rotOffset = new float3(
                    _applyX ? rotNoise : 0f,
                    _applyY ? rotNoise : 0f,
                    _applyZ ? rotNoise : 0f
                ) * _rotationAmplitude;
                
                quaternion rotation = math.mul(
                    _originalRotation,
                    quaternion.Euler(math.radians(rotOffset))
                );
                
                if (_useLocalSpace)
                    transform.localRotation = rotation;
                else
                    transform.rotation = rotation;
            }
        }
        
        /// <summary>
        /// Resets to original position.
        /// </summary>
        public void ResetToOriginal()
        {
            if (_useLocalSpace)
            {
                transform.localPosition = _originalPosition;
                transform.localRotation = _originalRotation;
            }
            else
            {
                transform.position = _originalPosition;
                transform.rotation = _originalRotation;
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                _noiseField.Frequency = _frequency;
                _noiseField.Amplitude = _amplitude;
                _noiseField.TimeScale = _timeScale;
            }
        }
#endif
    }
    
    /// <summary>
    /// Types of noise motion presets.
    /// </summary>
    public enum NoiseMotionType
    {
        /// <summary>Slow, subtle motion for idle/breathing.</summary>
        Breathing,
        /// <summary>Directional, flowing motion for wind effects.</summary>
        Wind,
        /// <summary>Fast, small motion for trembling/shaking.</summary>
        Trembling,
        /// <summary>Use custom frequency/amplitude/timeScale settings.</summary>
        Custom
    }
}
