using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Eraflo.Catalyst.ProceduralAnimation;
using Eraflo.Catalyst.ProceduralAnimation.Jobs;
using Eraflo.Catalyst.ProceduralAnimation.Perception;
using Eraflo.Catalyst.ProceduralAnimation.SignalProcessing;
using Eraflo.Catalyst.ProceduralAnimation.Solvers;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.Locomotion
{
    /// <summary>
    /// Procedural locomotion using the job system.
    /// Implements IProceduralAnimationJob for integration with AnimationJobManager.
    /// </summary>
    [AddComponentMenu("Catalyst/Procedural Animation/Procedural Locomotion")]
    [RequireComponent(typeof(ProceduralAnimator))]
    public class ProceduralLocomotion : MonoBehaviour, IProceduralAnimationJob
    {
        [Header("Movement")]
        [Tooltip("Movement input (normalized direction).")]
        [SerializeField] private Vector3 _movementInput;
        
        [Tooltip("Movement speed in m/s.")]
        [SerializeField] private float _speed = 2f;
        
        [Header("Gait")]
        [Tooltip("Gait cycle duration.")]
        [SerializeField] private float _gaitDuration = 0.8f;
        
        [Tooltip("Stance ratio (0-1).")]
        [SerializeField, Range(0.3f, 0.8f)] private float _stanceRatio = 0.6f;
        
        [Tooltip("Step height.")]
        [SerializeField] private float _stepHeight = 0.15f;
        
        [Header("Balance")]
        [Tooltip("Target height above ground.")]
        [SerializeField] private float _targetHeight = 1f;
        
        [Tooltip("Bobbing amplitude.")]
        [SerializeField] private float _bobbingAmplitude = 0.02f;
        
        [Tooltip("Movement lean angle.")]
        [SerializeField, Range(0f, 20f)] private float _leanAngle = 5f;
        
        [Header("Spring Settings")]
        [Tooltip("Foot spring frequency.")]
        [SerializeField] private float _footSpringFrequency = 5f;
        
        [Tooltip("Foot spring damping.")]
        [SerializeField] private float _footSpringDamping = 0.8f;
        
        // Native arrays
        private NativeArray<float3> _footTargets;
        private NativeArray<float3> _footPositions;
        private NativeArray<float3> _footVelocities;
        private NativeArray<bool> _footInSwing;
        private NativeArray<float> _footSwingProgress;
        private NativeArray<float3> _footSwingStart;
        private NativeArray<float> _footStepHeights;
        private NativeArray<bool> _footGrounded;
        
        private NativeArray<float3> _bodyPosition;
        private NativeArray<quaternion> _bodyRotation;
        
        // FABRIK IK arrays for leg solving
        private NativeArray<float3> _legJointPositions;  // All joints for all legs
        private NativeArray<float> _legBoneLengths;
        private NativeArray<quaternion> _legRotations;
        private NativeArray<quaternion> _legOriginalRotations;
        private NativeArray<int2> _legChainRanges;       // Start index + length per leg
        private NativeArray<float3> _legRootPositions;
        private NativeArray<float3> _legUpVectors;
        private TransformAccessArray _legTransformAccess;
        private Transform[] _allLegBones;
        
        // Runtime
        private ProceduralAnimator _animator;
        private LimbChain[] _legs;
        private float[] _legPhases;
        private GaitCycle _gaitCycle;
        private float3 _velocity;
        private bool _initialized;
        private bool _needsUpdate;
        private float _deltaTime;
        private bool[] _wasInStance;
        
        private SpringCoefficients _springConfig;
        private InertializationBlender _velocityInertializer;
        private float3 _smoothedVelocity;
        private int _totalLegBones;
        
        [Header("IK")]
        [Tooltip("Enable FABRIK IK for leg bones.")]
        [SerializeField] private bool _enableLegIK = true;
        
        [Tooltip("FABRIK solver iterations.")]
        [SerializeField, Range(1, 15)] private int _ikIterations = 8;
        
        /// <summary>
        /// Movement input direction.
        /// </summary>
        public Vector3 MovementInput
        {
            get => _movementInput;
            set => _movementInput = value;
        }
        
        /// <summary>
        /// Movement speed.
        /// </summary>
        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }
        
        /// <summary>
        /// Current velocity.
        /// </summary>
        public float3 Velocity => _velocity;
        
        /// <summary>
        /// Current gait phase (0-1).
        /// </summary>
        public float GaitPhase => _gaitCycle?.Phase ?? 0f;
        
        #region IProceduralAnimationJob Implementation
        
        public bool NeedsUpdate => _needsUpdate && _initialized;
        
        public void Prepare(float deltaTime)
        {
            _deltaTime = deltaTime;
            
            // Update velocity with inertialization
            float3 inputDir = math.normalizesafe(new float3(_movementInput.x, 0, _movementInput.z));
            float3 targetVelocity = inputDir * _speed;
            
            _velocityInertializer.Update(deltaTime);
            _smoothedVelocity = _velocityInertializer.ApplyPosition(targetVelocity);
            _velocity = _smoothedVelocity;
            
            // Update gait cycle
            float movementSpeed = math.length(_velocity);
            _gaitCycle.Update(deltaTime, movementSpeed);
            
            // Calculate per-leg data
            for (int i = 0; i < _legs.Length; i++)
            {
                float legPhaseOffset = _legPhases[i];
                bool inStance = _gaitCycle.IsInStance(legPhaseOffset);
                bool inSwing = _gaitCycle.IsInSwing(legPhaseOffset);
                
                // Detect phase transitions
                if (inStance && !_wasInStance[i])
                {
                    // Landing - set target to current position
                    _footSwingStart[i] = _footPositions[i];
                }
                else if (inSwing && _wasInStance[i])
                {
                    // Lifting off - record start position
                    _footSwingStart[i] = _footPositions[i];
                }
                
                _wasInStance[i] = inStance;
                
                // Calculate target position (raycast would go here, simplified for now)
                float3 hipPos = _legs[i].Root != null ? (float3)_legs[i].Root.position : (float3)transform.position;
                float3 moveDir = math.length(_velocity) > 0.1f 
                    ? math.normalizesafe(_velocity) 
                    : new float3(0, 0, 0);
                
                _footTargets[i] = hipPos + moveDir * 0.3f;
                _footTargets[i] = new float3(_footTargets[i].x, 0f, _footTargets[i].z); // Ground level
                
                _footInSwing[i] = inSwing;
                _footGrounded[i] = inStance;
                _footSwingProgress[i] = _gaitCycle.GetSwingProgress(legPhaseOffset);
                _footStepHeights[i] = _gaitCycle.StepHeight;
                
                // Prepare FABRIK data
                if (_enableLegIK && _legRootPositions.IsCreated)
                {
                    _legRootPositions[i] = hipPos;
                    
                    // Copy current leg bone positions for FABRIK
                    var range = _legChainRanges[i];
                    for (int j = 0; j < range.y; j++)
                    {
                        int idx = range.x + j;
                        if (_allLegBones[idx] != null)
                        {
                            _legJointPositions[idx] = _allLegBones[idx].position;
                            _legOriginalRotations[idx] = _allLegBones[idx].rotation;
                        }
                    }
                }
            }
        }
        
        public JobHandle Schedule(JobHandle dependency)
        {
            // Schedule foot placement job
            var footJob = new FootPlacementJob
            {
                TargetPositions = _footTargets,
                IsInSwing = _footInSwing,
                SwingProgress = _footSwingProgress,
                SwingStartPositions = _footSwingStart,
                StepHeights = _footStepHeights,
                Positions = _footPositions,
                Velocities = _footVelocities,
                SpringConfig = _springConfig,
                DeltaTime = _deltaTime
            };
            
            var footHandle = footJob.Schedule(_legs.Length, 4, dependency);
            
            // Schedule FABRIK IK for leg bones
            JobHandle ikHandle = footHandle;
            if (_enableLegIK && _totalLegBones > 0)
            {
                // Copy foot positions to FABRIK targets (in Prepare, we already set hip positions)
                // FABRIK job solves each leg chain
                var fabrikJob = new FABRIKJob
                {
                    ChainRanges = _legChainRanges,
                    RootPositions = _legRootPositions,
                    TargetPositions = _footPositions,  // Foot positions are the targets
                    BoneLengths = _legBoneLengths,
                    JointPositions = _legJointPositions,
                    MaxIterations = _ikIterations,
                    Tolerance = 0.001f
                };
                
                var fabrikHandle = fabrikJob.Schedule(_legs.Length, dependency: footHandle);
                
                // Convert positions to rotations
                var rotationJob = new PositionToRotationJob
                {
                    ChainRanges = _legChainRanges,
                    JointPositions = _legJointPositions,
                    UpVectors = _legUpVectors,
                    Rotations = _legRotations
                };
                
                ikHandle = rotationJob.Schedule(_totalLegBones, 4, fabrikHandle);
            }
            
            // Schedule body balance job
            var balanceJob = new BodyBalanceJob
            {
                FootPositions = _footPositions,
                FootGrounded = _footGrounded,
                Velocity = _velocity,
                GaitPhase = _gaitCycle.Phase,
                TargetHeight = _targetHeight,
                BobbingAmplitude = _bobbingAmplitude,
                MaxTiltAngle = 15f,
                MovementLeanAngle = _leanAngle,
                OutputPosition = _bodyPosition,
                OutputRotation = _bodyRotation
            };
            
            return balanceJob.Schedule(ikHandle);
        }
        
        public void Apply()
        {
            // Apply foot positions to effectors
            for (int i = 0; i < _legs.Length; i++)
            {
                if (_legs[i].Effector != null)
                {
                    _legs[i].Effector.position = _footPositions[i];
                }
            }
            
            // Apply leg bone rotations from FABRIK
            if (_enableLegIK && _totalLegBones > 0)
            {
                for (int i = 0; i < _totalLegBones; i++)
                {
                    if (_allLegBones[i] != null)
                    {
                        _allLegBones[i].rotation = _legRotations[i];
                    }
                }
            }
            
            // Note: Body position is available in _bodyPosition[0] for use by other systems
        }
        
        #endregion
        
        private void Awake()
        {
            _animator = GetComponent<ProceduralAnimator>();
        }
        
        private void Start()
        {
            if (_animator.IsAnalyzed)
            {
                Initialize();
            }
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
        
        private void Update()
        {
            // Initialize if analyzer completed
            if (!_initialized && _animator.IsAnalyzed)
            {
                Initialize();
            }
        }
        
        private void Initialize()
        {
            if (_initialized) return;
            
            var topology = _animator.Topology;
            _legs = topology.GetLegs();
            
            if (_legs.Length == 0)
            {
                Debug.LogWarning($"[ProceduralLocomotion] No legs found on {gameObject.name}");
                return;
            }
            
            int legCount = _legs.Length;
            
            // Allocate native arrays
            _footTargets = new NativeArray<float3>(legCount, Allocator.Persistent);
            _footPositions = new NativeArray<float3>(legCount, Allocator.Persistent);
            _footVelocities = new NativeArray<float3>(legCount, Allocator.Persistent);
            _footInSwing = new NativeArray<bool>(legCount, Allocator.Persistent);
            _footSwingProgress = new NativeArray<float>(legCount, Allocator.Persistent);
            _footSwingStart = new NativeArray<float3>(legCount, Allocator.Persistent);
            _footStepHeights = new NativeArray<float>(legCount, Allocator.Persistent);
            _footGrounded = new NativeArray<bool>(legCount, Allocator.Persistent);
            
            _bodyPosition = new NativeArray<float3>(1, Allocator.Persistent);
            _bodyRotation = new NativeArray<quaternion>(1, Allocator.Persistent);
            
            // Initialize with current positions
            _legPhases = new float[legCount];
            _wasInStance = new bool[legCount];
            
            for (int i = 0; i < legCount; i++)
            {
                _legPhases[i] = _legs[i].GaitPhase;
                _wasInStance[i] = true;
                
                if (_legs[i].Effector != null)
                {
                    _footPositions[i] = _legs[i].Effector.position;
                    _footSwingStart[i] = _footPositions[i];
                }
            }
            
            _springConfig = SpringCoefficients.Create(_footSpringFrequency, _footSpringDamping, 0f);
            
            // Initialize FABRIK IK arrays for leg solving
            if (_enableLegIK)
            {
                InitializeLegIK(legCount);
            }
            
            // Initialize gait cycle
            _gaitCycle = new GaitCycle
            {
                CycleDuration = _gaitDuration,
                StanceDutyFactor = _stanceRatio,
                StepHeight = _stepHeight
            };
            
            // Initialize velocity inertialization
            _velocityInertializer = InertializationBlender.Create(0.1f);
            
            _initialized = true;
            _needsUpdate = true;
            
            AnimationJobManager.Instance?.Register(this);
            
            Debug.Log($"[ProceduralLocomotion] Initialized with {legCount} legs using GaitCycle + FABRIK IK");
        }
        
        private void InitializeLegIK(int legCount)
        {
            // Count total bones across all legs
            _totalLegBones = 0;
            foreach (var leg in _legs)
            {
                _totalLegBones += leg.Bones?.Length ?? 0;
            }
            
            if (_totalLegBones == 0) return;
            
            // Allocate arrays
            _legJointPositions = new NativeArray<float3>(_totalLegBones, Allocator.Persistent);
            _legBoneLengths = new NativeArray<float>(_totalLegBones, Allocator.Persistent);
            _legRotations = new NativeArray<quaternion>(_totalLegBones, Allocator.Persistent);
            _legOriginalRotations = new NativeArray<quaternion>(_totalLegBones, Allocator.Persistent);
            _legChainRanges = new NativeArray<int2>(legCount, Allocator.Persistent);
            _legRootPositions = new NativeArray<float3>(legCount, Allocator.Persistent);
            _legUpVectors = new NativeArray<float3>(legCount, Allocator.Persistent);
            
            // Build bone list and chain ranges
            _allLegBones = new Transform[_totalLegBones];
            int boneIndex = 0;
            
            for (int i = 0; i < legCount; i++)
            {
                var leg = _legs[i];
                int chainStart = boneIndex;
                int chainLength = leg.Bones?.Length ?? 0;
                
                _legChainRanges[i] = new int2(chainStart, chainLength);
                _legUpVectors[i] = new float3(0, 0, 1);  // Default forward for knee bend
                
                if (leg.Bones != null)
                {
                    for (int j = 0; j < leg.Bones.Length; j++)
                    {
                        var bone = leg.Bones[j];
                        _allLegBones[boneIndex] = bone;
                        
                        if (bone != null)
                        {
                            _legJointPositions[boneIndex] = bone.position;
                            _legRotations[boneIndex] = bone.rotation;
                            _legOriginalRotations[boneIndex] = bone.rotation;
                            
                            // Calculate bone length (to next bone)
                            if (j < leg.Bones.Length - 1 && leg.Bones[j + 1] != null)
                            {
                                _legBoneLengths[boneIndex] = Vector3.Distance(bone.position, leg.Bones[j + 1].position);
                            }
                        }
                        
                        boneIndex++;
                    }
                }
            }
            
            _legTransformAccess = new TransformAccessArray(_allLegBones);
        }
        
        /// <summary>
        /// Sets up locomotion from a BodyTopology.
        /// Called by ProceduralAnimator.SetupLocomotion().
        /// </summary>
        public void SetupFromTopology(BodyTopology topology)
        {
            if (topology == null || topology.GetLegs().Length == 0)
            {
                Debug.LogWarning("[ProceduralLocomotion] Cannot setup: no topology or legs.");
                return;
            }
            
            // Force re-initialization with new topology
            if (_initialized)
            {
                Dispose();
            }
            
            _legs = topology.GetLegs();
            
            // Copy leg phases from topology
            int legCount = _legs.Length;
            _legPhases = new float[legCount];
            _wasInStance = new bool[legCount];
            
            for (int i = 0; i < legCount; i++)
            {
                _legPhases[i] = _legs[i].GaitPhase;
            }
            
            // Continue with standard initialization
            _footTargets = new NativeArray<float3>(legCount, Allocator.Persistent);
            _footPositions = new NativeArray<float3>(legCount, Allocator.Persistent);
            _footVelocities = new NativeArray<float3>(legCount, Allocator.Persistent);
            _footInSwing = new NativeArray<bool>(legCount, Allocator.Persistent);
            _footSwingProgress = new NativeArray<float>(legCount, Allocator.Persistent);
            _footSwingStart = new NativeArray<float3>(legCount, Allocator.Persistent);
            _footStepHeights = new NativeArray<float>(legCount, Allocator.Persistent);
            _footGrounded = new NativeArray<bool>(legCount, Allocator.Persistent);
            _bodyPosition = new NativeArray<float3>(1, Allocator.Persistent);
            _bodyRotation = new NativeArray<quaternion>(1, Allocator.Persistent);
            
            for (int i = 0; i < legCount; i++)
            {
                _wasInStance[i] = true;
                if (_legs[i].Effector != null)
                {
                    _footPositions[i] = _legs[i].Effector.position;
                    _footSwingStart[i] = _footPositions[i];
                }
            }
            
            _springConfig = SpringCoefficients.Create(_footSpringFrequency, _footSpringDamping, 0f);
            _initialized = true;
            _needsUpdate = true;
            
            AnimationJobManager.Instance?.Register(this);
            
            Debug.Log($"[ProceduralLocomotion] Setup from topology with {legCount} legs");
        }
        
        public void Dispose()
        {
            AnimationJobManager.Instance?.Unregister(this);
            
            if (_footTargets.IsCreated) _footTargets.Dispose();
            if (_footPositions.IsCreated) _footPositions.Dispose();
            if (_footVelocities.IsCreated) _footVelocities.Dispose();
            if (_footInSwing.IsCreated) _footInSwing.Dispose();
            if (_footSwingProgress.IsCreated) _footSwingProgress.Dispose();
            if (_footSwingStart.IsCreated) _footSwingStart.Dispose();
            if (_footStepHeights.IsCreated) _footStepHeights.Dispose();
            if (_footGrounded.IsCreated) _footGrounded.Dispose();
            if (_bodyPosition.IsCreated) _bodyPosition.Dispose();
            if (_bodyRotation.IsCreated) _bodyRotation.Dispose();
            
            // Dispose FABRIK IK arrays
            if (_legJointPositions.IsCreated) _legJointPositions.Dispose();
            if (_legBoneLengths.IsCreated) _legBoneLengths.Dispose();
            if (_legRotations.IsCreated) _legRotations.Dispose();
            if (_legOriginalRotations.IsCreated) _legOriginalRotations.Dispose();
            if (_legChainRanges.IsCreated) _legChainRanges.Dispose();
            if (_legRootPositions.IsCreated) _legRootPositions.Dispose();
            if (_legUpVectors.IsCreated) _legUpVectors.Dispose();
            if (_legTransformAccess.isCreated) _legTransformAccess.Dispose();
            
            _initialized = false;
        }
        
        /// <summary>
        /// Gets the current foot positions.
        /// </summary>
        public float3[] GetFootPositions()
        {
            if (!_initialized || !_footPositions.IsCreated) return Array.Empty<float3>();
            return _footPositions.ToArray();
        }
        
        /// <summary>
        /// Gets the computed body position.
        /// </summary>
        public float3 GetBodyPosition()
        {
            return _bodyPosition.IsCreated && _bodyPosition.Length > 0 
                ? _bodyPosition[0] 
                : (float3)transform.position;
        }
        
        /// <summary>
        /// Gets the computed body rotation.
        /// </summary>
        public quaternion GetBodyRotation()
        {
            return _bodyRotation.IsCreated && _bodyRotation.Length > 0 
                ? _bodyRotation[0] 
                : (quaternion)transform.rotation;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_initialized || !_footPositions.IsCreated) return;
            
            for (int i = 0; i < _legs.Length; i++)
            {
                bool grounded = _footGrounded.IsCreated && _footGrounded[i];
                
                Gizmos.color = grounded ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(_footPositions[i], 0.03f);
                
                if (_footTargets.IsCreated)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(_footPositions[i], _footTargets[i]);
                }
            }
        }
#endif
    }
}
