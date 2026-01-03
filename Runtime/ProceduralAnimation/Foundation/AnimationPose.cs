using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Represents a pose (position + rotation) with optional velocity data.
    /// Used throughout the animation system for pose blending and transitions.
    /// </summary>
    [System.Serializable]
    public struct AnimationPose
    {
        /// <summary>
        /// World-space position.
        /// </summary>
        public float3 Position;
        
        /// <summary>
        /// World-space rotation.
        /// </summary>
        public quaternion Rotation;
        
        /// <summary>
        /// Linear velocity (optional, for inertialization).
        /// </summary>
        public float3 Velocity;
        
        /// <summary>
        /// Angular velocity in radians/sec (optional, for inertialization).
        /// </summary>
        public float3 AngularVelocity;
        
        /// <summary>
        /// Creates a pose from position and rotation.
        /// </summary>
        public AnimationPose(float3 position, quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
            Velocity = float3.zero;
            AngularVelocity = float3.zero;
        }
        
        /// <summary>
        /// Creates a pose from a Transform.
        /// </summary>
        public static AnimationPose FromTransform(Transform transform)
        {
            return new AnimationPose
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Velocity = float3.zero,
                AngularVelocity = float3.zero
            };
        }
        
        /// <summary>
        /// Creates a pose from a Transform's local space.
        /// </summary>
        public static AnimationPose FromTransformLocal(Transform transform)
        {
            return new AnimationPose
            {
                Position = transform.localPosition,
                Rotation = transform.localRotation,
                Velocity = float3.zero,
                AngularVelocity = float3.zero
            };
        }
        
        /// <summary>
        /// Applies this pose to a Transform in world space.
        /// </summary>
        public void ApplyToTransform(Transform transform)
        {
            transform.position = Position;
            transform.rotation = Rotation;
        }
        
        /// <summary>
        /// Applies this pose to a Transform in local space.
        /// </summary>
        public void ApplyToTransformLocal(Transform transform)
        {
            transform.localPosition = Position;
            transform.localRotation = Rotation;
        }
        
        /// <summary>
        /// Linearly interpolates between two poses.
        /// </summary>
        public static AnimationPose Lerp(AnimationPose a, AnimationPose b, float t)
        {
            return new AnimationPose
            {
                Position = math.lerp(a.Position, b.Position, t),
                Rotation = math.slerp(a.Rotation, b.Rotation, t),
                Velocity = math.lerp(a.Velocity, b.Velocity, t),
                AngularVelocity = math.lerp(a.AngularVelocity, b.AngularVelocity, t)
            };
        }
        
        /// <summary>
        /// Calculates the difference between two poses.
        /// </summary>
        public static AnimationPose Delta(AnimationPose from, AnimationPose to)
        {
            return new AnimationPose
            {
                Position = to.Position - from.Position,
                Rotation = math.mul(to.Rotation, math.conjugate(from.Rotation)),
                Velocity = to.Velocity - from.Velocity,
                AngularVelocity = to.AngularVelocity - from.AngularVelocity
            };
        }
        
        /// <summary>
        /// Adds a delta pose to this pose.
        /// </summary>
        public AnimationPose Add(AnimationPose delta)
        {
            return new AnimationPose
            {
                Position = Position + delta.Position,
                Rotation = math.mul(delta.Rotation, Rotation),
                Velocity = Velocity + delta.Velocity,
                AngularVelocity = AngularVelocity + delta.AngularVelocity
            };
        }
        
        /// <summary>
        /// Scales the pose by a factor.
        /// </summary>
        public AnimationPose Scale(float factor)
        {
            // For rotation, we need to scale the angle
            float angle = 0f;
            float3 axis = float3.zero;
            
            // Extract axis-angle from quaternion
            float sinHalfAngle = math.length(Rotation.value.xyz);
            if (sinHalfAngle > 0.0001f)
            {
                float halfAngle = math.asin(math.clamp(sinHalfAngle, -1f, 1f));
                angle = 2f * halfAngle * factor;
                axis = Rotation.value.xyz / sinHalfAngle;
            }
            
            return new AnimationPose
            {
                Position = Position * factor,
                Rotation = sinHalfAngle > 0.0001f ? quaternion.AxisAngle(axis, angle) : quaternion.identity,
                Velocity = Velocity * factor,
                AngularVelocity = AngularVelocity * factor
            };
        }
        
        /// <summary>
        /// Identity pose (zero position, identity rotation).
        /// </summary>
        public static AnimationPose Identity => new AnimationPose
        {
            Position = float3.zero,
            Rotation = quaternion.identity,
            Velocity = float3.zero,
            AngularVelocity = float3.zero
        };
    }
}
