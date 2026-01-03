using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Eraflo.Catalyst.ProceduralAnimation.Jobs;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.IK
{
    /// <summary>
    /// Optimizes IK solutions to stay close to a rest/comfort pose.
    /// Prevents unnatural poses like hyperextended joints or awkward angles.
    /// </summary>
    [System.Serializable]
    public class ComfortPoseOptimizer
    {
        [Header("Rest Pose")]
        [Tooltip("Reference pose transforms (rest position).")]
        [SerializeField] private Transform[] _restPoseReferences;
        
        [Tooltip("Weight of comfort preference (0 = pure IK, 1 = heavy comfort bias).")]
        [SerializeField, Range(0f, 1f)] private float _comfortWeight = 0.3f;
        
        [Header("Joint Limits")]
        [Tooltip("Maximum angular deviation from rest pose per joint (degrees).")]
        [SerializeField] private float _maxDeviation = 90f;
        
        [Tooltip("Apply soft limits (gradual falloff) vs hard limits.")]
        [SerializeField] private bool _useSoftLimits = true;
        
        [Tooltip("Softness of limits (higher = softer transition).")]
        [SerializeField] private float _limitSoftness = 0.3f;
        
        // Native arrays for job
        private NativeArray<quaternion> _restRotations;
        private NativeArray<float> _jointWeights;
        private bool _initialized;
        
        /// <summary>
        /// Comfort weight (0 = pure IK, 1 = heavy comfort bias).
        /// </summary>
        public float ComfortWeight
        {
            get => _comfortWeight;
            set => _comfortWeight = math.saturate(value);
        }
        
        /// <summary>
        /// Initializes with rest pose from current bone rotations.
        /// </summary>
        public void Initialize(Transform[] bones, float[] perJointWeights = null)
        {
            Dispose();
            
            if (bones == null || bones.Length == 0) return;
            
            _restRotations = new NativeArray<quaternion>(bones.Length, Allocator.Persistent);
            _jointWeights = new NativeArray<float>(bones.Length, Allocator.Persistent);
            
            for (int i = 0; i < bones.Length; i++)
            {
                _restRotations[i] = bones[i] != null ? (quaternion)bones[i].rotation : quaternion.identity;
                _jointWeights[i] = perJointWeights != null && i < perJointWeights.Length 
                    ? perJointWeights[i] 
                    : 1f;
            }
            
            _initialized = true;
        }
        
        /// <summary>
        /// Captures current pose as the new rest pose.
        /// </summary>
        public void CaptureRestPose(Transform[] bones)
        {
            if (!_initialized || bones == null) return;
            
            for (int i = 0; i < math.min(bones.Length, _restRotations.Length); i++)
            {
                if (bones[i] != null)
                {
                    _restRotations[i] = bones[i].rotation;
                }
            }
        }
        
        /// <summary>
        /// Optimizes rotations toward the rest pose while respecting IK targets.
        /// </summary>
        public void Optimize(NativeArray<quaternion> rotations, float weight = -1f)
        {
            if (!_initialized) return;
            
            float w = weight >= 0 ? weight : _comfortWeight;
            if (w <= 0) return;
            
            float maxRad = math.radians(_maxDeviation);
            
            for (int i = 0; i < math.min(rotations.Length, _restRotations.Length); i++)
            {
                quaternion current = rotations[i];
                quaternion rest = _restRotations[i];
                float jointWeight = _jointWeights[i];
                
                // Blend toward rest pose
                quaternion blended = math.slerp(current, rest, w * jointWeight);
                
                // Apply angular limits
                if (_maxDeviation < 180f)
                {
                    blended = ApplyAngularLimit(blended, rest, maxRad);
                }
                
                rotations[i] = blended;
            }
        }
        
        /// <summary>
        /// Schedules optimization as a Burst job.
        /// </summary>
        public JobHandle ScheduleOptimize(
            NativeArray<quaternion> rotations, 
            float weight,
            JobHandle dependency = default)
        {
            if (!_initialized) return dependency;
            
            var job = new ComfortOptimizeJob
            {
                Rotations = rotations,
                RestRotations = _restRotations,
                JointWeights = _jointWeights,
                ComfortWeight = weight >= 0 ? weight : _comfortWeight,
                MaxDeviationRadians = math.radians(_maxDeviation),
                UseSoftLimits = _useSoftLimits,
                LimitSoftness = _limitSoftness
            };
            
            return job.Schedule(rotations.Length, 8, dependency);
        }
        
        private quaternion ApplyAngularLimit(quaternion rotation, quaternion reference, float maxRadians)
        {
            // Calculate angular difference
            quaternion diff = math.mul(rotation, math.conjugate(reference));
            
            // Ensure shortest path
            if (diff.value.w < 0)
                diff = new quaternion(-diff.value);
            
            // Get angle
            float sinHalf = math.length(diff.value.xyz);
            if (sinHalf < 0.0001f) return rotation;
            
            float angle = 2f * math.asin(math.clamp(sinHalf, 0f, 1f));
            
            if (angle <= maxRadians) return rotation;
            
            // Clamp angle
            float3 axis = diff.value.xyz / sinHalf;
            float clampedAngle = maxRadians;
            
            if (_useSoftLimits)
            {
                // Soft limit: exponential falloff
                float excess = angle - maxRadians;
                clampedAngle = maxRadians + excess * _limitSoftness;
                clampedAngle = math.min(clampedAngle, maxRadians * 1.5f);
            }
            
            quaternion clampedDiff = quaternion.AxisAngle(axis, clampedAngle);
            return math.mul(clampedDiff, reference);
        }
        
        public void Dispose()
        {
            if (_restRotations.IsCreated) _restRotations.Dispose();
            if (_jointWeights.IsCreated) _jointWeights.Dispose();
            _initialized = false;
        }
        
        /// <summary>
        /// Gets angular deviation from rest pose for a joint (in degrees).
        /// </summary>
        public float GetDeviation(int jointIndex, quaternion currentRotation)
        {
            if (!_initialized || jointIndex < 0 || jointIndex >= _restRotations.Length)
                return 0f;
            
            quaternion diff = math.mul(currentRotation, math.conjugate(_restRotations[jointIndex]));
            if (diff.value.w < 0) diff = new quaternion(-diff.value);
            
            float sinHalf = math.length(diff.value.xyz);
            return math.degrees(2f * math.asin(math.clamp(sinHalf, 0f, 1f)));
        }
    }
}

