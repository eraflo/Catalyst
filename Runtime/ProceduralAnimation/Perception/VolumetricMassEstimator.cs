using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Perception
{
    /// <summary>
    /// Estimates the mass of bones based on mesh volumes.
    /// Uses bounding box approximation for fast calculation.
    /// </summary>
    public static class VolumetricMassEstimator
    {
        /// <summary>
        /// Configuration for mass estimation.
        /// </summary>
        public struct EstimatorConfig
        {
            /// <summary>
            /// Density in kg per cubic meter. Default is similar to human body (~1000 kg/m³).
            /// </summary>
            public float Density;
            
            /// <summary>
            /// Minimum mass to assign to any bone (kg).
            /// </summary>
            public float MinMass;
            
            /// <summary>
            /// Maximum mass to assign to any bone (kg).
            /// </summary>
            public float MaxMass;
            
            /// <summary>
            /// Whether to use mesh bounds for estimation (more accurate but slower).
            /// If false, uses bone length approximation.
            /// </summary>
            public bool UseMeshBounds;
            
            /// <summary>
            /// Default bone radius as fraction of bone length (when no mesh available).
            /// </summary>
            public float DefaultBoneRadiusFraction;
            
            /// <summary>
            /// Mass multipliers for specific bone types.
            /// </summary>
            public Dictionary<BoneType, float> BoneTypeMultipliers;
            
            /// <summary>
            /// Default configuration.
            /// </summary>
            public static EstimatorConfig Default => new EstimatorConfig
            {
                Density = 1000f, // Similar to water/human tissue
                MinMass = 0.01f,
                MaxMass = 100f,
                UseMeshBounds = true,
                DefaultBoneRadiusFraction = 0.15f,
                BoneTypeMultipliers = new Dictionary<BoneType, float>
                {
                    { BoneType.Hips, 1.5f },
                    { BoneType.Chest, 1.3f },
                    { BoneType.Head, 0.8f },
                    { BoneType.UpperLeg, 1.2f },
                    { BoneType.LowerLeg, 0.9f },
                    { BoneType.Foot, 0.5f },
                    { BoneType.UpperArm, 0.7f },
                    { BoneType.LowerArm, 0.5f },
                    { BoneType.Hand, 0.3f },
                    { BoneType.Finger, 0.1f }
                }
            };
        }
        
        /// <summary>
        /// Estimates masses for all bones in a topology.
        /// </summary>
        /// <param name="topology">The body topology to estimate masses for.</param>
        /// <param name="config">Estimation configuration.</param>
        /// <returns>Total estimated mass.</returns>
        public static float EstimateMasses(BodyTopology topology, EstimatorConfig config = default)
        {
            if (config.Density == 0f)
                config = EstimatorConfig.Default;
            
            float totalMass = 0f;
            
            foreach (var bone in topology.AllBones)
            {
                float volume = EstimateBoneVolume(bone, config);
                float mass = volume * config.Density;
                
                // Apply bone type multiplier
                if (config.BoneTypeMultipliers != null && 
                    config.BoneTypeMultipliers.TryGetValue(bone.Type, out float multiplier))
                {
                    mass *= multiplier;
                }
                
                // Clamp mass
                mass = math.clamp(mass, config.MinMass, config.MaxMass);
                
                bone.Mass = mass;
                bone.Volume = volume;
                totalMass += mass;
            }
            
            topology.TotalMass = totalMass;
            
            // Also update limb masses
            UpdateLimbMasses(topology);
            UpdateSpineMasses(topology);
            
            return totalMass;
        }
        
        /// <summary>
        /// Estimates the volume of a single bone segment.
        /// </summary>
        private static float EstimateBoneVolume(BoneData bone, EstimatorConfig config)
        {
            if (bone.Transform == null)
                return 0f;
            
            float volume = 0f;
            
            if (config.UseMeshBounds)
            {
                // Try to find renderers for this bone
                volume = EstimateVolumeFromMesh(bone.Transform);
            }
            
            // Fallback to cylinder approximation if no mesh volume
            if (volume <= 0f && bone.Length > 0f)
            {
                float radius = bone.Length * config.DefaultBoneRadiusFraction;
                volume = math.PI * radius * radius * bone.Length;
            }
            
            // Minimum volume based on position
            if (volume <= 0f)
            {
                volume = 0.0001f; // 0.1 cm³ minimum
            }
            
            return volume;
        }
        
        /// <summary>
        /// Estimates volume from mesh bounds on the bone and its children.
        /// </summary>
        private static float EstimateVolumeFromMesh(Transform bone)
        {
            float totalVolume = 0f;
            
            // Check this bone's renderers
            var renderers = bone.GetComponents<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer smr)
                {
                    totalVolume += EstimateVolumeFromBounds(smr.bounds);
                }
                else if (renderer is MeshRenderer mr)
                {
                    totalVolume += EstimateVolumeFromBounds(mr.bounds);
                }
            }
            
            // For skinned meshes, check the root for the full mesh
            if (totalVolume <= 0f)
            {
                // Look for skinned mesh renderer that might affect this bone
                var transform = bone;
                while (transform != null && totalVolume <= 0f)
                {
                    var smrs = transform.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var smr in smrs)
                    {
                        // Check if this bone is in the bone list
                        if (smr.bones != null)
                        {
                            foreach (var smrBone in smr.bones)
                            {
                                if (smrBone == bone)
                                {
                                    // Estimate based on bone weight/influence
                                    // This is a rough approximation
                                    totalVolume = EstimateVolumeFromBounds(smr.bounds) / smr.bones.Length;
                                    break;
                                }
                            }
                        }
                        if (totalVolume > 0f) break;
                    }
                    transform = transform.parent;
                }
            }
            
            return totalVolume;
        }
        
        /// <summary>
        /// Estimates volume from AABB bounds (box approximation).
        /// Applies a reduction factor since real meshes don't fill the entire box.
        /// </summary>
        private static float EstimateVolumeFromBounds(Bounds bounds)
        {
            float3 size = bounds.size;
            float boxVolume = size.x * size.y * size.z;
            
            // Reduce by ~40% since organic shapes don't fill the full bounding box
            return boxVolume * 0.6f;
        }
        
        /// <summary>
        /// Updates the total mass of each limb.
        /// </summary>
        private static void UpdateLimbMasses(BodyTopology topology)
        {
            foreach (var limb in topology.Limbs)
            {
                float mass = 0f;
                foreach (var bone in limb.Bones)
                {
                    var boneData = topology.GetBone(bone);
                    if (boneData != null)
                    {
                        mass += boneData.Mass;
                    }
                }
                limb.Mass = mass;
            }
        }
        
        /// <summary>
        /// Updates the total mass of each spine.
        /// </summary>
        private static void UpdateSpineMasses(BodyTopology topology)
        {
            foreach (var spine in topology.Spines)
            {
                float mass = 0f;
                foreach (var bone in spine.Bones)
                {
                    var boneData = topology.GetBone(bone);
                    if (boneData != null)
                    {
                        mass += boneData.Mass;
                    }
                }
                spine.Mass = mass;
            }
        }
        
        /// <summary>
        /// Normalizes masses so they sum to a target total.
        /// Useful for ensuring consistent physics behavior.
        /// </summary>
        /// <param name="topology">The topology to normalize.</param>
        /// <param name="targetTotalMass">Target total mass in kg.</param>
        public static void NormalizeMasses(BodyTopology topology, float targetTotalMass)
        {
            if (topology.TotalMass <= 0f || targetTotalMass <= 0f)
                return;
            
            float scale = targetTotalMass / topology.TotalMass;
            
            foreach (var bone in topology.AllBones)
            {
                bone.Mass *= scale;
            }
            
            foreach (var limb in topology.Limbs)
            {
                limb.Mass *= scale;
            }
            
            foreach (var spine in topology.Spines)
            {
                spine.Mass *= scale;
            }
            
            topology.TotalMass = targetTotalMass;
        }
    }
}
