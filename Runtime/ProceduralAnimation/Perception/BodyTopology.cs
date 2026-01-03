using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Perception
{
    /// <summary>
    /// Represents the analyzed topology of a character's body.
    /// Contains information about the skeleton structure, limbs, spine, and mass distribution.
    /// </summary>
    [Serializable]
    public class BodyTopology
    {
        /// <summary>
        /// Root transform of the skeleton.
        /// </summary>
        public Transform Root;
        
        /// <summary>
        /// The center of mass bone (usually hips/pelvis).
        /// </summary>
        public Transform CenterOfMass;
        
        /// <summary>
        /// All limbs detected in the skeleton.
        /// </summary>
        public List<LimbChain> Limbs = new List<LimbChain>();
        
        /// <summary>
        /// All spine segments detected.
        /// </summary>
        public List<SpineChain> Spines = new List<SpineChain>();
        
        /// <summary>
        /// All bones in the skeleton with their data.
        /// Uses the unified BoneData type from Types namespace.
        /// </summary>
        public List<BoneData> AllBones = new List<BoneData>();
        
        /// <summary>
        /// The detected morphology type.
        /// </summary>
        public MorphologyType Morphology = MorphologyType.Unknown;
        
        /// <summary>
        /// Total estimated mass of the character.
        /// </summary>
        public float TotalMass;
        
        /// <summary>
        /// Character height (from lowest to highest point).
        /// </summary>
        public float Height;
        
        /// <summary>
        /// Characteristic size (for scaling calculations).
        /// </summary>
        public float CharacteristicSize;
        
        /// <summary>
        /// Whether the topology has been successfully analyzed.
        /// </summary>
        public bool IsValid => Root != null && CenterOfMass != null;
        
        #region Accessors
        
        /// <summary>
        /// Number of legs detected.
        /// </summary>
        public int LegCount
        {
            get
            {
                int count = 0;
                foreach (var limb in Limbs)
                {
                    if (limb.Type == LimbType.Leg) count++;
                }
                return count;
            }
        }
        
        /// <summary>
        /// Number of arms detected.
        /// </summary>
        public int ArmCount
        {
            get
            {
                int count = 0;
                foreach (var limb in Limbs)
                {
                    if (limb.Type == LimbType.Arm) count++;
                }
                return count;
            }
        }
        
        /// <summary>
        /// Gets all legs.
        /// </summary>
        public LimbChain[] GetLegs()
        {
            var legs = new List<LimbChain>();
            foreach (var limb in Limbs)
            {
                if (limb.Type == LimbType.Leg) legs.Add(limb);
            }
            return legs.ToArray();
        }
        
        /// <summary>
        /// Gets all arms.
        /// </summary>
        public LimbChain[] GetArms()
        {
            var arms = new List<LimbChain>();
            foreach (var limb in Limbs)
            {
                if (limb.Type == LimbType.Arm) arms.Add(limb);
            }
            return arms.ToArray();
        }
        
        /// <summary>
        /// Gets the main spine (hips to chest/head).
        /// </summary>
        public SpineChain GetMainSpine()
        {
            foreach (var spine in Spines)
            {
                if (spine.Type == SpineType.MainSpine) return spine;
            }
            return Spines.Count > 0 ? Spines[0] : null;
        }
        
        /// <summary>
        /// Gets the tail if present.
        /// </summary>
        public SpineChain GetTail()
        {
            foreach (var spine in Spines)
            {
                if (spine.Type == SpineType.Tail) return spine;
            }
            return null;
        }
        
        /// <summary>
        /// Gets a limb by name.
        /// </summary>
        public LimbChain GetLimb(string name)
        {
            foreach (var limb in Limbs)
            {
                if (string.Equals(limb.Name, name, StringComparison.OrdinalIgnoreCase))
                    return limb;
            }
            return null;
        }
        
        /// <summary>
        /// Gets a bone by transform.
        /// </summary>
        public BoneData GetBone(Transform transform)
        {
            foreach (var bone in AllBones)
            {
                if (bone.Transform == transform) return bone;
            }
            return null;
        }
        
        /// <summary>
        /// Gets a bone by index.
        /// </summary>
        public BoneData GetBone(int index)
        {
            if (index >= 0 && index < AllBones.Count)
                return AllBones[index];
            return null;
        }
        
        #endregion
        
        #region Gait Helpers
        
        /// <summary>
        /// Assigns gait phases to legs based on the detected morphology.
        /// </summary>
        public void AssignGaitPhases()
        {
            var legs = GetLegs();
            if (legs.Length == 0) return;
            
            switch (Morphology)
            {
                case MorphologyType.Biped:
                    AssignBipedGait(legs);
                    break;
                case MorphologyType.Quadruped:
                    AssignQuadrupedGait(legs);
                    break;
                case MorphologyType.Hexapod:
                    AssignHexapodGait(legs);
                    break;
                case MorphologyType.Octopod:
                    AssignOctopodGait(legs);
                    break;
                default:
                    AssignGenericGait(legs);
                    break;
            }
        }
        
        private void AssignBipedGait(LimbChain[] legs)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                legs[i].GaitPhase = legs[i].Side == BodySide.Left ? 0f : 0.5f;
            }
        }
        
        private void AssignQuadrupedGait(LimbChain[] legs)
        {
            foreach (var leg in legs)
            {
                switch (leg.Side)
                {
                    case BodySide.FrontLeft:
                    case BodySide.BackRight:
                        leg.GaitPhase = 0f;
                        break;
                    case BodySide.FrontRight:
                    case BodySide.BackLeft:
                        leg.GaitPhase = 0.5f;
                        break;
                }
            }
        }
        
        private void AssignHexapodGait(LimbChain[] legs)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                legs[i].GaitPhase = (i % 2 == 0) ? 0f : 0.5f;
            }
        }
        
        private void AssignOctopodGait(LimbChain[] legs)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                legs[i].GaitPhase = (float)i / legs.Length;
            }
        }
        
        private void AssignGenericGait(LimbChain[] legs)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                legs[i].GaitPhase = (float)i / legs.Length;
            }
        }
        
        #endregion
        
        /// <summary>
        /// Captures rest poses for all limbs and spines.
        /// </summary>
        public void CaptureRestPoses()
        {
            foreach (var limb in Limbs)
            {
                limb.CaptureRestPoses();
                limb.CalculateBoneLengths();
            }
            
            foreach (var spine in Spines)
            {
                spine.CaptureRestPoses();
                spine.CalculateLength();
            }
            
            foreach (var bone in AllBones)
            {
                bone.CaptureRestPose();
            }
        }
        
        /// <summary>
        /// Calculates the current world-space center of mass.
        /// </summary>
        public float3 CalculateCenterOfMass()
        {
            if (TotalMass <= 0f || AllBones.Count == 0)
            {
                return CenterOfMass != null ? (float3)CenterOfMass.position : float3.zero;
            }
            
            float3 weightedSum = float3.zero;
            foreach (var bone in AllBones)
            {
                if (bone.Transform != null)
                {
                    weightedSum += (float3)bone.Transform.position * bone.Mass;
                }
            }
            
            return weightedSum / TotalMass;
        }
    }
    
    /// <summary>
    /// General morphology types.
    /// </summary>
    public enum MorphologyType
    {
        Unknown,
        Biped,          // 2 legs (humanoid, bird)
        Quadruped,      // 4 legs (dog, horse)
        Hexapod,        // 6 legs (insect)
        Octopod,        // 8 legs (spider, octopus)
        Serpentine,     // No legs (snake, worm)
        Centipede,      // Many legs
        Custom          // User-defined
    }
}
