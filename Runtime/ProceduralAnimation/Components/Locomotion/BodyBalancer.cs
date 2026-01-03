using System;
using Unity.Mathematics;
using UnityEngine;
using Eraflo.Catalyst.ProceduralAnimation.Perception;

namespace Eraflo.Catalyst.ProceduralAnimation.Components.Locomotion
{
    /// <summary>
    /// Handles body balance and center of mass adjustment based on foot positions.
    /// </summary>
    [Serializable]
    public class BodyBalancer
    {
        [Header("Balance")]
        [Tooltip("How much to lean into the direction of movement.")]
        [SerializeField, Range(0f, 30f)] private float _movementLeanAngle = 5f;
        
        [Tooltip("How much to lean toward grounded feet.")]
        [SerializeField, Range(0f, 15f)] private float _balanceLeanAngle = 3f;
        
        [Tooltip("Speed of balance adjustment.")]
        [SerializeField] private float _balanceSpeed = 5f;
        
        [Header("Height")]
        [Tooltip("Target height above ground.")]
        [SerializeField] private float _targetHeight = 1f;
        
        [Tooltip("Speed of height adjustment.")]
        [SerializeField] private float _heightSpeed = 8f;
        
        [Tooltip("Bobbing amplitude during walk.")]
        [SerializeField] private float _bobbingAmplitude = 0.02f;
        
        [Header("Rotation")]
        [Tooltip("Speed of rotation smoothing.")]
        [SerializeField] private float _rotationSpeed = 10f;
        
        [Tooltip("Maximum tilt angle from legs.")]
        [SerializeField, Range(0f, 30f)] private float _maxTiltAngle = 15f;
        
        // Current state - using SpringMotion for smooth movement
        private SpringMotion _positionSpring;
        private SpringMotionQuaternion _rotationSpring;
        private float3 _velocity;
        private bool _initialized;
        
        /// <summary>
        /// Current body position (world space).
        /// </summary>
        public float3 Position => _positionSpring.Position;
        
        /// <summary>
        /// Current body rotation (world space).
        /// </summary>
        public quaternion Rotation => _rotationSpring.Rotation;
        
        /// <summary>
        /// Target height above ground.
        /// </summary>
        public float TargetHeight
        {
            get => _targetHeight;
            set => _targetHeight = math.max(0.1f, value);
        }
        
        /// <summary>
        /// Initializes the balancer at a position.
        /// </summary>
        public void Initialize(float3 position, quaternion rotation)
        {
            _positionSpring = SpringMotion.Create(_heightSpeed * 0.3f, 0.9f, 0f);
            _positionSpring.Reset(position);
            
            _rotationSpring = SpringMotionQuaternion.Create(_rotationSpeed * 0.3f, 0.85f, 0f);
            _rotationSpring.Reset(rotation);
            
            _velocity = float3.zero;
            _initialized = true;
        }
        
        /// <summary>
        /// Updates the body balance based on foot positions.
        /// </summary>
        /// <param name="footPositions">World positions of all feet.</param>
        /// <param name="footGrounded">Whether each foot is grounded.</param>
        /// <param name="velocity">Current movement velocity.</param>
        /// <param name="gaitPhase">Current phase of gait cycle.</param>
        /// <param name="deltaTime">Time step.</param>
        public void Update(float3[] footPositions, bool[] footGrounded, float3 velocity, 
                          float gaitPhase, float deltaTime)
        {
            if (footPositions == null || footPositions.Length == 0) return;
            if (!_initialized) return;
            
            _velocity = velocity;
            
            // Calculate support polygon center
            float3 supportCenter = CalculateSupportCenter(footPositions, footGrounded);
            
            // Calculate target height
            float groundHeight = CalculateGroundHeight(footPositions, footGrounded);
            float bobHeight = CalculateBobHeight(gaitPhase);
            float targetY = groundHeight + _targetHeight + bobHeight;
            
            // Calculate target position (centered over support)
            float3 targetPosition = new float3(supportCenter.x, targetY, supportCenter.z);
            
            // Use SpringMotion for smooth position
            _positionSpring.Update(targetPosition, deltaTime);
            
            // Calculate target rotation
            quaternion targetRotation = CalculateTargetRotation(footPositions, footGrounded, velocity);
            
            // Use SpringMotion for smooth rotation
            _rotationSpring.Update(targetRotation, deltaTime);
        }
        
        /// <summary>
        /// Calculates the center of the support polygon formed by grounded feet.
        /// </summary>
        private float3 CalculateSupportCenter(float3[] footPositions, bool[] footGrounded)
        {
            float3 sum = float3.zero;
            int count = 0;
            
            for (int i = 0; i < footPositions.Length; i++)
            {
                if (i < footGrounded.Length && footGrounded[i])
                {
                    sum += footPositions[i];
                    count++;
                }
            }
            
            if (count == 0)
            {
                // No feet grounded, use average of all
                foreach (var pos in footPositions)
                    sum += pos;
                count = footPositions.Length;
            }
            
            return count > 0 ? sum / count : _positionSpring.Position;
        }
        
        /// <summary>
        /// Calculates the average ground height from foot positions.
        /// </summary>
        private float CalculateGroundHeight(float3[] footPositions, bool[] footGrounded)
        {
            float sum = 0f;
            int count = 0;
            
            for (int i = 0; i < footPositions.Length; i++)
            {
                if (i < footGrounded.Length && footGrounded[i])
                {
                    sum += footPositions[i].y;
                    count++;
                }
            }
            
            if (count == 0)
            {
                foreach (var pos in footPositions)
                    sum += pos.y;
                count = footPositions.Length;
            }
            
            return count > 0 ? sum / count : 0f;
        }
        
        /// <summary>
        /// Calculates bobbing height based on gait phase.
        /// </summary>
        private float CalculateBobHeight(float gaitPhase)
        {
            // Two bobs per cycle (for biped), use double frequency
            return math.cos(gaitPhase * math.PI * 4f) * _bobbingAmplitude;
        }
        
        /// <summary>
        /// Calculates the target body rotation based on feet and movement.
        /// </summary>
        private quaternion CalculateTargetRotation(float3[] footPositions, bool[] footGrounded, float3 velocity)
        {
            // Base forward direction
            float speed = math.length(velocity);
            float3 forward = speed > 0.1f 
                ? math.normalizesafe(new float3(velocity.x, 0, velocity.z))
                : math.forward(_rotationSpring.Rotation);
            
            // Calculate tilt from foot heights
            float3 tiltAxis;
            float tiltAngle;
            CalculateTiltFromFeet(footPositions, footGrounded, out tiltAxis, out tiltAngle);
            
            // Calculate lean from movement
            float3 leanAxis = math.cross(new float3(0, 1, 0), forward);
            float leanAngle = speed * math.radians(_movementLeanAngle) * 0.1f;
            
            // Combine rotations
            quaternion baseRot = quaternion.LookRotation(forward, new float3(0, 1, 0));
            quaternion tiltRot = quaternion.AxisAngle(tiltAxis, tiltAngle);
            quaternion leanRot = quaternion.AxisAngle(leanAxis, leanAngle);
            
            return math.mul(leanRot, math.mul(tiltRot, baseRot));
        }
        
        /// <summary>
        /// Calculates body tilt based on foot height differences.
        /// </summary>
        private void CalculateTiltFromFeet(float3[] footPositions, bool[] footGrounded, 
                                           out float3 axis, out float angle)
        {
            axis = float3.zero;
            angle = 0f;
            
            if (footPositions.Length < 2) return;
            
            // Find left and right foot heights
            float leftY = 0f, rightY = 0f;
            float leftCount = 0f, rightCount = 0f;
            float frontY = 0f, backY = 0f;
            float frontCount = 0f, backCount = 0f;
            
            float3 center = CalculateSupportCenter(footPositions, footGrounded);
            
            for (int i = 0; i < footPositions.Length; i++)
            {
                float3 pos = footPositions[i];
                
                // Left/right based on X relative to center
                if (pos.x < center.x)
                {
                    leftY += pos.y;
                    leftCount++;
                }
                else
                {
                    rightY += pos.y;
                    rightCount++;
                }
                
                // Front/back based on Z
                if (pos.z > center.z)
                {
                    frontY += pos.y;
                    frontCount++;
                }
                else
                {
                    backY += pos.y;
                    backCount++;
                }
            }
            
            // Average heights
            leftY = leftCount > 0 ? leftY / leftCount : 0;
            rightY = rightCount > 0 ? rightY / rightCount : 0;
            frontY = frontCount > 0 ? frontY / frontCount : 0;
            backY = backCount > 0 ? backY / backCount : 0;
            
            // Calculate tilt angles
            float rollAngle = math.atan2(rightY - leftY, 1f);
            float pitchAngle = math.atan2(frontY - backY, 1f);
            
            // Clamp angles
            float maxRad = math.radians(_maxTiltAngle);
            rollAngle = math.clamp(rollAngle, -maxRad, maxRad);
            pitchAngle = math.clamp(pitchAngle, -maxRad, maxRad);
            
            // Combine into axis-angle
            float3 rollAxis = new float3(0, 0, 1);
            float3 pitchAxis = new float3(1, 0, 0);
            
            axis = math.normalizesafe(rollAxis * rollAngle + pitchAxis * pitchAngle);
            angle = math.length(new float2(rollAngle, pitchAngle));
        }
    }
}

