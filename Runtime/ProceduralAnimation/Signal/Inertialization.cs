using System;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.SignalProcessing
{
    /// <summary>
    /// Inertialization blender for smooth animation transitions.
    /// Based on the Inertialization algorithm from games like The Last of Us.
    /// </summary>
    /// <remarks>
    /// Inertialization works by capturing the difference between the old and new animation
    /// at the transition point, then exponentially decaying that difference over time.
    /// This creates seamless transitions without blending artifacts.
    /// </remarks>
    [Serializable]
    public struct InertializationBlender
    {
        // Position inertialization state
        private float3 _positionOffset;
        private float3 _positionVelocity;
        
        // Rotation inertialization state
        private float3 _rotationOffset;    // Axis-angle offset
        private float3 _rotationVelocity;
        
        // Configuration
        private float _halfLife;
        private bool _isActive;
        
        /// <summary>
        /// Whether inertialization is currently active.
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// Current position offset being applied.
        /// </summary>
        public float3 PositionOffset => _positionOffset;
        
        /// <summary>
        /// Current rotation offset being applied (axis-angle).
        /// </summary>
        public float3 RotationOffset => _rotationOffset;
        
        /// <summary>
        /// Creates an inertialization blender with the specified half-life.
        /// </summary>
        /// <param name="halfLife">Time in seconds for the offset to decay to half. Range: 0.01 to 1.0.</param>
        public static InertializationBlender Create(float halfLife = 0.15f)
        {
            return new InertializationBlender
            {
                _halfLife = math.clamp(halfLife, 0.01f, 1f),
                _isActive = false
            };
        }
        
        /// <summary>
        /// Sets the half-life for decay.
        /// </summary>
        public void SetHalfLife(float halfLife)
        {
            _halfLife = math.clamp(halfLife, 0.01f, 1f);
        }
        
        /// <summary>
        /// Initiates a transition from the old pose to the new pose.
        /// </summary>
        /// <param name="oldPose">The pose before the transition.</param>
        /// <param name="newPose">The pose after the transition.</param>
        public void Transition(AnimationPose oldPose, AnimationPose newPose)
        {
            TransitionPosition(oldPose.Position, newPose.Position, oldPose.Velocity, newPose.Velocity);
            TransitionRotation(oldPose.Rotation, newPose.Rotation, oldPose.AngularVelocity, newPose.AngularVelocity);
            _isActive = true;
        }
        
        /// <summary>
        /// Initiates a position-only transition.
        /// </summary>
        public void TransitionPosition(float3 oldPosition, float3 newPosition, 
                                        float3 oldVelocity = default, float3 newVelocity = default)
        {
            _positionOffset = oldPosition - newPosition;
            _positionVelocity = oldVelocity - newVelocity;
            _isActive = true;
        }
        
        /// <summary>
        /// Initiates a rotation-only transition.
        /// </summary>
        public void TransitionRotation(quaternion oldRotation, quaternion newRotation,
                                        float3 oldAngularVelocity = default, float3 newAngularVelocity = default)
        {
            // Calculate the rotation difference as axis-angle
            quaternion delta = math.mul(oldRotation, math.conjugate(newRotation));
            
            // Ensure shortest path
            if (delta.value.w < 0)
                delta = new quaternion(-delta.value);
            
            delta = math.normalizesafe(delta);
            
            // Convert to axis-angle
            float sinHalfAngle = math.length(delta.value.xyz);
            if (sinHalfAngle > 0.0001f)
            {
                float halfAngle = math.asin(math.clamp(sinHalfAngle, -1f, 1f));
                float angle = 2f * halfAngle;
                float3 axis = delta.value.xyz / sinHalfAngle;
                _rotationOffset = axis * angle;
            }
            else
            {
                _rotationOffset = float3.zero;
            }
            
            _rotationVelocity = oldAngularVelocity - newAngularVelocity;
            _isActive = true;
        }
        
        /// <summary>
        /// Updates the blender, decaying the offsets over time.
        /// </summary>
        /// <param name="deltaTime">Time step.</param>
        public void Update(float deltaTime)
        {
            if (!_isActive) return;
            
            deltaTime = math.clamp(deltaTime, 0.0001f, 0.1f);
            
            // Calculate decay factor: 0.5^(dt/halfLife)
            float decay = math.pow(0.5f, deltaTime / _halfLife);
            
            // Decay position offset and velocity
            _positionOffset *= decay;
            _positionVelocity *= decay;
            
            // Decay rotation offset and velocity
            _rotationOffset *= decay;
            _rotationVelocity *= decay;
            
            // Deactivate when offsets are negligible
            float positionMagnitude = math.lengthsq(_positionOffset);
            float rotationMagnitude = math.lengthsq(_rotationOffset);
            
            if (positionMagnitude < 0.000001f && rotationMagnitude < 0.000001f)
            {
                _positionOffset = float3.zero;
                _rotationOffset = float3.zero;
                _positionVelocity = float3.zero;
                _rotationVelocity = float3.zero;
                _isActive = false;
            }
        }
        
        /// <summary>
        /// Applies the current inertialization offset to a pose.
        /// </summary>
        /// <param name="pose">The input pose from the new animation.</param>
        /// <returns>The pose with inertialization offset applied.</returns>
        public AnimationPose Apply(AnimationPose pose)
        {
            if (!_isActive)
                return pose;
            
            // Apply position offset
            pose.Position += _positionOffset;
            pose.Velocity += _positionVelocity;
            
            // Apply rotation offset (convert axis-angle back to quaternion)
            if (math.lengthsq(_rotationOffset) > 0.000001f)
            {
                float angle = math.length(_rotationOffset);
                float3 axis = _rotationOffset / angle;
                quaternion offsetRotation = quaternion.AxisAngle(axis, angle);
                pose.Rotation = math.mul(offsetRotation, pose.Rotation);
            }
            
            pose.AngularVelocity += _rotationVelocity;
            
            return pose;
        }
        
        /// <summary>
        /// Applies position offset only.
        /// </summary>
        public float3 ApplyPosition(float3 position)
        {
            return _isActive ? position + _positionOffset : position;
        }
        
        /// <summary>
        /// Applies rotation offset only.
        /// </summary>
        public quaternion ApplyRotation(quaternion rotation)
        {
            if (!_isActive || math.lengthsq(_rotationOffset) < 0.000001f)
                return rotation;
            
            float angle = math.length(_rotationOffset);
            float3 axis = _rotationOffset / angle;
            quaternion offsetRotation = quaternion.AxisAngle(axis, angle);
            return math.mul(offsetRotation, rotation);
        }
        
        /// <summary>
        /// Resets the blender, clearing all offsets.
        /// </summary>
        public void Reset()
        {
            _positionOffset = float3.zero;
            _positionVelocity = float3.zero;
            _rotationOffset = float3.zero;
            _rotationVelocity = float3.zero;
            _isActive = false;
        }
    }
    
    /// <summary>
    /// Manages inertialization for multiple bones.
    /// </summary>
    public class SkeletonInertializer
    {
        private InertializationBlender[] _blenders;
        private float _halfLife;
        
        /// <summary>
        /// Number of bones being inertialized.
        /// </summary>
        public int BoneCount => _blenders?.Length ?? 0;
        
        /// <summary>
        /// Creates a skeleton inertializer for the given number of bones.
        /// </summary>
        public SkeletonInertializer(int boneCount, float halfLife = 0.15f)
        {
            _halfLife = halfLife;
            _blenders = new InertializationBlender[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                _blenders[i] = InertializationBlender.Create(halfLife);
            }
        }
        
        /// <summary>
        /// Sets the half-life for all blenders.
        /// </summary>
        public void SetHalfLife(float halfLife)
        {
            _halfLife = halfLife;
            for (int i = 0; i < _blenders.Length; i++)
            {
                _blenders[i].SetHalfLife(halfLife);
            }
        }
        
        /// <summary>
        /// Initiates transitions for all bones.
        /// </summary>
        public void Transition(AnimationPose[] oldPoses, AnimationPose[] newPoses)
        {
            int count = math.min(math.min(oldPoses.Length, newPoses.Length), _blenders.Length);
            for (int i = 0; i < count; i++)
            {
                _blenders[i].Transition(oldPoses[i], newPoses[i]);
            }
        }
        
        /// <summary>
        /// Updates all blenders.
        /// </summary>
        public void Update(float deltaTime)
        {
            for (int i = 0; i < _blenders.Length; i++)
            {
                _blenders[i].Update(deltaTime);
            }
        }
        
        /// <summary>
        /// Applies inertialization to all poses.
        /// </summary>
        public void Apply(AnimationPose[] poses)
        {
            int count = math.min(poses.Length, _blenders.Length);
            for (int i = 0; i < count; i++)
            {
                poses[i] = _blenders[i].Apply(poses[i]);
            }
        }
        
        /// <summary>
        /// Resets all blenders.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < _blenders.Length; i++)
            {
                _blenders[i].Reset();
            }
        }
        
        /// <summary>
        /// Gets the blender for a specific bone.
        /// </summary>
        public ref InertializationBlender GetBlender(int boneIndex)
        {
            return ref _blenders[boneIndex];
        }
    }
}
