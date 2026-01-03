using System;
using Unity.Mathematics;
using UnityEngine;
using Eraflo.Catalyst.ProceduralAnimation.Perception;
using Eraflo.Catalyst.ProceduralAnimation.Solvers;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.Locomotion
{
    /// <summary>
    /// Handles foot placement using raycasting for ground detection.
    /// </summary>
    [Serializable]
    public class FootPlacement
    {
        [Header("Ground Detection")]
        [Tooltip("Layer mask for ground detection.")]
        [SerializeField] private LayerMask _groundMask = ~0;
        
        [Tooltip("Maximum raycast distance.")]
        [SerializeField] private float _raycastDistance = 2f;
        
        [Tooltip("Offset from foot position for raycast origin.")]
        [SerializeField] private float _raycastHeightOffset = 0.5f;
        
        [Header("Placement")]
        [Tooltip("How far ahead to predict foot placement.")]
        [SerializeField] private float _predictionDistance = 0.3f;
        
        [Tooltip("Minimum distance to retarget foot.")]
        [SerializeField] private float _retargetThreshold = 0.1f;
        
        [Tooltip("Maximum slope angle in degrees.")]
        [SerializeField] private float _maxSlopeAngle = 45f;
        
        [Header("Smoothing")]
        [Tooltip("Speed of foot position smoothing.")]
        [SerializeField] private float _smoothingSpeed = 10f;
        
        [Tooltip("Speed of foot rotation alignment.")]
        [SerializeField] private float _rotationSmoothingSpeed = 8f;
        
        // Current state - now using SpringMotion for smooth movement
        private SpringMotion _positionSpring;
        private SpringMotionQuaternion _rotationSpring;
        private float3 _targetPosition;
        private quaternion _targetRotation;
        private float3 _groundNormal;
        private bool _isGrounded;
        private bool _initialized;
        
        /// <summary>
        /// Current foot position (world space).
        /// </summary>
        public float3 CurrentPosition => _positionSpring.Position;
        
        /// <summary>
        /// Target foot position (world space).
        /// </summary>
        public float3 TargetPosition => _targetPosition;
        
        /// <summary>
        /// Current foot rotation (world space).
        /// </summary>
        public quaternion CurrentRotation => _rotationSpring.Rotation;
        
        /// <summary>
        /// Ground normal at foot position.
        /// </summary>
        public float3 GroundNormal => _groundNormal;
        
        /// <summary>
        /// Whether the foot is currently on the ground.
        /// </summary>
        public bool IsGrounded => _isGrounded;
        
        /// <summary>
        /// Ground layer mask.
        /// </summary>
        public LayerMask GroundMask
        {
            get => _groundMask;
            set => _groundMask = value;
        }
        
        /// <summary>
        /// Initializes the foot placement at a position.
        /// </summary>
        public void Initialize(float3 position, quaternion rotation)
        {
            // Initialize springs with Smooth preset for natural foot movement
            _positionSpring = SpringMotion.Create(_smoothingSpeed * 0.5f, 0.8f, 0f);
            _positionSpring.Reset(position);
            
            _rotationSpring = SpringMotionQuaternion.Create(_rotationSmoothingSpeed * 0.5f, 0.9f, 0f);
            _rotationSpring.Reset(rotation);
            
            _targetPosition = position;
            _targetRotation = rotation;
            _groundNormal = new float3(0, 1, 0);
            _initialized = true;
            
            // Initial ground check
            FindGround(position);
        }
        
        /// <summary>
        /// Updates foot placement in stance phase (grounded).
        /// </summary>
        public void UpdateStance(float deltaTime)
        {
            if (!_initialized) return;
            
            // Use SpringMotion for smooth movement toward target
            _positionSpring.Update(_targetPosition, deltaTime);
            
            // Align rotation to ground using spring
            UpdateRotationToGround(deltaTime);
        }
        
        /// <summary>
        /// Updates foot placement in swing phase (in air).
        /// </summary>
        /// <param name="hipPosition">Current hip position.</param>
        /// <param name="movementDirection">Direction of movement.</param>
        /// <param name="swingProgress">Progress through swing (0-1).</param>
        /// <param name="stepHeight">Height of step arc.</param>
        public void UpdateSwing(float3 hipPosition, float3 movementDirection, float swingProgress, 
                                float stepHeight, float deltaTime)
        {
            if (!_initialized) return;
            
            // Calculate predicted landing position
            float3 predictedPosition = hipPosition + movementDirection * _predictionDistance;
            
            // Find ground at predicted position
            if (FindGround(predictedPosition))
            {
                _targetPosition = _lastGroundHit;
            }
            
            // Interpolate current position with arc
            float3 liftPosition = _swingStartPosition;
            float3 landPosition = _targetPosition;
            
            // Horizontal interpolation
            float3 horizontalPos = math.lerp(liftPosition, landPosition, swingProgress);
            
            // Vertical arc
            float arcHeight = math.sin(swingProgress * math.PI) * stepHeight;
            float baseHeight = math.lerp(liftPosition.y, landPosition.y, swingProgress);
            
            // Set spring target (spring will smooth the movement)
            float3 arcTarget = new float3(horizontalPos.x, baseHeight + arcHeight, horizontalPos.z);
            _positionSpring.Update(arcTarget, deltaTime);
            
            // Prepare rotation for landing
            UpdateRotationToGround(deltaTime);
        }
        
        /// <summary>
        /// Called when entering swing phase (foot lifts off).
        /// </summary>
        public void BeginSwing()
        {
            _swingStartPosition = _positionSpring.Position;
            _isGrounded = false;
        }
        
        /// <summary>
        /// Called when entering stance phase (foot lands).
        /// </summary>
        public void BeginStance()
        {
            _isGrounded = true;
            _targetPosition = _positionSpring.Position;
        }
        
        /// <summary>
        /// Updates target position for a grounded foot that needs to move.
        /// </summary>
        public void UpdateGroundedTarget(float3 hipPosition, float3 velocity)
        {
            if (!_isGrounded) return;
            
            // Calculate ideal foot position relative to hip
            float3 idealPosition = hipPosition;
            
            // Check if current target is too far from ideal
            float distanceFromIdeal = math.length((_targetPosition - idealPosition) * new float3(1, 0, 1));
            
            if (distanceFromIdeal > _retargetThreshold)
            {
                // Find new ground position
                FindGround(idealPosition);
                _targetPosition = _lastGroundHit;
            }
        }
        
        private float3 _swingStartPosition;
        private float3 _lastGroundHit;
        
        /// <summary>
        /// Performs a raycast to find the ground.
        /// </summary>
        private bool FindGround(float3 position)
        {
            Vector3 rayOrigin = new Vector3(position.x, position.y + _raycastHeightOffset, position.z);
            
            if (UnityEngine.Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 
                               _raycastDistance + _raycastHeightOffset, _groundMask))
            {
                // Check slope
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle <= _maxSlopeAngle)
                {
                    _lastGroundHit = hit.point;
                    _groundNormal = hit.normal;
                    return true;
                }
            }
            
            // Fallback: just use the position
            _lastGroundHit = position;
            _groundNormal = new float3(0, 1, 0);
            return false;
        }
        
        /// <summary>
        /// Aligns foot rotation to the ground normal.
        /// </summary>
        private void UpdateRotationToGround(float deltaTime)
        {
            if (math.lengthsq(_groundNormal) < 0.01f)
                _groundNormal = new float3(0, 1, 0);
            
            // Calculate rotation that aligns foot up with ground normal
            float3 up = math.normalizesafe(_groundNormal);
            
            // Use ProjectOnPlane from MathExtensions for cleaner calculation
            float3 forward = MathExtensions.ProjectOnPlane(new float3(0, 0, 1), up);
            forward = math.normalizesafe(forward);
            
            if (math.lengthsq(forward) < 0.01f)
                forward = new float3(0, 0, 1);
            
            _targetRotation = quaternion.LookRotation(forward, up);
            
            // Use SpringMotion for smooth rotation
            _rotationSpring.Update(_targetRotation, deltaTime);
        }
    }
    
    /// <summary>
    /// Manages foot placement for all legs of a character.
    /// </summary>
    public class LegController
    {
        private readonly FootPlacement[] _feet;
        private readonly GaitCycle _gaitCycle;
        private readonly float[] _legPhases;
        private readonly bool[] _wasInStance;
        
        /// <summary>
        /// Number of legs.
        /// </summary>
        public int LegCount => _feet.Length;
        
        /// <summary>
        /// The gait cycle controlling timing.
        /// </summary>
        public GaitCycle GaitCycle => _gaitCycle;
        
        /// <summary>
        /// Creates a leg controller for the given number of legs.
        /// </summary>
        public LegController(int legCount, float[] legPhases = null)
        {
            _feet = new FootPlacement[legCount];
            _wasInStance = new bool[legCount];
            _gaitCycle = new GaitCycle();
            
            for (int i = 0; i < legCount; i++)
            {
                _feet[i] = new FootPlacement();
            }
            
            _legPhases = legPhases ?? new float[legCount];
        }
        
        /// <summary>
        /// Creates a leg controller from a body topology.
        /// </summary>
        public static LegController FromTopology(BodyTopology topology)
        {
            var legs = topology.GetLegs();
            var phases = new float[legs.Length];
            
            for (int i = 0; i < legs.Length; i++)
            {
                phases[i] = legs[i].GaitPhase;
            }
            
            return new LegController(legs.Length, phases);
        }
        
        /// <summary>
        /// Initializes all feet at their current positions.
        /// </summary>
        public void Initialize(float3[] positions, quaternion[] rotations)
        {
            for (int i = 0; i < _feet.Length; i++)
            {
                _feet[i].Initialize(
                    i < positions.Length ? positions[i] : float3.zero,
                    i < rotations.Length ? rotations[i] : quaternion.identity
                );
                _wasInStance[i] = true;
            }
        }
        
        /// <summary>
        /// Sets the leg phases.
        /// </summary>
        public void SetLegPhases(float[] phases)
        {
            for (int i = 0; i < math.min(phases.Length, _legPhases.Length); i++)
            {
                _legPhases[i] = phases[i];
            }
        }
        
        /// <summary>
        /// Updates all legs.
        /// </summary>
        public void Update(float3 bodyPosition, float3 velocity, float[] hipPositionsY, float deltaTime)
        {
            float speed = math.length(velocity);
            float3 direction = speed > 0.01f ? velocity / speed : new float3(0, 0, 1);
            
            _gaitCycle.Update(deltaTime, speed);
            
            for (int i = 0; i < _feet.Length; i++)
            {
                float legPhase = _legPhases[i];
                bool inStance = _gaitCycle.IsInStance(legPhase);
                
                // Detect phase transitions
                if (inStance && !_wasInStance[i])
                {
                    _feet[i].BeginStance();
                }
                else if (!inStance && _wasInStance[i])
                {
                    _feet[i].BeginSwing();
                }
                
                _wasInStance[i] = inStance;
                
                // Update based on phase
                float hipY = i < hipPositionsY.Length ? hipPositionsY[i] : bodyPosition.y;
                float3 hipPos = new float3(bodyPosition.x, hipY, bodyPosition.z);
                
                if (inStance)
                {
                    _feet[i].UpdateStance(deltaTime);
                    _feet[i].UpdateGroundedTarget(hipPos, velocity);
                }
                else
                {
                    float swingProgress = _gaitCycle.GetSwingProgress(legPhase);
                    float stepHeight = _gaitCycle.GetFootHeight(legPhase);
                    _feet[i].UpdateSwing(hipPos, direction, swingProgress, stepHeight, deltaTime);
                }
            }
        }
        
        /// <summary>
        /// Gets a foot placement.
        /// </summary>
        public FootPlacement GetFoot(int index)
        {
            return index >= 0 && index < _feet.Length ? _feet[index] : null;
        }
        
        /// <summary>
        /// Gets current foot positions.
        /// </summary>
        public float3[] GetFootPositions()
        {
            var positions = new float3[_feet.Length];
            for (int i = 0; i < _feet.Length; i++)
            {
                positions[i] = _feet[i].CurrentPosition;
            }
            return positions;
        }
        
        /// <summary>
        /// Gets current foot rotations.
        /// </summary>
        public quaternion[] GetFootRotations()
        {
            var rotations = new quaternion[_feet.Length];
            for (int i = 0; i < _feet.Length; i++)
            {
                rotations[i] = _feet[i].CurrentRotation;
            }
            return rotations;
        }
    }
}
