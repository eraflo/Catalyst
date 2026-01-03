using System;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Solvers
{
    /// <summary>
    /// Base interface for IK constraints.
    /// </summary>
    public interface IIKConstraint
    {
        /// <summary>
        /// Applies the constraint to a joint.
        /// </summary>
        /// <param name="jointIndex">Index of the joint in the chain.</param>
        /// <param name="position">Current position (will be modified).</param>
        /// <param name="rotation">Current rotation (will be modified).</param>
        /// <param name="parentRotation">Rotation of the parent joint.</param>
        void Apply(int jointIndex, ref float3 position, ref quaternion rotation, quaternion parentRotation);
    }
    
    /// <summary>
    /// Limits joint rotation to a cone around the parent's forward axis.
    /// Good for ball-and-socket joints like hips and shoulders.
    /// </summary>
    [Serializable]
    public class ConeConstraint : IIKConstraint
    {
        [Tooltip("Maximum angle from parent forward axis in degrees.")]
        [Range(0f, 180f)]
        public float MaxAngle = 90f;
        
        public void Apply(int jointIndex, ref float3 position, ref quaternion rotation, quaternion parentRotation)
        {
            if (MaxAngle >= 180f) return;
            
            // Get joint direction in parent space
            float3 parentForward = math.forward(parentRotation);
            float3 jointDir = math.normalizesafe(math.forward(rotation));
            
            // Calculate angle
            float angle = math.acos(math.clamp(math.dot(parentForward, jointDir), -1f, 1f));
            float maxRad = math.radians(MaxAngle);
            
            if (angle > maxRad)
            {
                // Clamp to cone
                float3 axis = math.normalizesafe(math.cross(parentForward, jointDir));
                if (math.lengthsq(axis) < 0.01f)
                    axis = math.mul(parentRotation, new float3(0, 1, 0));
                
                quaternion clampedRot = math.mul(parentRotation, quaternion.AxisAngle(axis, maxRad));
                rotation = clampedRot;
            }
        }
    }
    
    /// <summary>
    /// Limits joint rotation to a hinge (single axis) like elbows and knees.
    /// </summary>
    [Serializable]
    public class HingeConstraint : IIKConstraint
    {
        [Tooltip("Axis of rotation in local space.")]
        public float3 Axis = new float3(1, 0, 0);
        
        [Tooltip("Minimum angle in degrees.")]
        [Range(-180f, 0f)]
        public float MinAngle = -170f;
        
        [Tooltip("Maximum angle in degrees.")]
        [Range(0f, 180f)]
        public float MaxAngle = 0f;
        
        public void Apply(int jointIndex, ref float3 position, ref quaternion rotation, quaternion parentRotation)
        {
            // Convert rotation to local space
            quaternion localRot = math.mul(math.conjugate(parentRotation), rotation);
            
            // Extract rotation around the hinge axis
            float3 forward = math.forward(localRot);
            float3 normalizedAxis = math.normalizesafe(Axis);
            
            // Project forward onto plane perpendicular to hinge axis
            float3 projectedForward = forward - math.dot(forward, normalizedAxis) * normalizedAxis;
            projectedForward = math.normalizesafe(projectedForward);
            
            // Calculate current angle
            float3 reference = new float3(0, 0, 1);
            if (math.abs(math.dot(normalizedAxis, reference)) > 0.99f)
                reference = new float3(0, 1, 0);
            
            float3 refOnPlane = reference - math.dot(reference, normalizedAxis) * normalizedAxis;
            refOnPlane = math.normalizesafe(refOnPlane);
            
            float angle = math.atan2(
                math.dot(math.cross(refOnPlane, projectedForward), normalizedAxis),
                math.dot(refOnPlane, projectedForward)
            );
            
            // Clamp angle
            float minRad = math.radians(MinAngle);
            float maxRad = math.radians(MaxAngle);
            float clampedAngle = math.clamp(angle, minRad, maxRad);
            
            if (math.abs(angle - clampedAngle) > 0.001f)
            {
                // Apply clamped rotation
                quaternion hingeRot = quaternion.AxisAngle(normalizedAxis, clampedAngle);
                rotation = math.mul(parentRotation, hingeRot);
            }
        }
    }
    
    /// <summary>
    /// Limits twist (roll) around the bone axis.
    /// </summary>
    [Serializable]
    public class TwistConstraint : IIKConstraint
    {
        [Tooltip("Maximum twist angle in degrees.")]
        [Range(0f, 180f)]
        public float MaxTwist = 45f;
        
        public void Apply(int jointIndex, ref float3 position, ref quaternion rotation, quaternion parentRotation)
        {
            // Extract twist component (rotation around forward axis)
            float3 forward = math.forward(rotation);
            Unity.Mathematics.float3 parentUp = Unity.Mathematics.math.mul(parentRotation, new Unity.Mathematics.float3(0, 1, 0));
            
            // Project parent up onto plane perpendicular to forward
            Unity.Mathematics.float3 projectedUp = parentUp - Unity.Mathematics.math.dot(parentUp, forward) * forward;
            projectedUp = Unity.Mathematics.math.normalizesafe(projectedUp);
            
            Unity.Mathematics.float3 currentUp = Unity.Mathematics.math.mul(rotation, new Unity.Mathematics.float3(0, 1, 0));
            float3 projectedCurrentUp = currentUp - math.dot(currentUp, forward) * forward;
            projectedCurrentUp = math.normalizesafe(projectedCurrentUp);
            
            // Calculate twist angle
            float twistAngle = math.atan2(
                math.dot(math.cross(projectedUp, projectedCurrentUp), forward),
                math.dot(projectedUp, projectedCurrentUp)
            );
            
            // Clamp twist
            float maxRad = math.radians(MaxTwist);
            float clampedTwist = math.clamp(twistAngle, -maxRad, maxRad);
            
            if (math.abs(twistAngle - clampedTwist) > 0.001f)
            {
                // Remove excess twist
                float correction = twistAngle - clampedTwist;
                quaternion twistCorrection = quaternion.AxisAngle(forward, -correction);
                rotation = math.mul(twistCorrection, rotation);
            }
        }
    }
    
    /// <summary>
    /// Applies multiple constraints to a joint chain.
    /// </summary>
    public class ConstraintChain
    {
        private readonly IIKConstraint[][] _constraints;
        private readonly int _jointCount;
        
        /// <summary>
        /// Creates a constraint chain for the given number of joints.
        /// </summary>
        public ConstraintChain(int jointCount)
        {
            _jointCount = jointCount;
            _constraints = new IIKConstraint[jointCount][];
            for (int i = 0; i < jointCount; i++)
            {
                _constraints[i] = Array.Empty<IIKConstraint>();
            }
        }
        
        /// <summary>
        /// Adds a constraint to a specific joint.
        /// </summary>
        public void AddConstraint(int jointIndex, IIKConstraint constraint)
        {
            if (jointIndex < 0 || jointIndex >= _jointCount) return;
            
            var existing = _constraints[jointIndex];
            var newArray = new IIKConstraint[existing.Length + 1];
            Array.Copy(existing, newArray, existing.Length);
            newArray[existing.Length] = constraint;
            _constraints[jointIndex] = newArray;
        }
        
        /// <summary>
        /// Applies all constraints to joint rotations.
        /// </summary>
        public void Apply(float3[] positions, quaternion[] rotations)
        {
            if (positions.Length != rotations.Length) return;
            
            quaternion parentRot = quaternion.identity;
            
            for (int i = 0; i < math.min(positions.Length, _jointCount); i++)
            {
                float3 pos = positions[i];
                quaternion rot = rotations[i];
                
                foreach (var constraint in _constraints[i])
                {
                    constraint.Apply(i, ref pos, ref rot, parentRot);
                }
                
                positions[i] = pos;
                rotations[i] = rot;
                parentRot = rot;
            }
        }
        
        /// <summary>
        /// Creates a typical arm constraint chain.
        /// </summary>
        public static ConstraintChain CreateArmChain()
        {
            var chain = new ConstraintChain(4);
            
            // Shoulder - cone constraint
            chain.AddConstraint(0, new ConeConstraint { MaxAngle = 120f });
            chain.AddConstraint(0, new TwistConstraint { MaxTwist = 90f });
            
            // Elbow - hinge constraint
            chain.AddConstraint(1, new HingeConstraint 
            { 
                Axis = new float3(1, 0, 0), 
                MinAngle = -150f, 
                MaxAngle = 0f 
            });
            
            // Wrist - limited cone
            chain.AddConstraint(2, new ConeConstraint { MaxAngle = 80f });
            chain.AddConstraint(2, new TwistConstraint { MaxTwist = 90f });
            
            return chain;
        }
        
        /// <summary>
        /// Creates a typical leg constraint chain.
        /// </summary>
        public static ConstraintChain CreateLegChain()
        {
            var chain = new ConstraintChain(4);
            
            // Hip - cone constraint
            chain.AddConstraint(0, new ConeConstraint { MaxAngle = 90f });
            chain.AddConstraint(0, new TwistConstraint { MaxTwist = 45f });
            
            // Knee - hinge constraint (bends backward)
            chain.AddConstraint(1, new HingeConstraint 
            { 
                Axis = new float3(1, 0, 0), 
                MinAngle = 0f, 
                MaxAngle = 150f 
            });
            
            // Ankle - limited cone
            chain.AddConstraint(2, new ConeConstraint { MaxAngle = 45f });
            
            return chain;
        }
    }
}
