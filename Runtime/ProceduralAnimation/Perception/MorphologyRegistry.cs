using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation.Perception
{
    /// <summary>
    /// Registry for custom morphology definitions.
    /// Allows users to define custom skeleton patterns that the analyzer can't detect automatically.
    /// </summary>
    public static class MorphologyRegistry
    {
        private static readonly Dictionary<string, MorphologyDefinition> _definitions = 
            new Dictionary<string, MorphologyDefinition>(StringComparer.OrdinalIgnoreCase);
        
        private static bool _initialized;
        
        /// <summary>
        /// Initializes the registry with default morphologies.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            // Register built-in morphologies
            RegisterDefaults();
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    Clear();
                }
            };
        }
#endif
        
        /// <summary>
        /// Registers the default morphology definitions.
        /// </summary>
        private static void RegisterDefaults()
        {
            // Humanoid biped
            Register(new MorphologyDefinition
            {
                Name = "Humanoid",
                Type = MorphologyType.Biped,
                ExpectedLegCount = 2,
                ExpectedArmCount = 2,
                HasTail = false,
                SpineSegments = 4, // Hips, Spine, Chest, Neck
                LimbNamingPatterns = new[]
                {
                    new LimbPattern { Type = LimbType.Leg, Side = BodySide.Left, RequiredKeywords = new[] { "left", "leg" } },
                    new LimbPattern { Type = LimbType.Leg, Side = BodySide.Right, RequiredKeywords = new[] { "right", "leg" } },
                    new LimbPattern { Type = LimbType.Arm, Side = BodySide.Left, RequiredKeywords = new[] { "left", "arm" } },
                    new LimbPattern { Type = LimbType.Arm, Side = BodySide.Right, RequiredKeywords = new[] { "right", "arm" } }
                },
                GaitPattern = GaitPatternType.Alternating
            });
            
            // Dog/Wolf quadruped
            Register(new MorphologyDefinition
            {
                Name = "Canine",
                Type = MorphologyType.Quadruped,
                ExpectedLegCount = 4,
                ExpectedArmCount = 0,
                HasTail = true,
                SpineSegments = 5,
                LimbNamingPatterns = new[]
                {
                    new LimbPattern { Type = LimbType.Leg, Side = BodySide.FrontLeft, RequiredKeywords = new[] { "front", "left" } },
                    new LimbPattern { Type = LimbType.Leg, Side = BodySide.FrontRight, RequiredKeywords = new[] { "front", "right" } },
                    new LimbPattern { Type = LimbType.Leg, Side = BodySide.BackLeft, RequiredKeywords = new[] { "back", "left" } },
                    new LimbPattern { Type = LimbType.Leg, Side = BodySide.BackRight, RequiredKeywords = new[] { "back", "right" } }
                },
                GaitPattern = GaitPatternType.DiagonalPairs
            });
            
            // Spider
            Register(new MorphologyDefinition
            {
                Name = "Spider",
                Type = MorphologyType.Octopod,
                ExpectedLegCount = 8,
                ExpectedArmCount = 0,
                HasTail = false,
                SpineSegments = 2, // Cephalothorax, Abdomen
                GaitPattern = GaitPatternType.Wave
            });
            
            // Insect hexapod
            Register(new MorphologyDefinition
            {
                Name = "Insect",
                Type = MorphologyType.Hexapod,
                ExpectedLegCount = 6,
                ExpectedArmCount = 0,
                HasTail = false,
                SpineSegments = 3, // Head, Thorax, Abdomen
                GaitPattern = GaitPatternType.Tripod
            });
            
            // Snake/Worm
            Register(new MorphologyDefinition
            {
                Name = "Serpentine",
                Type = MorphologyType.Serpentine,
                ExpectedLegCount = 0,
                ExpectedArmCount = 0,
                HasTail = false, // The whole body is essentially the "tail"
                SpineSegments = 20, // Many vertebrae
                GaitPattern = GaitPatternType.Sinusoidal
            });
        }
        
        /// <summary>
        /// Registers a custom morphology definition.
        /// </summary>
        public static void Register(MorphologyDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.Name))
            {
                Debug.LogWarning("[MorphologyRegistry] Cannot register definition with empty name.");
                return;
            }
            
            _definitions[definition.Name] = definition;
        }
        
        /// <summary>
        /// Unregisters a morphology definition.
        /// </summary>
        public static bool Unregister(string name)
        {
            return _definitions.Remove(name);
        }
        
        /// <summary>
        /// Gets a morphology definition by name.
        /// </summary>
        public static MorphologyDefinition Get(string name)
        {
            _definitions.TryGetValue(name, out var definition);
            return definition;
        }
        
        /// <summary>
        /// Gets all registered morphology definitions.
        /// </summary>
        public static IEnumerable<MorphologyDefinition> GetAll()
        {
            return _definitions.Values;
        }
        
        /// <summary>
        /// Finds the best matching morphology for a topology.
        /// </summary>
        public static MorphologyDefinition FindBestMatch(BodyTopology topology)
        {
            MorphologyDefinition bestMatch = null;
            int bestScore = -1;
            
            foreach (var definition in _definitions.Values)
            {
                int score = CalculateMatchScore(topology, definition);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = definition;
                }
            }
            
            return bestMatch;
        }
        
        /// <summary>
        /// Calculates how well a topology matches a definition.
        /// </summary>
        private static int CalculateMatchScore(BodyTopology topology, MorphologyDefinition definition)
        {
            int score = 0;
            
            // Morphology type match
            if (topology.Morphology == definition.Type)
                score += 10;
            
            // Leg count match
            if (topology.LegCount == definition.ExpectedLegCount)
                score += 5;
            else
                score -= Math.Abs(topology.LegCount - definition.ExpectedLegCount);
            
            // Arm count match
            if (topology.ArmCount == definition.ExpectedArmCount)
                score += 3;
            else
                score -= Math.Abs(topology.ArmCount - definition.ExpectedArmCount);
            
            // Tail presence
            bool hasTail = topology.GetTail() != null;
            if (hasTail == definition.HasTail)
                score += 2;
            
            return score;
        }
        
        /// <summary>
        /// Clears all registered definitions.
        /// </summary>
        public static void Clear()
        {
            _definitions.Clear();
            _initialized = false;
        }
        
        /// <summary>
        /// Applies a morphology definition to a topology, 
        /// overriding detected values with the definition's expected values.
        /// </summary>
        public static void ApplyDefinition(BodyTopology topology, MorphologyDefinition definition)
        {
            if (definition == null) return;
            
            topology.Morphology = definition.Type;
            
            // Re-assign limb types based on patterns
            if (definition.LimbNamingPatterns != null)
            {
                foreach (var limb in topology.Limbs)
                {
                    foreach (var pattern in definition.LimbNamingPatterns)
                    {
                        if (MatchesPattern(limb, pattern))
                        {
                            limb.Type = pattern.Type;
                            limb.Side = pattern.Side;
                            break;
                        }
                    }
                }
            }
            
            // Re-assign gait phases based on pattern
            AssignGaitFromPattern(topology, definition.GaitPattern);
        }
        
        private static bool MatchesPattern(LimbChain limb, LimbPattern pattern)
        {
            if (limb.Bones == null || limb.Bones.Length == 0)
                return false;
            
            string combinedName = "";
            foreach (var bone in limb.Bones)
            {
                combinedName += bone.name.ToLowerInvariant() + " ";
            }
            
            foreach (var keyword in pattern.RequiredKeywords)
            {
                if (!combinedName.Contains(keyword.ToLowerInvariant()))
                    return false;
            }
            
            return true;
        }
        
        private static void AssignGaitFromPattern(BodyTopology topology, GaitPatternType pattern)
        {
            var legs = topology.GetLegs();
            if (legs.Length == 0) return;
            
            switch (pattern)
            {
                case GaitPatternType.Alternating:
                    // 0, 0.5, 0, 0.5...
                    for (int i = 0; i < legs.Length; i++)
                        legs[i].GaitPhase = (i % 2) * 0.5f;
                    break;
                    
                case GaitPatternType.DiagonalPairs:
                    // FL+BR = 0, FR+BL = 0.5
                    foreach (var leg in legs)
                    {
                        leg.GaitPhase = (leg.Side == BodySide.FrontLeft || leg.Side == BodySide.BackRight) ? 0f : 0.5f;
                    }
                    break;
                    
                case GaitPatternType.Tripod:
                    // Alternating triangles
                    for (int i = 0; i < legs.Length; i++)
                        legs[i].GaitPhase = (i % 2) * 0.5f;
                    break;
                    
                case GaitPatternType.Wave:
                    // Each leg has unique phase
                    for (int i = 0; i < legs.Length; i++)
                        legs[i].GaitPhase = (float)i / legs.Length;
                    break;
                    
                case GaitPatternType.Sinusoidal:
                    // No legs, handled by spine
                    break;
            }
        }
    }
    
    /// <summary>
    /// Definition of a morphology type.
    /// </summary>
    [Serializable]
    public class MorphologyDefinition
    {
        /// <summary>
        /// Unique name for this morphology.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// The base morphology type.
        /// </summary>
        public MorphologyType Type;
        
        /// <summary>
        /// Expected number of legs.
        /// </summary>
        public int ExpectedLegCount;
        
        /// <summary>
        /// Expected number of arms.
        /// </summary>
        public int ExpectedArmCount;
        
        /// <summary>
        /// Whether this morphology typically has a tail.
        /// </summary>
        public bool HasTail;
        
        /// <summary>
        /// Expected number of spine segments.
        /// </summary>
        public int SpineSegments;
        
        /// <summary>
        /// Patterns for identifying limbs by name.
        /// </summary>
        public LimbPattern[] LimbNamingPatterns;
        
        /// <summary>
        /// The gait pattern to use.
        /// </summary>
        public GaitPatternType GaitPattern;
    }
    
    /// <summary>
    /// Pattern for matching limbs by name.
    /// </summary>
    [Serializable]
    public struct LimbPattern
    {
        public LimbType Type;
        public BodySide Side;
        public string[] RequiredKeywords;
    }
    
    /// <summary>
    /// Types of gait patterns.
    /// </summary>
    public enum GaitPatternType
    {
        /// <summary>Simple left-right alternation (biped).</summary>
        Alternating,
        /// <summary>Diagonal pairs move together (quadruped walk).</summary>
        DiagonalPairs,
        /// <summary>Alternating triangles (hexapod).</summary>
        Tripod,
        /// <summary>Each leg has unique phase (many legs).</summary>
        Wave,
        /// <summary>Sinusoidal body motion (serpentine).</summary>
        Sinusoidal
    }
}
