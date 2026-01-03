using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Eraflo.Catalyst.ProceduralAnimation;
using Eraflo.Catalyst.ProceduralAnimation.Jobs;
using Eraflo.Catalyst.ProceduralAnimation.Perception;
using Eraflo.Catalyst.ProceduralAnimation.SignalProcessing;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.Locomotion
{
    /// <summary>
    /// Verlet integration-based spine/tail simulation.
    /// Uses "Follow the Leader" approach with distance constraints.
    /// Suitable for tails, tentacles, snakes, and flexible spines.
    /// </summary>
    [AddComponentMenu("Catalyst/Procedural Animation/Verlet Spine")]
    public class VerletSpine : MonoBehaviour, IProceduralAnimationJob
    {
        [Header("Chain")]
        [Tooltip("Root/leader of the chain (the part that leads movement).")]
        [SerializeField] private Transform _leader;
        
        [Tooltip("Bones in the chain from root to tip.")]
        [SerializeField] private Transform[] _bones;
        
        [Header("Physics")]
        [Tooltip("Damping factor (0 = no friction, 1 = full stop).")]
        [SerializeField, Range(0f, 1f)] private float _damping = 0.1f;
        
        [Tooltip("Gravity influence.")]
        [SerializeField] private float _gravity = -2f;
        
        [Tooltip("Stiffness of distance constraints (higher = more rigid).")]
        [SerializeField, Range(1, 10)] private int _constraintIterations = 3;
        
        [Header("Follow Leader")]
        [Tooltip("How quickly bones follow the leader.")]
        [SerializeField, Range(0f, 1f)] private float _followStrength = 0.8f;
        
        [Tooltip("Delay before following (creates wave effect).")]
        [SerializeField, Range(0f, 0.5f)] private float _followDelay = 0.05f;
        
        [Header("Noise")]
        [Tooltip("Add noise-based wiggling.")]
        [SerializeField] private bool _enableNoise = true;
        
        [Tooltip("Noise frequency.")]
        [SerializeField] private float _noiseFrequency = 2f;
        
        [Tooltip("Noise amplitude.")]
        [SerializeField] private float _noiseAmplitude = 0.1f;
        
        /// <summary>
        /// Whether noise-based wiggling is enabled.
        /// </summary>
        public bool EnableNoise { get => _enableNoise; set => _enableNoise = value; }
        
        /// <summary>
        /// Amplitude of the noise wiggling.
        /// </summary>
        public float NoiseAmplitude { get => _noiseAmplitude; set => _noiseAmplitude = value; }
        
        // Native arrays for job
        private NativeArray<float3> _positions;
        private NativeArray<float3> _previousPositions;
        private NativeArray<float> _boneLengths;
        private NativeArray<float3> _outputPositions;
        
        private float _time;
        private bool _initialized;
        private bool _needsUpdate;
        private float _deltaTime;
        private InertializationBlender _leaderInertializer;
        private float3 _smoothedLeaderPosition;
        
        #region IProceduralAnimationJob Implementation
        
        public bool NeedsUpdate => _needsUpdate && _initialized && _bones != null && _bones.Length > 1;
        
        public void Prepare(float deltaTime)
        {
            _deltaTime = deltaTime;
            _time += deltaTime;
            
            // Update leader inertialization
            _leaderInertializer.Update(deltaTime);
            
            // First bone follows the leader (with optional inertialization)
            if (_leader != null)
            {
                float3 rawLeaderPos = _leader.position;
                _smoothedLeaderPosition = _leaderInertializer.ApplyPosition(rawLeaderPos);
                _positions[0] = _smoothedLeaderPosition;
            }
            else if (_bones[0] != null)
            {
                _positions[0] = _bones[0].position;
            }
        }
        
        public JobHandle Schedule(JobHandle dependency)
        {
            var verletJob = new VerletSpineJob
            {
                Positions = _positions,
                PreviousPositions = _previousPositions,
                BoneLengths = _boneLengths,
                OutputPositions = _outputPositions,
                DeltaTime = _deltaTime,
                Damping = _damping,
                Gravity = _gravity,
                ConstraintIterations = _constraintIterations,
                FollowStrength = _followStrength,
                FollowDelay = _followDelay,
                EnableNoise = _enableNoise,
                NoiseFrequency = _noiseFrequency,
                NoiseAmplitude = _noiseAmplitude,
                Time = _time
            };
            
            return verletJob.Schedule(dependency);
        }
        
        public void Apply()
        {
            // Apply positions to transforms and calculate rotations
            for (int i = 0; i < _bones.Length; i++)
            {
                if (_bones[i] != null)
                {
                    _bones[i].position = _outputPositions[i];
                    
                    // Calculate rotation to look at next bone
                    if (i < _bones.Length - 1 && _bones[i + 1] != null)
                    {
                        Vector3 dir = _outputPositions[i + 1] - _outputPositions[i];
                        if (dir.sqrMagnitude > 0.0001f)
                        {
                            _bones[i].rotation = Quaternion.LookRotation(dir, Vector3.up);
                        }
                    }
                }
            }
            
            // Copy current to previous for next frame
            _previousPositions.CopyFrom(_positions);
            _positions.CopyFrom(_outputPositions);
        }
        
        #endregion
        
        private void Awake()
        {
            Initialize();
        }
        
        private void OnEnable()
        {
            if (_initialized)
            {
                AnimationJobManager.Instance?.Register(this);
                _needsUpdate = true;
            }
        }
        
        private void OnDisable()
        {
            _needsUpdate = false;
            AnimationJobManager.Instance?.Unregister(this);
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        private void Initialize()
        {
            if (_initialized) return;
            if (_bones == null || _bones.Length < 2) return;
            
            int count = _bones.Length;
            
            _positions = new NativeArray<float3>(count, Allocator.Persistent);
            _previousPositions = new NativeArray<float3>(count, Allocator.Persistent);
            _boneLengths = new NativeArray<float>(count - 1, Allocator.Persistent);
            _outputPositions = new NativeArray<float3>(count, Allocator.Persistent);
            
            // Initialize positions
            for (int i = 0; i < count; i++)
            {
                if (_bones[i] != null)
                {
                    _positions[i] = _bones[i].position;
                    _previousPositions[i] = _positions[i];
                    _outputPositions[i] = _positions[i];
                }
            }
            
            // Calculate bone lengths
            for (int i = 0; i < count - 1; i++)
            {
                if (_bones[i] != null && _bones[i + 1] != null)
                {
                    _boneLengths[i] = Vector3.Distance(_bones[i].position, _bones[i + 1].position);
                }
            }
            
            _initialized = true;
            _needsUpdate = true;
            
            // Initialize leader inertialization
            _leaderInertializer = InertializationBlender.Create(0.1f);
            _smoothedLeaderPosition = _leader != null ? (float3)_leader.position : float3.zero;
            
            AnimationJobManager.Instance?.Register(this);
        }
        
        public void Dispose()
        {
            AnimationJobManager.Instance?.Unregister(this);
            
            if (_positions.IsCreated) _positions.Dispose();
            if (_previousPositions.IsCreated) _previousPositions.Dispose();
            if (_boneLengths.IsCreated) _boneLengths.Dispose();
            if (_outputPositions.IsCreated) _outputPositions.Dispose();
            
            _initialized = false;
        }
        
        /// <summary>
        /// Sets up the spine from a SpineChain.
        /// </summary>
        public void SetupFromSpine(SpineChain spine)
        {
            if (spine == null || spine.Bones == null) return;
            
            Dispose();
            _bones = spine.Bones;
            _leader = spine.Bones.Length > 0 ? spine.Bones[0] : null;
            Initialize();
        }
        
        /// <summary>
        /// Sets up the spine from a SpineChain. Alias for SetupFromSpine.
        /// </summary>
        public void SetupFromChain(SpineChain chain) => SetupFromSpine(chain);
        
        /// <summary>
        /// Sets the leader transform with smooth transition.
        /// </summary>
        public void SetLeader(Transform leader, float transitionTime = 0.15f)
        {
            if (_initialized && leader != null && _leader != null)
            {
                _leaderInertializer.SetHalfLife(transitionTime);
                _leaderInertializer.TransitionPosition(_leader.position, leader.position);
            }
            _leader = leader;
        }
        
        /// <summary>
        /// Applies an impulse force at a position.
        /// </summary>
        public void ApplyImpulse(Vector3 force, int boneIndex)
        {
            if (!_initialized || boneIndex < 0 || boneIndex >= _positions.Length) return;
            
            // Modify previous position to create velocity
            _previousPositions[boneIndex] = _previousPositions[boneIndex] - (float3)force * _deltaTime;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_bones == null || _bones.Length < 2) return;
            
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _bones.Length - 1; i++)
            {
                if (_bones[i] != null && _bones[i + 1] != null)
                {
                    Gizmos.DrawLine(_bones[i].position, _bones[i + 1].position);
                    Gizmos.DrawWireSphere(_bones[i].position, 0.02f);
                }
            }
            
            if (_bones[_bones.Length - 1] != null)
            {
                Gizmos.DrawWireSphere(_bones[_bones.Length - 1].position, 0.02f);
            }
        }
#endif
    }
}

