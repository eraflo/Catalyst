using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Eraflo.Catalyst.ProceduralAnimation;
using Eraflo.Catalyst.ProceduralAnimation.Jobs;
using Eraflo.Catalyst.ProceduralAnimation.Perception;
using Eraflo.Catalyst.ProceduralAnimation.SignalProcessing;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.Physics
{
    /// <summary>
    /// Component that creates and manages an active ragdoll using the job system.
    /// Blends between animation and physics-driven motion.
    /// Implements IProceduralAnimationJob for integration with AnimationJobManager.
    /// Note: Physics updates still happen in FixedUpdate for stability.
    /// </summary>
    [AddComponentMenu("Catalyst/Procedural Animation/Active Ragdoll")]
    public class ActiveRagdoll : MonoBehaviour, IProceduralAnimationJob
    {
        [Header("Setup")]
        [Tooltip("The animated skeleton root to follow.")]
        [SerializeField] private Transform _animatedRoot;
        
        [Tooltip("Auto-generate ragdoll from skeleton.")]
        [SerializeField] private bool _autoGenerate = true;
        
        [Header("Blend")]
        [Tooltip("Overall muscle strength (0 = full ragdoll, 1 = full animation).")]
        [SerializeField, Range(0f, 1f)] private float _muscleStrength = 0.8f;
        
        [Tooltip("Speed of strength transitions.")]
        [SerializeField] private float _transitionSpeed = 5f;
        
        [Header("Muscles")]
        [Tooltip("Muscle preset for initial setup.")]
        [SerializeField] private MusclePreset _musclePreset = MusclePreset.Normal;
        
        [Header("Physics")]
        [Tooltip("Mass of the root body.")]
        [SerializeField] private float _rootMass = 50f;
        
        [Tooltip("Mass of each limb segment.")]
        [SerializeField] private float _limbMass = 5f;
        
        [Tooltip("Use gravity.")]
        [SerializeField] private bool _useGravity = true;
        
        [Tooltip("Collision detection mode.")]
        [SerializeField] private CollisionDetectionMode _collisionMode = CollisionDetectionMode.Continuous;
        
        [Header("Recovery")]
        [Tooltip("Automatically try to recover balance after impacts.")]
        [SerializeField] private bool _autoRecover = true;
        
        [Tooltip("Time to wait before starting recovery.")]
        [SerializeField] private float _recoveryDelay = 0.5f;
        
        [Tooltip("Speed of recovery.")]
        [SerializeField] private float _recoverySpeed = 2f;
        
        // Runtime data
        private List<RagdollMuscle> _muscles = new List<RagdollMuscle>();
        private List<Rigidbody> _bodies = new List<Rigidbody>();
        private List<Transform> _animatedBones = new List<Transform>();
        private Dictionary<Transform, Transform> _boneMapping = new Dictionary<Transform, Transform>();
        
        // Job data
        private NativeArray<quaternion> _animatedRotations;
        private NativeArray<quaternion> _currentRotations;
        private NativeArray<quaternion> _targetRotations;
        private NativeArray<float3> _angularErrors;
        
        private float _currentStrength;
        private float _targetStrength;
        private float _impactTime;
        private bool _isRecovering;
        private bool _initialized;
        private bool _needsUpdate;
        private float _deltaTime;
        private InertializationBlender _strengthInertializer;
        
        /// <summary>
        /// Overall muscle strength.
        /// </summary>
        public float MuscleStrength
        {
            get => _muscleStrength;
            set => _targetStrength = math.saturate(value);
        }
        
        /// <summary>
        /// Whether the ragdoll is currently recovering from an impact.
        /// </summary>
        public bool IsRecovering => _isRecovering;
        
        /// <summary>
        /// All rigidbodies in the ragdoll.
        /// </summary>
        public IReadOnlyList<Rigidbody> Bodies => _bodies;
        
        /// <summary>
        /// All muscles in the ragdoll.
        /// </summary>
        public IReadOnlyList<RagdollMuscle> Muscles => _muscles;
        
        #region IProceduralAnimationJob Implementation
        
        public bool NeedsUpdate => _needsUpdate && _initialized && _muscles.Count > 0;
        
        public void Prepare(float deltaTime)
        {
            _deltaTime = deltaTime;
            
            // Transition strength (on main thread, needed immediately)
            _currentStrength = math.lerp(_currentStrength, _targetStrength, 
                                         deltaTime * _transitionSpeed);
            
            // Copy animation rotations
            for (int i = 0; i < _muscles.Count; i++)
            {
                if (_animatedBones[i] != null)
                    _animatedRotations[i] = _animatedBones[i].rotation;
                
                if (_muscles[i].Joint != null && _muscles[i].Joint.gameObject != null)
                    _currentRotations[i] = _muscles[i].Joint.transform.rotation;
            }
        }
        
        public JobHandle Schedule(JobHandle dependency)
        {
            // Compute muscle targets in parallel
            var muscleJob = new MuscleComputeJob
            {
                AnimatedRotations = _animatedRotations,
                CurrentRotations = _currentRotations,
                Strength = _currentStrength,
                TargetRotations = _targetRotations,
                AngularErrors = _angularErrors
            };
            
            return muscleJob.Schedule(_muscles.Count, 8, dependency);
        }
        
        public void Apply()
        {
            // Apply computed angular errors to muscles
            for (int i = 0; i < _muscles.Count; i++)
            {
                var muscle = _muscles[i];
                if (muscle.Joint != null && muscle.Joint.GetComponent<Rigidbody>() != null)
                {
                    var rb = muscle.Joint.GetComponent<Rigidbody>();
                    float3 error = _angularErrors[i];
                    
                    // Apply torque based on PD control
                    float3 torque = error * muscle.AngularSpring - (float3)rb.angularVelocity * muscle.AngularDamper;
                    torque = math.clamp(torque, -muscle.MaxForce, muscle.MaxForce);
                    
                    rb.AddTorque((Vector3)torque, ForceMode.Force);
                }
            }
        }
        
        #endregion
        
        private void Awake()
        {
            _targetStrength = _muscleStrength;
            _currentStrength = _muscleStrength;
            _strengthInertializer = InertializationBlender.Create(0.15f);
        }
        
        private void Start()
        {
            if (_autoGenerate && _animatedRoot != null)
            {
                GenerateRagdoll();
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
        
        private void FixedUpdate()
        {
            if (!_initialized) return;
            
            // Handle recovery
            if (_autoRecover && _isRecovering)
            {
                float timeSinceImpact = Time.time - _impactTime;
                if (timeSinceImpact > _recoveryDelay)
                {
                    _targetStrength = math.lerp(_targetStrength, _muscleStrength, 
                                                Time.fixedDeltaTime * _recoverySpeed);
                    
                    if (math.abs(_targetStrength - _muscleStrength) < 0.01f)
                    {
                        _targetStrength = _muscleStrength;
                        _isRecovering = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// Generates the ragdoll from the animated skeleton.
        /// </summary>
        public void GenerateRagdoll()
        {
            if (_animatedRoot == null)
            {
                Debug.LogError("[ActiveRagdoll] No animated root specified.");
                return;
            }
            
            // Create ragdoll hierarchy
            CreateRagdollHierarchy(_animatedRoot, transform, null);
            
            // Allocate job arrays
            int muscleCount = _muscles.Count;
            if (muscleCount > 0)
            {
                _animatedRotations = new NativeArray<quaternion>(muscleCount, Allocator.Persistent);
                _currentRotations = new NativeArray<quaternion>(muscleCount, Allocator.Persistent);
                _targetRotations = new NativeArray<quaternion>(muscleCount, Allocator.Persistent);
                _angularErrors = new NativeArray<float3>(muscleCount, Allocator.Persistent);
            }
            
            _initialized = true;
            _needsUpdate = true;
            
            AnimationJobManager.Instance?.Register(this);
            
            Debug.Log($"[ActiveRagdoll] Generated ragdoll with {_muscles.Count} muscles.");
        }
        
        /// <summary>
        /// Sets up the ragdoll from a BodyTopology.
        /// Called by ProceduralAnimator.SetupRagdoll().
        /// </summary>
        public void SetupFromTopology(BodyTopology topology, Transform animatedRoot)
        {
            if (topology == null)
            {
                Debug.LogWarning("[ActiveRagdoll] Cannot setup: no topology.");
                return;
            }
            
            _animatedRoot = animatedRoot;
            
            // Use topology bone data for mass distribution
            foreach (var bone in topology.AllBones)
            {
                if (bone.Transform != null && bone.Mass > 0.01f)
                {
                    // Store mass info for ragdoll generation
                    _boneMasses[bone.Transform] = bone.Mass;
                }
            }
            
            GenerateRagdoll();
        }
        
        private Dictionary<Transform, float> _boneMasses = new Dictionary<Transform, float>();
        
        private void CreateRagdollHierarchy(Transform source, Transform parent, Rigidbody parentBody)
        {
            // Create ragdoll bone
            var ragdollBone = new GameObject(source.name + "_Ragdoll");
            ragdollBone.transform.SetParent(parent);
            ragdollBone.transform.position = source.position;
            ragdollBone.transform.rotation = source.rotation;
            
            // Add rigidbody
            var rb = ragdollBone.AddComponent<Rigidbody>();
            rb.mass = parentBody == null ? _rootMass : _limbMass;
            rb.useGravity = _useGravity;
            rb.collisionDetectionMode = _collisionMode;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            _bodies.Add(rb);
            
            // Add collider (capsule for limbs, box for body)
            if (source.childCount > 0)
            {
                var firstChild = source.GetChild(0);
                float length = Vector3.Distance(source.position, firstChild.position);
                
                if (length > 0.01f)
                {
                    var capsule = ragdollBone.AddComponent<CapsuleCollider>();
                    capsule.radius = length * 0.15f;
                    capsule.height = length;
                    capsule.direction = 1; // Y-axis
                    
                    // Center the capsule
                    Vector3 dir = firstChild.position - source.position;
                    capsule.center = ragdollBone.transform.InverseTransformDirection(dir) * 0.5f;
                }
            }
            else
            {
                // Leaf bone - small sphere
                var sphere = ragdollBone.AddComponent<SphereCollider>();
                sphere.radius = 0.05f;
            }
            
            // Add joint to parent
            if (parentBody != null)
            {
                var joint = ragdollBone.AddComponent<ConfigurableJoint>();
                joint.connectedBody = parentBody;
                
                // Configure joint limits
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                
                // Set limits
                var lowLimit = new SoftJointLimit { limit = -60f };
                var highLimit = new SoftJointLimit { limit = 60f };
                joint.lowAngularXLimit = lowLimit;
                joint.highAngularXLimit = highLimit;
                joint.angularYLimit = highLimit;
                joint.angularZLimit = highLimit;
                
                // Create muscle
                var muscle = RagdollMuscle.CreateFromPreset(_musclePreset);
                muscle.Joint = joint;
                muscle.Target = source;
                muscle.Initialize();
                _muscles.Add(muscle);
                _animatedBones.Add(source);
            }
            
            // Map bones
            _boneMapping[source] = ragdollBone.transform;
            
            // Process children
            foreach (Transform child in source)
            {
                if (child.childCount > 0 || IsImportantBone(child.name))
                {
                    CreateRagdollHierarchy(child, ragdollBone.transform, rb);
                }
            }
        }
        
        private bool IsImportantBone(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Contains("hand") || lower.Contains("foot") || 
                   lower.Contains("head") || lower.Contains("spine") ||
                   lower.Contains("arm") || lower.Contains("leg");
        }
        
        public void Dispose()
        {
            AnimationJobManager.Instance?.Unregister(this);
            
            if (_animatedRotations.IsCreated) _animatedRotations.Dispose();
            if (_currentRotations.IsCreated) _currentRotations.Dispose();
            if (_targetRotations.IsCreated) _targetRotations.Dispose();
            if (_angularErrors.IsCreated) _angularErrors.Dispose();
            
            _initialized = false;
        }
        
        /// <summary>
        /// Applies an impact force to the ragdoll.
        /// </summary>
        public void ApplyImpact(Vector3 force, Vector3 position)
        {
            // Find closest body
            Rigidbody closestBody = null;
            float closestDist = float.MaxValue;
            
            foreach (var body in _bodies)
            {
                float dist = Vector3.Distance(body.position, position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestBody = body;
                }
            }
            
            if (closestBody != null)
            {
                closestBody.AddForceAtPosition(force, position, ForceMode.Impulse);
            }
            
            // Reduce strength
            _targetStrength = 0.1f;
            _impactTime = Time.time;
            _isRecovering = true;
        }
        
        /// <summary>
        /// Applies an explosion force to all bodies.
        /// </summary>
        public void ApplyExplosion(Vector3 center, float force, float radius)
        {
            foreach (var body in _bodies)
            {
                body.AddExplosionForce(force, center, radius);
            }
            
            _targetStrength = 0f;
            _impactTime = Time.time;
            _isRecovering = true;
        }
        
        /// <summary>
        /// Goes fully limp (full ragdoll) with smooth transition.
        /// </summary>
        public void GoLimp(float transitionTime = 0.2f)
        {
            _strengthInertializer.SetHalfLife(transitionTime);
            _strengthInertializer.TransitionPosition(
                new float3(_currentStrength, 0, 0), 
                new float3(0, 0, 0));
            _targetStrength = 0f;
            _isRecovering = false;
        }
        
        /// <summary>
        /// Returns to full animation tracking with smooth transition.
        /// </summary>
        public void Recover(float transitionTime = 0.3f)
        {
            _strengthInertializer.SetHalfLife(transitionTime);
            _strengthInertializer.TransitionPosition(
                new float3(_currentStrength, 0, 0), 
                new float3(_muscleStrength, 0, 0));
            _targetStrength = _muscleStrength;
        }
        
        /// <summary>
        /// Gets the ragdoll bone corresponding to an animated bone.
        /// </summary>
        public Transform GetRagdollBone(Transform animatedBone)
        {
            _boneMapping.TryGetValue(animatedBone, out var ragdollBone);
            return ragdollBone;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_bodies == null || _bodies.Count == 0) return;
            
            Gizmos.color = Color.Lerp(Color.red, Color.green, _currentStrength);
            
            foreach (var body in _bodies)
            {
                if (body != null)
                {
                    Gizmos.DrawWireSphere(body.position, 0.02f);
                }
            }
        }
#endif
    }
}
