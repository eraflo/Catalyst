using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Perception
{
    /// <summary>
    /// Represents a limb chain (arm, leg, tentacle, etc.).
    /// A limb is a linear chain of bones ending at an effector.
    /// </summary>
    [Serializable]
    public class LimbChain
    {
        /// <summary>
        /// Name of this limb (e.g., "LeftArm", "RightLeg", "Tentacle_1").
        /// </summary>
        public string Name;
        
        /// <summary>
        /// The bones in this limb, from root to tip.
        /// </summary>
        public Transform[] Bones;
        
        /// <summary>
        /// The end effector (tip of the limb).
        /// </summary>
        public Transform Effector => Bones != null && Bones.Length > 0 ? Bones[Bones.Length - 1] : null;
        
        /// <summary>
        /// The root bone of this limb.
        /// </summary>
        public Transform Root => Bones != null && Bones.Length > 0 ? Bones[0] : null;
        
        /// <summary>
        /// Type of limb.
        /// </summary>
        public LimbType Type;
        
        /// <summary>
        /// Which side of the body this limb is on.
        /// </summary>
        public BodySide Side;
        
        /// <summary>
        /// Length of each bone segment.
        /// </summary>
        public float[] BoneLengths;
        
        /// <summary>
        /// Total length of the limb chain.
        /// </summary>
        public float TotalLength
        {
            get
            {
                if (BoneLengths == null) return 0f;
                float total = 0f;
                foreach (var length in BoneLengths)
                    total += length;
                return total;
            }
        }
        
        /// <summary>
        /// Estimated mass of this limb.
        /// </summary>
        public float Mass;
        
        /// <summary>
        /// Rest poses for each bone in local space.
        /// </summary>
        public AnimationPose[] RestPoses;
        
        /// <summary>
        /// Phase offset for gait pattern (0-1 where 0 is in sync with reference leg).
        /// </summary>
        public float GaitPhase;
        
        /// <summary>
        /// Whether this limb is currently grounded (for legs).
        /// </summary>
        public bool IsGrounded;
        
        /// <summary>
        /// Current IK target position (world space).
        /// </summary>
        public float3 IKTarget;
        
        /// <summary>
        /// Current IK pole/hint position (world space).
        /// </summary>
        public float3 IKPole;
        
        /// <summary>
        /// Number of bones in this limb.
        /// </summary>
        public int BoneCount => Bones?.Length ?? 0;
        
        /// <summary>
        /// Calculates the current world position of the effector.
        /// </summary>
        public float3 GetEffectorPosition()
        {
            return Effector != null ? (float3)Effector.position : float3.zero;
        }
        
        /// <summary>
        /// Stores the current poses as rest poses.
        /// </summary>
        public void CaptureRestPoses()
        {
            if (Bones == null) return;
            
            RestPoses = new AnimationPose[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                if (Bones[i] != null)
                {
                    RestPoses[i] = AnimationPose.FromTransformLocal(Bones[i]);
                }
            }
        }
        
        /// <summary>
        /// Calculates bone lengths from current positions.
        /// </summary>
        public void CalculateBoneLengths()
        {
            if (Bones == null || Bones.Length < 2)
            {
                BoneLengths = new float[] { 0f };
                return;
            }
            
            BoneLengths = new float[Bones.Length - 1];
            for (int i = 0; i < Bones.Length - 1; i++)
            {
                if (Bones[i] != null && Bones[i + 1] != null)
                {
                    BoneLengths[i] = Vector3.Distance(Bones[i].position, Bones[i + 1].position);
                }
            }
        }
    }
    
    /// <summary>
    /// Types of limbs.
    /// </summary>
    public enum LimbType
    {
        Unknown,
        Arm,
        Leg,
        Wing,
        Tail,
        Tentacle,
        Antenna,
        Finger,
        Custom
    }
    
    /// <summary>
    /// Body side for limb identification.
    /// </summary>
    public enum BodySide
    {
        Center,
        Left,
        Right,
        Front,
        Back,
        FrontLeft,
        FrontRight,
        BackLeft,
        BackRight
    }
    
    /// <summary>
    /// Represents a spine chain (vertebrae from hips to head, or tail).
    /// </summary>
    [Serializable]
    public class SpineChain
    {
        /// <summary>
        /// Name of this spine segment.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// The bones in this spine, from base to tip.
        /// </summary>
        public Transform[] Bones;
        
        /// <summary>
        /// Type of spine.
        /// </summary>
        public SpineType Type;
        
        /// <summary>
        /// Total length of the spine.
        /// </summary>
        public float TotalLength;
        
        /// <summary>
        /// Estimated mass of this spine segment.
        /// </summary>
        public float Mass;
        
        /// <summary>
        /// Rest poses for each vertebra.
        /// </summary>
        public AnimationPose[] RestPoses;
        
        /// <summary>
        /// Stiffness of this spine segment (0 = floppy, 1 = rigid).
        /// </summary>
        public float Stiffness = 0.5f;
        
        /// <summary>
        /// Number of bones in this spine.
        /// </summary>
        public int BoneCount => Bones?.Length ?? 0;
        
        /// <summary>
        /// The base bone of the spine.
        /// </summary>
        public Transform Base => Bones != null && Bones.Length > 0 ? Bones[0] : null;
        
        /// <summary>
        /// The root bone of the spine (alias for Base).
        /// </summary>
        public Transform Root => Base;
        
        /// <summary>
        /// The tip bone of the spine.
        /// </summary>
        public Transform Tip => Bones != null && Bones.Length > 0 ? Bones[Bones.Length - 1] : null;
        
        /// <summary>
        /// Stores the current poses as rest poses.
        /// </summary>
        public void CaptureRestPoses()
        {
            if (Bones == null) return;
            
            RestPoses = new AnimationPose[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                if (Bones[i] != null)
                {
                    RestPoses[i] = AnimationPose.FromTransformLocal(Bones[i]);
                }
            }
        }
        
        /// <summary>
        /// Calculates total length from bone positions.
        /// </summary>
        public void CalculateLength()
        {
            TotalLength = 0f;
            if (Bones == null || Bones.Length < 2) return;
            
            for (int i = 0; i < Bones.Length - 1; i++)
            {
                if (Bones[i] != null && Bones[i + 1] != null)
                {
                    TotalLength += Vector3.Distance(Bones[i].position, Bones[i + 1].position);
                }
            }
        }
    }
    
    /// <summary>
    /// Types of spine segments.
    /// </summary>
    public enum SpineType
    {
        Unknown,
        MainSpine,      // Hips to chest/neck
        Neck,           // Neck to head
        Tail,           // Hip to tail tip
        Body,           // For creatures like snakes/worms
        Custom
    }
}
