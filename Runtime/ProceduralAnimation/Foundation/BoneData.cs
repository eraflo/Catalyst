using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Represents a bone in an animation skeleton with full topology information.
    /// This is the unified bone type used across the procedural animation system.
    /// </summary>
    [System.Serializable]
    public class BoneData
    {
        /// <summary>
        /// The transform of this bone.
        /// </summary>
        public Transform Transform;
        
        /// <summary>
        /// Name of the bone.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// Index of the parent bone (-1 for root).
        /// </summary>
        public int ParentIndex = -1;
        
        /// <summary>
        /// Indices of child bones.
        /// </summary>
        public List<int> ChildIndices = new List<int>();
        
        /// <summary>
        /// Depth in the hierarchy (0 = root).
        /// </summary>
        public int Depth;
        
        /// <summary>
        /// Length of this bone (distance to first child or end).
        /// </summary>
        public float Length;
        
        /// <summary>
        /// Estimated mass of this bone segment.
        /// </summary>
        public float Mass;
        
        /// <summary>
        /// Estimated volume of this bone's mesh.
        /// </summary>
        public float Volume;
        
        /// <summary>
        /// Rest pose in local space.
        /// </summary>
        public AnimationPose RestPose;
        
        /// <summary>
        /// Current pose in local space.
        /// </summary>
        public AnimationPose CurrentPose;
        
        /// <summary>
        /// Bone type for semantic identification.
        /// </summary>
        public BoneType Type = BoneType.Unknown;
        
        /// <summary>
        /// Whether this bone is a "hub" (has 3+ children).
        /// </summary>
        public bool IsHub => ChildIndices.Count >= 3;
        
        /// <summary>
        /// Whether this bone is a leaf (no children).
        /// </summary>
        public bool IsLeaf => ChildIndices.Count == 0;
        
        /// <summary>
        /// Whether this bone is part of a simple chain (1 child).
        /// </summary>
        public bool IsChain => ChildIndices.Count == 1;
        
        /// <summary>
        /// Creates an empty BoneData.
        /// </summary>
        public BoneData() { }
        
        /// <summary>
        /// Creates a BoneData from a transform.
        /// </summary>
        public static BoneData FromTransform(Transform transform, int parentIndex = -1)
        {
            return new BoneData
            {
                Transform = transform,
                Name = transform != null ? transform.name : "",
                ParentIndex = parentIndex,
                RestPose = transform != null ? AnimationPose.FromTransformLocal(transform) : AnimationPose.Identity
            };
        }
        
        /// <summary>
        /// Captures current pose from transform.
        /// </summary>
        public void CaptureCurrentPose()
        {
            if (Transform != null)
            {
                CurrentPose = AnimationPose.FromTransformLocal(Transform);
            }
        }
        
        /// <summary>
        /// Captures rest pose from current transform state.
        /// </summary>
        public void CaptureRestPose()
        {
            if (Transform != null)
            {
                RestPose = AnimationPose.FromTransformLocal(Transform);
            }
        }
    }
    
    /// <summary>
    /// Types of bones for semantic identification.
    /// </summary>
    public enum BoneType
    {
        Unknown,
        Root,
        Hips,
        Spine,
        Chest,
        Neck,
        Head,
        Shoulder,
        UpperArm,
        LowerArm,
        Hand,
        Finger,
        UpperLeg,
        LowerLeg,
        Foot,
        Toe,
        Tail,
        Custom
    }
    
    /// <summary>
    /// Represents a chain of bones (e.g., arm, leg, spine).
    /// </summary>
    [System.Serializable]
    public class BoneChain
    {
        /// <summary>
        /// Name of this chain.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// Indices of bones in this chain, from root to tip.
        /// </summary>
        public int[] BoneIndices;
        
        /// <summary>
        /// Type of chain.
        /// </summary>
        public ChainType Type;
        
        /// <summary>
        /// Total length of the chain.
        /// </summary>
        public float TotalLength;
        
        /// <summary>
        /// IK target for this chain (if applicable).
        /// </summary>
        public Transform IKTarget;
        
        /// <summary>
        /// IK pole/hint target for this chain (if applicable).
        /// </summary>
        public Transform IKPole;
        
        /// <summary>
        /// Whether this chain currently has an active IK target.
        /// </summary>
        public bool HasIKTarget => IKTarget != null;
    }
    
    /// <summary>
    /// Types of bone chains.
    /// </summary>
    public enum ChainType
    {
        Unknown,
        Spine,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg,
        Tail,
        Neck,
        LeftFinger,
        RightFinger,
        Custom
    }
    
    /// <summary>
    /// State of a single spring for bone animation.
    /// </summary>
    public struct BoneSpringState
    {
        public float3 PositionVelocity;
        public float3 RotationVelocity;
    }
}
