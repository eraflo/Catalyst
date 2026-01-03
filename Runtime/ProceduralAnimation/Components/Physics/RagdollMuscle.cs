using System;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.Physics
{
    /// <summary>
    /// Represents a muscle that drives a ragdoll joint toward an animation target.
    /// Uses PD (Proportional-Derivative) control.
    /// </summary>
    [Serializable]
    public class RagdollMuscle
    {
        [Header("Joint")]
        [Tooltip("The configurable joint being controlled.")]
        public ConfigurableJoint Joint;
        
        [Tooltip("The target transform to follow.")]
        public Transform Target;
        
        [Header("Strength")]
        [Tooltip("Proportional gain (spring strength).")]
        [SerializeField] private float _positionSpring = 1000f;
        
        [Tooltip("Derivative gain (damping).")]
        [SerializeField] private float _positionDamper = 100f;
        
        [Tooltip("Maximum force the muscle can apply.")]
        [SerializeField] private float _maxForce = 500f;
        
        [Header("Rotation")]
        [Tooltip("Angular spring strength.")]
        [SerializeField] private float _angularSpring = 5000f;
        
        [Tooltip("Angular damping.")]
        [SerializeField] private float _angularDamper = 500f;
        
        [Tooltip("Maximum angular force.")]
        [SerializeField] private float _maxAngularForce = 1000f;
        
        // Runtime state
        private Rigidbody _rb;
        private Quaternion _initialRotation;
        private bool _initialized;
        
        /// <summary>
        /// Position spring strength.
        /// </summary>
        public float PositionSpring
        {
            get => _positionSpring;
            set => _positionSpring = math.max(0f, value);
        }
        
        /// <summary>
        /// Position damping.
        /// </summary>
        public float PositionDamper
        {
            get => _positionDamper;
            set => _positionDamper = math.max(0f, value);
        }
        
        /// <summary>
        /// Angular spring strength.
        /// </summary>
        public float AngularSpring
        {
            get => _angularSpring;
            set => _angularSpring = math.max(0f, value);
        }
        
        /// <summary>
        /// Angular damping.
        /// </summary>
        public float AngularDamper
        {
            get => _angularDamper;
            set => _angularDamper = math.max(0f, value);
        }
        
        /// <summary>
        /// Maximum force the muscle can apply.
        /// </summary>
        public float MaxForce
        {
            get => _maxForce;
            set => _maxForce = math.max(0f, value);
        }
        
        /// <summary>
        /// Maximum angular force the muscle can apply.
        /// </summary>
        public float MaxAngularForce
        {
            get => _maxAngularForce;
            set => _maxAngularForce = math.max(0f, value);
        }
        
        /// <summary>
        /// Initializes the muscle.
        /// </summary>
        public void Initialize()
        {
            if (Joint == null) return;
            
            _rb = Joint.GetComponent<Rigidbody>();
            _initialRotation = Joint.transform.localRotation;
            
            // Configure joint drives
            UpdateJointDrive();
            
            _initialized = true;
        }
        
        /// <summary>
        /// Updates the joint drive settings.
        /// </summary>
        public void UpdateJointDrive()
        {
            if (Joint == null) return;
            
            // Position drive (for translation if needed)
            var posDrive = new JointDrive
            {
                positionSpring = _positionSpring,
                positionDamper = _positionDamper,
                maximumForce = _maxForce
            };
            
            Joint.xDrive = posDrive;
            Joint.yDrive = posDrive;
            Joint.zDrive = posDrive;
            
            // Angular drive (for rotation)
            var angDrive = new JointDrive
            {
                positionSpring = _angularSpring,
                positionDamper = _angularDamper,
                maximumForce = _maxAngularForce
            };
            
            Joint.angularXDrive = angDrive;
            Joint.angularYZDrive = angDrive;
            Joint.slerpDrive = angDrive;
            
            // Use rotation drive mode
            Joint.rotationDriveMode = RotationDriveMode.Slerp;
        }
        
        /// <summary>
        /// Updates the muscle, driving the joint toward the target.
        /// </summary>
        public void Update()
        {
            if (!_initialized || Joint == null || Target == null) return;
            
            // Calculate target rotation in joint space
            Quaternion targetRotation = Target.rotation;
            
            // Set the target rotation for the joint
            Joint.targetRotation = Quaternion.Inverse(_initialRotation) * 
                                   Quaternion.Inverse(Joint.connectedBody != null ? 
                                                      Joint.connectedBody.rotation : 
                                                      Quaternion.identity) * 
                                   targetRotation;
        }
        
        /// <summary>
        /// Sets the muscle strength as a fraction (0-1).
        /// </summary>
        public void SetStrength(float strength)
        {
            strength = math.saturate(strength);
            
            UpdateJointDrive();
            
            // Scale all drives
            var scaledPosDrive = Joint.xDrive;
            scaledPosDrive.positionSpring *= strength;
            scaledPosDrive.positionDamper *= strength;
            
            Joint.xDrive = scaledPosDrive;
            Joint.yDrive = scaledPosDrive;
            Joint.zDrive = scaledPosDrive;
            
            var scaledAngDrive = Joint.slerpDrive;
            scaledAngDrive.positionSpring *= strength;
            scaledAngDrive.positionDamper *= strength;
            
            Joint.angularXDrive = scaledAngDrive;
            Joint.angularYZDrive = scaledAngDrive;
            Joint.slerpDrive = scaledAngDrive;
        }
        
        /// <summary>
        /// Relaxes the muscle (sets strength to 0).
        /// </summary>
        public void Relax()
        {
            SetStrength(0f);
        }
        
        /// <summary>
        /// Creates a muscle configuration from preset.
        /// </summary>
        public static RagdollMuscle CreateFromPreset(MusclePreset preset)
        {
            return preset switch
            {
                MusclePreset.Strong => new RagdollMuscle
                {
                    _positionSpring = 2000f,
                    _positionDamper = 200f,
                    _angularSpring = 10000f,
                    _angularDamper = 1000f,
                    _maxForce = 1000f,
                    _maxAngularForce = 2000f
                },
                MusclePreset.Normal => new RagdollMuscle
                {
                    _positionSpring = 1000f,
                    _positionDamper = 100f,
                    _angularSpring = 5000f,
                    _angularDamper = 500f,
                    _maxForce = 500f,
                    _maxAngularForce = 1000f
                },
                MusclePreset.Weak => new RagdollMuscle
                {
                    _positionSpring = 300f,
                    _positionDamper = 30f,
                    _angularSpring = 1500f,
                    _angularDamper = 150f,
                    _maxForce = 200f,
                    _maxAngularForce = 400f
                },
                MusclePreset.Limp => new RagdollMuscle
                {
                    _positionSpring = 50f,
                    _positionDamper = 5f,
                    _angularSpring = 200f,
                    _angularDamper = 20f,
                    _maxForce = 50f,
                    _maxAngularForce = 100f
                },
                _ => new RagdollMuscle()
            };
        }
    }
    
    /// <summary>
    /// Preset muscle strength configurations.
    /// </summary>
    public enum MusclePreset
    {
        /// <summary>Very strong muscles, almost full animation tracking.</summary>
        Strong,
        /// <summary>Normal muscle strength.</summary>
        Normal,
        /// <summary>Weak muscles, more physics influence.</summary>
        Weak,
        /// <summary>Very weak, mostly ragdoll with slight correction.</summary>
        Limp
    }
}
