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

namespace Eraflo.Catalyst.ProceduralAnimation.Components.IK
{
    /// <summary>
    /// High-performance limb IK component using the job system.
    /// Implements IProceduralAnimationJob to integrate with AnimationJobManager.
    /// </summary>
    [AddComponentMenu("Catalyst/Procedural Animation/Limb IK")]
    public class LimbIK : MonoBehaviour, IProceduralAnimationJob
    {
        [Header("Chain")]
        [Tooltip("Root bone of the chain (e.g., shoulder, hip).")]
        [SerializeField] private Transform _root;
        
        [Tooltip("Tip bone of the chain (e.g., hand, foot).")]
        [SerializeField] private Transform _tip;
        
        [Header("Target")]
        [Tooltip("IK target transform.")]
        [SerializeField] private Transform _target;
        
        [Tooltip("Pole/hint target for elbow/knee direction.")]
        [SerializeField] private Transform _pole;
        
        [Tooltip("IK blend weight (0 = FK, 1 = IK).")]
        [SerializeField, Range(0f, 1f)] private float _weight = 1f;
        
        [Header("Solver")]
        [Tooltip("Maximum solver iterations.")]
        [SerializeField, Range(1, 20)] private int _iterations = 10;
        
        [Tooltip("Solver tolerance.")]
        [SerializeField] private float _tolerance = 0.001f;
        
        [Header("Constraints")]
        [Tooltip("Limb type for preset constraints.")]
        [SerializeField] private LimbType _limbType = LimbType.Arm;
        
        [Header("Comfort")] 
        [Tooltip("Enable comfort pose optimization.")]
        [SerializeField] private bool _useComfortPose = true;
        
        [Tooltip("Comfort pose optimizer settings.")]
        [SerializeField] private ComfortPoseOptimizer _comfortOptimizer = new ComfortPoseOptimizer();
        
        // Native arrays for job data
        private NativeArray<float3> _jointPositions;
        private NativeArray<float> _boneLengths;
        private NativeArray<quaternion> _rotations;
        private NativeArray<quaternion> _originalRotations;
        private NativeArray<int2> _chainRanges;
        private NativeArray<float3> _rootPositions;
        private NativeArray<float3> _targetPositions;
        private NativeArray<float3> _upVectors;
        private TransformAccessArray _transformAccess;
        
        // Runtime data
        private Transform[] _bones;
        private int _boneCount;
        private bool _initialized;
        private bool _needsUpdate;
        private float _deltaTime;
        private InertializationBlender _targetInertializer;
        private float3 _smoothedTargetPosition;
        
        /// <summary>
        /// IK blend weight.
        /// </summary>
        public float Weight
        {
            get => _weight;
            set => _weight = math.saturate(value);
        }
        
        /// <summary>
        /// Target transform.
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }
        
        /// <summary>
        /// Pole/hint transform.
        /// </summary>
        public Transform Pole
        {
            get => _pole;
            set => _pole = value;
        }
        
        #region IProceduralAnimationJob Implementation
        
        public bool NeedsUpdate => _needsUpdate && _initialized && _target != null && _weight > 0f;
        
        public void Prepare(float deltaTime)
        {
            _deltaTime = deltaTime;
            
            // Copy current joint positions
            for (int i = 0; i < _boneCount; i++)
            {
                _jointPositions[i] = _bones[i].position;
                _originalRotations[i] = _bones[i].rotation;
            }
            
            // Update inertialization for smooth target transitions
            _targetInertializer.Update(deltaTime);
            float3 rawTarget = _target.position;
            _smoothedTargetPosition = _targetInertializer.ApplyPosition(rawTarget);
            
            // Set root and target
            _rootPositions[0] = _bones[0].position;
            _targetPositions[0] = _smoothedTargetPosition;
            _upVectors[0] = _pole != null 
                ? math.normalizesafe((float3)_pole.position - _jointPositions[1])
                : new float3(0, 1, 0);
        }
        
        public JobHandle Schedule(JobHandle dependency)
        {
            // FABRIK solve
            var fabrikJob = new FABRIKJob
            {
                ChainRanges = _chainRanges,
                RootPositions = _rootPositions,
                TargetPositions = _targetPositions,
                BoneLengths = _boneLengths,
                JointPositions = _jointPositions,
                MaxIterations = _iterations,
                Tolerance = _tolerance
            };
            
            var fabrikHandle = fabrikJob.Schedule(1, dependency);
            
            // Convert positions to rotations
            var rotationJob = new PositionToRotationJob
            {
                ChainRanges = _chainRanges,
                JointPositions = _jointPositions,
                UpVectors = _upVectors,
                Rotations = _rotations
            };
            
            var rotationHandle = rotationJob.Schedule(_boneCount, 4, fabrikHandle);
            
            // Comfort pose optimization
            JobHandle comfortHandle = rotationHandle;
            if (_useComfortPose)
            {
                comfortHandle = _comfortOptimizer.ScheduleOptimize(_rotations, -1f, rotationHandle);
            }
            
            // Apply rotations to transforms
            var applyJob = new ApplyRotationsJob
            {
                Rotations = _rotations,
                OriginalRotations = _originalRotations,
                BlendWeight = _weight
            };
            
            return applyJob.Schedule(_transformAccess, comfortHandle);
        }
        
        public void Apply()
        {
            // Results are applied directly by ApplyRotationsJob
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
            if (_root == null || _tip == null) return;
            
            // Build chain
            var boneList = new System.Collections.Generic.List<Transform>();
            Transform current = _tip;
            
            while (current != null && current != _root.parent)
            {
                boneList.Insert(0, current);
                current = current.parent;
                if (boneList.Count > 50) break;
            }
            
            if (boneList.Count < 2)
            {
                Debug.LogWarning($"[LimbIK] Chain too short on {gameObject.name}");
                return;
            }
            
            _bones = boneList.ToArray();
            _boneCount = _bones.Length;
            
            // Allocate native arrays
            _jointPositions = new NativeArray<float3>(_boneCount, Allocator.Persistent);
            _boneLengths = new NativeArray<float>(_boneCount - 1, Allocator.Persistent);
            _rotations = new NativeArray<quaternion>(_boneCount, Allocator.Persistent);
            _originalRotations = new NativeArray<quaternion>(_boneCount, Allocator.Persistent);
            _chainRanges = new NativeArray<int2>(1, Allocator.Persistent);
            _rootPositions = new NativeArray<float3>(1, Allocator.Persistent);
            _targetPositions = new NativeArray<float3>(1, Allocator.Persistent);
            _upVectors = new NativeArray<float3>(1, Allocator.Persistent);
            
            // Calculate bone lengths
            for (int i = 0; i < _boneCount - 1; i++)
            {
                _boneLengths[i] = Vector3.Distance(_bones[i].position, _bones[i + 1].position);
            }
            
            // Set chain range (single chain: starts at 0, length = boneCount)
            _chainRanges[0] = new int2(0, _boneCount);
            
            // Create transform access array
            _transformAccess = new TransformAccessArray(_bones);
            
            _initialized = true;
            _needsUpdate = true;
            
            // Initialize comfort optimizer with current pose as rest
            if (_useComfortPose)
            {
                _comfortOptimizer.Initialize(_bones);
            }
            
            // Initialize target inertialization for smooth transitions
            _targetInertializer = InertializationBlender.Create(0.1f);
            _smoothedTargetPosition = _target != null ? (float3)_target.position : float3.zero;
            
            // Register with job manager
            AnimationJobManager.Instance?.Register(this);
        }
        
        /// <summary>
        /// Sets a new IK target with smooth transition using inertialization.
        /// </summary>
        public void SetTarget(Transform newTarget, float transitionTime = 0.15f)
        {
            if (newTarget == null) return;
            
            float3 oldPosition = _target != null ? (float3)_target.position : _smoothedTargetPosition;
            _target = newTarget;
            float3 newPosition = newTarget.position;
            
            // Trigger inertialization for smooth transition
            _targetInertializer.SetHalfLife(transitionTime);
            _targetInertializer.TransitionPosition(oldPosition, newPosition);
        }
        
        /// <summary>
        /// Sets target position directly with smooth transition.
        /// </summary>
        public void SetTargetPosition(float3 newPosition, float transitionTime = 0.15f)
        {
            float3 oldPosition = _smoothedTargetPosition;
            
            _targetInertializer.SetHalfLife(transitionTime);
            _targetInertializer.TransitionPosition(oldPosition, newPosition);
        }
        
        public void Dispose()
        {
            AnimationJobManager.Instance?.Unregister(this);
            
            if (_jointPositions.IsCreated) _jointPositions.Dispose();
            if (_boneLengths.IsCreated) _boneLengths.Dispose();
            if (_rotations.IsCreated) _rotations.Dispose();
            if (_originalRotations.IsCreated) _originalRotations.Dispose();
            if (_chainRanges.IsCreated) _chainRanges.Dispose();
            if (_rootPositions.IsCreated) _rootPositions.Dispose();
            if (_targetPositions.IsCreated) _targetPositions.Dispose();
            if (_upVectors.IsCreated) _upVectors.Dispose();
            if (_transformAccess.isCreated) _transformAccess.Dispose();
            
            _comfortOptimizer?.Dispose();
            
            _initialized = false;
        }
        
        /// <summary>
        /// Sets up the chain from transforms.
        /// </summary>
        public void SetupChain(Transform root, Transform tip)
        {
            Dispose();
            _root = root;
            _tip = tip;
            Initialize();
        }
        
        /// <summary>
        /// Sets up from a LimbChain.
        /// </summary>
        public void SetupFromLimb(LimbChain limb)
        {
            if (limb.Bones == null || limb.Bones.Length < 2) return;
            
            Dispose();
            _root = limb.Root;
            _tip = limb.Effector;
            _limbType = limb.Type;
            Initialize();
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_bones == null || _bones.Length < 2) return;
            
            Gizmos.color = Color.blue;
            for (int i = 0; i < _bones.Length - 1; i++)
            {
                if (_bones[i] != null && _bones[i + 1] != null)
                {
                    Gizmos.DrawLine(_bones[i].position, _bones[i + 1].position);
                    Gizmos.DrawWireSphere(_bones[i].position, 0.02f);
                }
            }
            
            if (_bones[_bones.Length - 1] != null)
                Gizmos.DrawWireSphere(_bones[_bones.Length - 1].position, 0.02f);
            
            if (_target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_target.position, 0.03f);
            }
            
            if (_pole != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_pole.position, 0.02f);
            }
        }
#endif
    }
}
