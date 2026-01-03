using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Perception
{
    /// <summary>
    /// Analyzes the topology of a skeleton using graph traversal algorithms.
    /// Identifies hubs, limbs, spines, and classifies the morphology.
    /// </summary>
    public static class GraphTopologyAnalyzer
    {
        /// <summary>
        /// Configuration for the topology analyzer.
        /// </summary>
        public struct AnalyzerConfig
        {
            /// <summary>
            /// Minimum bone length to consider valid (in meters).
            /// </summary>
            public float MinBoneLength;
            
            /// <summary>
            /// Maximum depth to search in the hierarchy.
            /// </summary>
            public int MaxDepth;
            
            /// <summary>
            /// Whether to auto-detect limb types based on naming conventions.
            /// </summary>
            public bool UseNamingHeuristics;
            
            /// <summary>
            /// Keywords for left side detection.
            /// </summary>
            public string[] LeftKeywords;
            
            /// <summary>
            /// Keywords for right side detection.
            /// </summary>
            public string[] RightKeywords;
            
            /// <summary>
            /// Keywords for arm detection.
            /// </summary>
            public string[] ArmKeywords;
            
            /// <summary>
            /// Keywords for leg detection.
            /// </summary>
            public string[] LegKeywords;
            
            /// <summary>
            /// Keywords for spine detection.
            /// </summary>
            public string[] SpineKeywords;
            
            /// <summary>
            /// Keywords for tail detection.
            /// </summary>
            public string[] TailKeywords;
            
            /// <summary>
            /// Default configuration with common naming conventions.
            /// </summary>
            public static AnalyzerConfig Default => new AnalyzerConfig
            {
                MinBoneLength = 0.001f,
                MaxDepth = 50,
                UseNamingHeuristics = true,
                LeftKeywords = new[] { "left", "l_", "_l", ".l", "gauche" },
                RightKeywords = new[] { "right", "r_", "_r", ".r", "droit" },
                ArmKeywords = new[] { "arm", "hand", "shoulder", "clavicle", "elbow", "wrist", "bras", "main" },
                LegKeywords = new[] { "leg", "foot", "thigh", "calf", "ankle", "toe", "hip", "jambe", "pied" },
                SpineKeywords = new[] { "spine", "chest", "torso", "abdomen", "pelvis", "hips", "colonne" },
                TailKeywords = new[] { "tail", "queue" }
            };
        }
        
        /// <summary>
        /// Analyzes the skeleton starting from the given root transform.
        /// </summary>
        /// <param name="root">Root transform of the skeleton.</param>
        /// <param name="config">Analyzer configuration.</param>
        /// <returns>The analyzed body topology.</returns>
        public static BodyTopology Analyze(Transform root, AnalyzerConfig config = default)
        {
            if (config.MaxDepth == 0)
                config = AnalyzerConfig.Default;
            
            var topology = new BodyTopology
            {
                Root = root
            };
            
            if (root == null)
            {
                Debug.LogWarning("[GraphTopologyAnalyzer] Root transform is null.");
                return topology;
            }
            
            // Phase 1: BFS traversal to build bone list
            BuildBoneList(root, topology, config);
            
            // Phase 2: Identify hubs (bones with 3+ children)
            var hubs = FindHubs(topology);
            
            // Phase 3: Find center of mass (usually the root hub or pelvis)
            topology.CenterOfMass = FindCenterOfMass(topology, hubs, config);
            
            // Phase 4: Extract limb chains from hubs
            ExtractLimbs(topology, hubs, config);
            
            // Phase 5: Extract spine chains
            ExtractSpines(topology, hubs, config);
            
            // Phase 6: Classify morphology
            topology.Morphology = ClassifyMorphology(topology);
            
            // Phase 7: Assign gait phases
            topology.AssignGaitPhases();
            
            // Phase 8: Calculate bounding box and character size
            CalculateCharacterMetrics(topology);
            
            // Phase 9: Capture rest poses
            topology.CaptureRestPoses();
            
            Debug.Log($"[GraphTopologyAnalyzer] Analyzed skeleton: {topology.Morphology}, {topology.LegCount} legs, {topology.ArmCount} arms, {topology.Spines.Count} spines");
            
            return topology;
        }
        
        #region Phase 1: Build Bone List
        
        private static void BuildBoneList(Transform root, BodyTopology topology, AnalyzerConfig config)
        {
            var queue = new Queue<(Transform transform, int parentIndex, int depth)>();
            queue.Enqueue((root, -1, 0));
            
            while (queue.Count > 0)
            {
                var (current, parentIndex, depth) = queue.Dequeue();
                
                if (current == null || depth > config.MaxDepth)
                    continue;
                
                // Create bone data
                var boneData = new BoneData
                {
                    Transform = current,
                    Name = current.name,
                    ParentIndex = parentIndex,
                    Depth = depth
                };
                
                int currentIndex = topology.AllBones.Count;
                topology.AllBones.Add(boneData);
                
                // Update parent's child list
                if (parentIndex >= 0 && parentIndex < topology.AllBones.Count)
                {
                    topology.AllBones[parentIndex].ChildIndices.Add(currentIndex);
                }
                
                // Calculate bone length
                if (current.childCount > 0)
                {
                    boneData.Length = Vector3.Distance(current.position, current.GetChild(0).position);
                }
                
                // Classify bone type using naming heuristics
                if (config.UseNamingHeuristics)
                {
                    boneData.Type = ClassifyBoneByName(current.name, config);
                }
                
                // Enqueue children
                for (int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    
                    // Skip very small bones (likely helpers/constraints)
                    if (child.childCount == 0)
                    {
                        float dist = Vector3.Distance(current.position, child.position);
                        if (dist < config.MinBoneLength)
                            continue;
                    }
                    
                    queue.Enqueue((child, currentIndex, depth + 1));
                }
            }
        }
        
        private static BoneType ClassifyBoneByName(string name, AnalyzerConfig config)
        {
            string lowerName = name.ToLowerInvariant();
            
            if (ContainsAny(lowerName, config.SpineKeywords))
            {
                if (lowerName.Contains("hip") || lowerName.Contains("pelvis"))
                    return BoneType.Hips;
                if (lowerName.Contains("chest"))
                    return BoneType.Chest;
                if (lowerName.Contains("neck"))
                    return BoneType.Neck;
                return BoneType.Spine;
            }
            
            if (ContainsAny(lowerName, config.ArmKeywords))
            {
                if (lowerName.Contains("shoulder") || lowerName.Contains("clavicle"))
                    return BoneType.Shoulder;
                if (lowerName.Contains("upper") || lowerName.Contains("arm"))
                    return BoneType.UpperArm;
                if (lowerName.Contains("elbow") || lowerName.Contains("lower") || lowerName.Contains("fore"))
                    return BoneType.LowerArm;
                if (lowerName.Contains("hand") || lowerName.Contains("wrist"))
                    return BoneType.Hand;
                if (lowerName.Contains("finger"))
                    return BoneType.Finger;
                return BoneType.UpperArm;
            }
            
            if (ContainsAny(lowerName, config.LegKeywords))
            {
                if (lowerName.Contains("thigh") || lowerName.Contains("upper"))
                    return BoneType.UpperLeg;
                if (lowerName.Contains("calf") || lowerName.Contains("shin") || lowerName.Contains("lower"))
                    return BoneType.LowerLeg;
                if (lowerName.Contains("foot") || lowerName.Contains("ankle"))
                    return BoneType.Foot;
                if (lowerName.Contains("toe"))
                    return BoneType.Toe;
                return BoneType.UpperLeg;
            }
            
            if (ContainsAny(lowerName, config.TailKeywords))
                return BoneType.Tail;
            
            if (lowerName.Contains("head"))
                return BoneType.Head;
            
            if (lowerName.Contains("root"))
                return BoneType.Root;
            
            return BoneType.Unknown;
        }
        
        private static bool ContainsAny(string text, string[] keywords)
        {
            if (keywords == null) return false;
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword))
                    return true;
            }
            return false;
        }
        
        #endregion
        
        #region Phase 2: Find Hubs
        
        private static List<int> FindHubs(BodyTopology topology)
        {
            var hubs = new List<int>();
            
            for (int i = 0; i < topology.AllBones.Count; i++)
            {
                if (topology.AllBones[i].IsHub)
                {
                    hubs.Add(i);
                }
            }
            
            return hubs;
        }
        
        #endregion
        
        #region Phase 3: Find Center of Mass
        
        private static Transform FindCenterOfMass(BodyTopology topology, List<int> hubs, AnalyzerConfig config)
        {
            // Priority 1: Look for hips/pelvis by name
            foreach (var bone in topology.AllBones)
            {
                string lowerName = bone.Name.ToLowerInvariant();
                if (lowerName.Contains("hip") || lowerName.Contains("pelvis") || 
                    lowerName.Contains("root") || lowerName.Contains("cog"))
                {
                    return bone.Transform;
                }
            }
            
            // Priority 2: First hub that is not the root
            if (hubs.Count > 0)
            {
                foreach (var hubIndex in hubs)
                {
                    if (hubIndex > 0) // Not root
                        return topology.AllBones[hubIndex].Transform;
                }
            }
            
            // Priority 3: Just use root
            return topology.Root;
        }
        
        #endregion
        
        #region Phase 4: Extract Limbs
        
        private static void ExtractLimbs(BodyTopology topology, List<int> hubs, AnalyzerConfig config)
        {
            // For each hub, trace chains that lead to leaves (potential limbs)
            foreach (var hubIndex in hubs)
            {
                var hub = topology.AllBones[hubIndex];
                
                foreach (var childIndex in hub.ChildIndices)
                {
                    var chain = TraceChainToLeaf(topology, childIndex);
                    
                    if (chain.Count >= 2) // Minimum 2 bones for a limb
                    {
                        var limb = CreateLimbFromChain(topology, chain, config);
                        if (limb != null)
                        {
                            topology.Limbs.Add(limb);
                        }
                    }
                }
            }
            
            // If no hubs found (simple skeleton), trace from root
            if (hubs.Count == 0 && topology.AllBones.Count > 0)
            {
                var rootBone = topology.AllBones[0];
                foreach (var childIndex in rootBone.ChildIndices)
                {
                    var chain = TraceChainToLeaf(topology, childIndex);
                    if (chain.Count >= 2)
                    {
                        var limb = CreateLimbFromChain(topology, chain, config);
                        if (limb != null)
                        {
                            topology.Limbs.Add(limb);
                        }
                    }
                }
            }
        }
        
        private static List<int> TraceChainToLeaf(BodyTopology topology, int startIndex)
        {
            var chain = new List<int>();
            int current = startIndex;
            
            while (current >= 0 && current < topology.AllBones.Count)
            {
                var bone = topology.AllBones[current];
                chain.Add(current);
                
                // Stop at leaf
                if (bone.IsLeaf)
                    break;
                
                // Stop at hub (branch point)
                if (bone.IsHub)
                    break;
                
                // Continue to single child
                if (bone.ChildIndices.Count == 1)
                {
                    current = bone.ChildIndices[0];
                }
                else
                {
                    break;
                }
            }
            
            return chain;
        }
        
        private static LimbChain CreateLimbFromChain(BodyTopology topology, List<int> boneIndices, AnalyzerConfig config)
        {
            if (boneIndices.Count == 0) return null;
            
            var transforms = new Transform[boneIndices.Count];
            for (int i = 0; i < boneIndices.Count; i++)
            {
                transforms[i] = topology.AllBones[boneIndices[i]].Transform;
            }
            
            // Determine limb type and side from bone names
            LimbType limbType = LimbType.Unknown;
            BodySide side = BodySide.Center;
            string limbName = transforms[0].name;
            
            foreach (var idx in boneIndices)
            {
                var bone = topology.AllBones[idx];
                string lowerName = bone.Name.ToLowerInvariant();
                
                // Detect type
                if (limbType == LimbType.Unknown)
                {
                    if (ContainsAny(lowerName, config.LegKeywords))
                        limbType = LimbType.Leg;
                    else if (ContainsAny(lowerName, config.ArmKeywords))
                        limbType = LimbType.Arm;
                    else if (ContainsAny(lowerName, config.TailKeywords))
                        limbType = LimbType.Tail;
                }
                
                // Detect side
                if (side == BodySide.Center)
                {
                    if (ContainsAny(lowerName, config.LeftKeywords))
                        side = BodySide.Left;
                    else if (ContainsAny(lowerName, config.RightKeywords))
                        side = BodySide.Right;
                }
            }
            
            // Refine side for quadrupeds (front/back)
            if (limbType == LimbType.Leg && topology.Root != null)
            {
                float3 limbPos = transforms[0].position;
                float3 rootPos = topology.Root.position;
                float3 rootForward = topology.Root.forward;
                
                float dotForward = math.dot(math.normalizesafe(limbPos - rootPos), rootForward);
                
                if (side == BodySide.Left)
                    side = dotForward > 0 ? BodySide.FrontLeft : BodySide.BackLeft;
                else if (side == BodySide.Right)
                    side = dotForward > 0 ? BodySide.FrontRight : BodySide.BackRight;
                else
                    side = dotForward > 0 ? BodySide.Front : BodySide.Back;
            }
            
            // Generate name
            string generatedName = $"{side}_{limbType}";
            if (!string.IsNullOrEmpty(limbName))
                generatedName = limbName;
            
            var limb = new LimbChain
            {
                Name = generatedName,
                Bones = transforms,
                Type = limbType,
                Side = side
            };
            
            limb.CalculateBoneLengths();
            
            return limb;
        }
        
        #endregion
        
        #region Phase 5: Extract Spines
        
        private static void ExtractSpines(BodyTopology topology, List<int> hubs, AnalyzerConfig config)
        {
            // Look for spine bones and group them
            var spineIndices = new List<int>();
            var tailIndices = new List<int>();
            
            for (int i = 0; i < topology.AllBones.Count; i++)
            {
                var bone = topology.AllBones[i];
                
                if (bone.Type == BoneType.Spine || bone.Type == BoneType.Chest || 
                    bone.Type == BoneType.Neck || bone.Type == BoneType.Hips)
                {
                    spineIndices.Add(i);
                }
                else if (bone.Type == BoneType.Tail)
                {
                    tailIndices.Add(i);
                }
            }
            
            // Create main spine if we found spine bones
            if (spineIndices.Count >= 2)
            {
                // Sort by depth to get correct order
                spineIndices.Sort((a, b) => topology.AllBones[a].Depth.CompareTo(topology.AllBones[b].Depth));
                
                var spineTransforms = new Transform[spineIndices.Count];
                for (int i = 0; i < spineIndices.Count; i++)
                {
                    spineTransforms[i] = topology.AllBones[spineIndices[i]].Transform;
                }
                
                var mainSpine = new SpineChain
                {
                    Name = "MainSpine",
                    Bones = spineTransforms,
                    Type = SpineType.MainSpine
                };
                mainSpine.CalculateLength();
                
                topology.Spines.Add(mainSpine);
            }
            
            // Create tail if found
            if (tailIndices.Count >= 2)
            {
                tailIndices.Sort((a, b) => topology.AllBones[a].Depth.CompareTo(topology.AllBones[b].Depth));
                
                var tailTransforms = new Transform[tailIndices.Count];
                for (int i = 0; i < tailIndices.Count; i++)
                {
                    tailTransforms[i] = topology.AllBones[tailIndices[i]].Transform;
                }
                
                var tail = new SpineChain
                {
                    Name = "Tail",
                    Bones = tailTransforms,
                    Type = SpineType.Tail
                };
                tail.CalculateLength();
                
                topology.Spines.Add(tail);
            }
        }
        
        #endregion
        
        #region Phase 6: Classify Morphology
        
        private static MorphologyType ClassifyMorphology(BodyTopology topology)
        {
            int legCount = topology.LegCount;
            int armCount = topology.ArmCount;
            
            // Check for serpentine (no legs, has spine)
            if (legCount == 0 && topology.Spines.Count > 0)
            {
                var mainSpine = topology.GetMainSpine();
                if (mainSpine != null && mainSpine.BoneCount >= 5)
                    return MorphologyType.Serpentine;
            }
            
            // Classify by leg count
            return legCount switch
            {
                0 => MorphologyType.Unknown,
                1 => MorphologyType.Unknown,
                2 => MorphologyType.Biped,
                3 => MorphologyType.Unknown,
                4 => MorphologyType.Quadruped,
                5 => MorphologyType.Unknown,
                6 => MorphologyType.Hexapod,
                7 => MorphologyType.Unknown,
                8 => MorphologyType.Octopod,
                _ => legCount > 8 ? MorphologyType.Centipede : MorphologyType.Unknown
            };
        }
        
        #endregion
        
        #region Phase 7: Character Metrics
        
        private static void CalculateCharacterMetrics(BodyTopology topology)
        {
            if (topology.AllBones.Count == 0)
            {
                topology.Height = 1f;
                topology.CharacteristicSize = 1f;
                return;
            }
            
            float3 min = new float3(float.MaxValue);
            float3 max = new float3(float.MinValue);
            
            foreach (var bone in topology.AllBones)
            {
                if (bone.Transform != null)
                {
                    float3 pos = bone.Transform.position;
                    min = math.min(min, pos);
                    max = math.max(max, pos);
                }
            }
            
            float3 size = max - min;
            topology.Height = size.y;
            topology.CharacteristicSize = math.max(math.max(size.x, size.y), size.z);
            
            // Clamp to reasonable values
            if (topology.Height < 0.01f) topology.Height = 1f;
            if (topology.CharacteristicSize < 0.01f) topology.CharacteristicSize = 1f;
        }
        
        #endregion
    }
}
