using UnityEngine;
using Eraflo.Catalyst.ProceduralAnimation.Perception;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Main component for procedural animation.
    /// Drop this on any rigged character for automatic skeleton analysis and animation.
    /// </summary>
    [AddComponentMenu("Catalyst/Procedural Animation/Procedural Animator")]
    [DisallowMultipleComponent]
    public class ProceduralAnimator : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Skeleton")]
        [Tooltip("Root bone of the skeleton. If null, will use this transform.")]
        [SerializeField] private Transform _skeletonRoot;
        
        [Tooltip("Override morphology instead of auto-detecting.")]
        [SerializeField] private bool _overrideMorphology;
        
        [Tooltip("The morphology to use if override is enabled.")]
        [SerializeField] private MorphologyType _morphologyOverride = MorphologyType.Biped;
        
        [Tooltip("Custom morphology definition name from registry.")]
        [SerializeField] private string _customMorphologyName;
        
        [Header("Analysis")]
        [Tooltip("Minimum bone length to consider valid (meters).")]
        [SerializeField] private float _minBoneLength = 0.001f;
        
        [Tooltip("Maximum hierarchy depth to search.")]
        [SerializeField] private int _maxDepth = 50;
        
        [Tooltip("Use naming heuristics to identify bone types.")]
        [SerializeField] private bool _useNamingHeuristics = true;
        
        [Header("Mass")]
        [Tooltip("Estimate masses from mesh volumes.")]
        [SerializeField] private bool _estimateMass = true;
        
        [Tooltip("Target total mass in kg (0 = use estimated).")]
        [SerializeField] private float _targetMass = 0f;
        
        [Tooltip("Density for mass estimation (kg/m³).")]
        [SerializeField] private float _density = 1000f;
        
        [Header("Debug")]
        [Tooltip("Show debug gizmos in Scene view.")]
        [SerializeField] private bool _showDebugGizmos = true;
        
        [Tooltip("Show bone hierarchy.")]
        [SerializeField] private bool _showBones = true;
        
        [Tooltip("Show limb chains.")]
        [SerializeField] private bool _showLimbs = true;
        
        [Tooltip("Show spine chains.")]
        [SerializeField] private bool _showSpines = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// The analyzed body topology.
        /// </summary>
        public BodyTopology Topology { get; private set; }
        
        /// <summary>
        /// Whether the skeleton has been analyzed.
        /// </summary>
        public bool IsAnalyzed => Topology != null && Topology.IsValid;
        
        /// <summary>
        /// The detected or overridden morphology type.
        /// </summary>
        public MorphologyType Morphology => Topology?.Morphology ?? MorphologyType.Unknown;
        
        /// <summary>
        /// Number of legs in the topology.
        /// </summary>
        public int LegCount => Topology?.LegCount ?? 0;
        
        /// <summary>
        /// Number of arms in the topology.
        /// </summary>
        public int ArmCount => Topology?.ArmCount ?? 0;
        
        /// <summary>
        /// The skeleton root transform.
        /// </summary>
        public Transform SkeletonRoot => _skeletonRoot != null ? _skeletonRoot : transform;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_skeletonRoot == null)
                _skeletonRoot = transform;
        }
        
        private void Start()
        {
            AnalyzeSkeleton();
        }
        
        private void OnValidate()
        {
            if (_skeletonRoot == null)
                _skeletonRoot = transform;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Analyzes the skeleton and builds the body topology.
        /// Call this to re-analyze if the skeleton changes.
        /// </summary>
        public void AnalyzeSkeleton()
        {
            var config = new GraphTopologyAnalyzer.AnalyzerConfig
            {
                MinBoneLength = _minBoneLength,
                MaxDepth = _maxDepth,
                UseNamingHeuristics = _useNamingHeuristics,
                LeftKeywords = new[] { "left", "l_", "_l", ".l", "gauche" },
                RightKeywords = new[] { "right", "r_", "_r", ".r", "droit" },
                ArmKeywords = new[] { "arm", "hand", "shoulder", "clavicle", "elbow", "wrist", "bras", "main" },
                LegKeywords = new[] { "leg", "foot", "thigh", "calf", "ankle", "toe", "hip", "jambe", "pied" },
                SpineKeywords = new[] { "spine", "chest", "torso", "abdomen", "pelvis", "hips", "colonne" },
                TailKeywords = new[] { "tail", "queue" }
            };
            
            Topology = GraphTopologyAnalyzer.Analyze(SkeletonRoot, config);
            
            // Apply morphology override if enabled
            if (_overrideMorphology)
            {
                Topology.Morphology = _morphologyOverride;
                Topology.AssignGaitPhases();
            }
            else if (!string.IsNullOrEmpty(_customMorphologyName))
            {
                var definition = MorphologyRegistry.Get(_customMorphologyName);
                if (definition != null)
                {
                    MorphologyRegistry.ApplyDefinition(Topology, definition);
                }
            }
            
            // Estimate masses if enabled
            if (_estimateMass)
            {
                var massConfig = new VolumetricMassEstimator.EstimatorConfig
                {
                    Density = _density,
                    MinMass = 0.01f,
                    MaxMass = 100f,
                    UseMeshBounds = true,
                    DefaultBoneRadiusFraction = 0.15f
                };
                
                VolumetricMassEstimator.EstimateMasses(Topology, massConfig);
                
                // Normalize to target mass if specified
                if (_targetMass > 0f)
                {
                    VolumetricMassEstimator.NormalizeMasses(Topology, _targetMass);
                }
            }
            
            Debug.Log($"[ProceduralAnimator] Analyzed: {Topology.Morphology}, " +
                      $"{Topology.LegCount} legs, {Topology.ArmCount} arms, " +
                      $"{Topology.AllBones.Count} bones, {Topology.TotalMass:F1} kg");
        }
        
        /// <summary>
        /// Gets a limb by name.
        /// </summary>
        public LimbChain GetLimb(string name)
        {
            return Topology?.GetLimb(name);
        }
        
        /// <summary>
        /// Gets all legs.
        /// </summary>
        public LimbChain[] GetLegs()
        {
            return Topology?.GetLegs() ?? new LimbChain[0];
        }
        
        /// <summary>
        /// Gets all arms.
        /// </summary>
        public LimbChain[] GetArms()
        {
            return Topology?.GetArms() ?? new LimbChain[0];
        }
        
        /// <summary>
        /// Gets the main spine.
        /// </summary>
        public SpineChain GetMainSpine()
        {
            return Topology?.GetMainSpine();
        }
        
        /// <summary>
        /// Gets the tail if present.
        /// </summary>
        public SpineChain GetTail()
        {
            return Topology?.GetTail();
        }
        
        /// <summary>
        /// Sets up all procedural animation subsystems automatically.
        /// This is the recommended method for "zero-setup" procedural animation.
        /// </summary>
        /// <param name="enableLocomotion">Enable procedural locomotion for legs.</param>
        /// <param name="enableIK">Enable IK for arms.</param>
        /// <param name="enableSpines">Enable Verlet simulation for tails/secondary spines.</param>
        public void SetupAll(bool enableLocomotion = true, bool enableIK = true, bool enableSpines = true)
        {
            if (!IsAnalyzed)
            {
                AnalyzeSkeleton();
            }
            
            if (enableLocomotion && LegCount > 0)
            {
                SetupLocomotion();
            }
            
            if (enableIK && ArmCount > 0)
            {
                SetupArmIK();
            }
            
            if (enableSpines)
            {
                SetupSecondarySpines();
            }
            
            Debug.Log($"[ProceduralAnimator] Auto-setup complete: " +
                $"Locomotion={enableLocomotion && LegCount > 0}, " +
                $"IK={enableIK && ArmCount > 0}, " +
                $"Spines={enableSpines}");
        }
        
        /// <summary>
        /// Sets up procedural locomotion for all detected legs.
        /// </summary>
        /// <returns>The configured ProceduralLocomotion component.</returns>
        public Components.Locomotion.ProceduralLocomotion SetupLocomotion()
        {
            if (!IsAnalyzed || LegCount == 0)
            {
                Debug.LogWarning("[ProceduralAnimator] Cannot setup locomotion: no legs detected.");
                return null;
            }
            
            var locomotion = GetComponent<Components.Locomotion.ProceduralLocomotion>();
            if (locomotion == null)
            {
                locomotion = gameObject.AddComponent<Components.Locomotion.ProceduralLocomotion>();
            }
            
            // Configure from topology
            locomotion.SetupFromTopology(Topology);
            
            return locomotion;
        }
        
        /// <summary>
        /// Sets up IK for all detected arms.
        /// </summary>
        /// <returns>Array of configured LimbIK components.</returns>
        public Components.IK.LimbIK[] SetupArmIK()
        {
            if (!IsAnalyzed || ArmCount == 0)
            {
                Debug.LogWarning("[ProceduralAnimator] Cannot setup arm IK: no arms detected.");
                return new Components.IK.LimbIK[0];
            }
            
            var arms = GetArms();
            var ikComponents = new Components.IK.LimbIK[arms.Length];
            
            for (int i = 0; i < arms.Length; i++)
            {
                var ik = gameObject.AddComponent<Components.IK.LimbIK>();
                ik.SetupFromLimb(arms[i]);
                ikComponents[i] = ik;
            }
            
            return ikComponents;
        }
        
        /// <summary>
        /// Sets up Verlet simulation for secondary spines (tails, tentacles).
        /// Does not affect the main spine.
        /// </summary>
        /// <returns>Array of configured VerletSpine components.</returns>
        public Components.Locomotion.VerletSpine[] SetupSecondarySpines()
        {
            if (!IsAnalyzed || Topology.Spines.Count <= 1)
            {
                return new Components.Locomotion.VerletSpine[0];
            }
            
            var spines = new System.Collections.Generic.List<Components.Locomotion.VerletSpine>();
            
            foreach (var spine in Topology.Spines)
            {
                // Skip main spine, only setup tails and secondary chains
                if (spine.Type == SpineType.MainSpine) continue;
                if (spine.Root == null) continue;
                
                var verlet = spine.Root.GetComponent<Components.Locomotion.VerletSpine>();
                if (verlet == null)
                {
                    verlet = spine.Root.gameObject.AddComponent<Components.Locomotion.VerletSpine>();
                }
                
                verlet.SetupFromChain(spine);
                
                // Configure based on spine type
                if (spine.Type == SpineType.Tail)
                {
                    verlet.EnableNoise = true;
                    verlet.NoiseAmplitude = 0.05f;
                }
                
                spines.Add(verlet);
            }
            
            return spines.ToArray();
        }
        
        /// <summary>
        /// Sets up active ragdoll for physics-based animation.
        /// </summary>
        /// <param name="animatedRoot">The animated skeleton to copy poses from.</param>
        /// <returns>The configured ActiveRagdoll component, or null if setup failed.</returns>
        public Components.Physics.ActiveRagdoll SetupRagdoll(Transform animatedRoot = null)
        {
            if (!IsAnalyzed)
            {
                Debug.LogWarning("[ProceduralAnimator] Cannot setup ragdoll: skeleton not analyzed.");
                return null;
            }
            
            var ragdoll = GetComponent<Components.Physics.ActiveRagdoll>();
            if (ragdoll == null)
            {
                ragdoll = gameObject.AddComponent<Components.Physics.ActiveRagdoll>();
            }
            
            // Setup from topology
            ragdoll.SetupFromTopology(Topology, animatedRoot ?? transform);
            
            return ragdoll;
        }
        
        /// <summary>
        /// Gets or creates a component on this GameObject.
        /// </summary>
        private T EnsureComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
        
        #endregion
        
        #region Debug Visualization
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_showDebugGizmos || Topology == null) return;
            
            if (_showBones)
                DrawBoneGizmos();
            
            if (_showLimbs)
                DrawLimbGizmos();
            
            if (_showSpines)
                DrawSpineGizmos();
            
            // Draw center of mass
            if (Topology.CenterOfMass != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(Topology.CenterOfMass.position, 0.05f);
            }
        }
        
        private void DrawBoneGizmos()
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            foreach (var bone in Topology.AllBones)
            {
                if (bone.Transform == null) continue;
                
                // Draw bone
                float size = bone.IsHub ? 0.03f : 0.015f;
                
                if (bone.IsHub)
                    Gizmos.color = Color.red;
                else if (bone.IsLeaf)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.gray;
                
                Gizmos.DrawWireSphere(bone.Transform.position, size);
                
                // Draw connection to parent
                if (bone.ParentIndex >= 0 && bone.ParentIndex < Topology.AllBones.Count)
                {
                    var parent = Topology.AllBones[bone.ParentIndex];
                    if (parent.Transform != null)
                    {
                        Gizmos.color = Color.gray;
                        Gizmos.DrawLine(bone.Transform.position, parent.Transform.position);
                    }
                }
            }
        }
        
        private void DrawLimbGizmos()
        {
            foreach (var limb in Topology.Limbs)
            {
                if (limb.Bones == null || limb.Bones.Length == 0) continue;
                
                // Color based on limb type
                Gizmos.color = limb.Type switch
                {
                    LimbType.Leg => Color.blue,
                    LimbType.Arm => Color.cyan,
                    LimbType.Tail => Color.magenta,
                    _ => Color.white
                };
                
                // Draw chain
                for (int i = 0; i < limb.Bones.Length - 1; i++)
                {
                    if (limb.Bones[i] != null && limb.Bones[i + 1] != null)
                    {
                        Gizmos.DrawLine(limb.Bones[i].position, limb.Bones[i + 1].position);
                    }
                }
                
                // Draw effector
                if (limb.Effector != null)
                {
                    Gizmos.DrawWireSphere(limb.Effector.position, 0.02f);
                }
            }
        }
        
        private void DrawSpineGizmos()
        {
            foreach (var spine in Topology.Spines)
            {
                if (spine.Bones == null || spine.Bones.Length == 0) continue;
                
                Gizmos.color = spine.Type == SpineType.Tail ? Color.magenta : Color.yellow;
                
                for (int i = 0; i < spine.Bones.Length - 1; i++)
                {
                    if (spine.Bones[i] != null && spine.Bones[i + 1] != null)
                    {
                        Gizmos.DrawLine(spine.Bones[i].position, spine.Bones[i + 1].position);
                        Gizmos.DrawWireSphere(spine.Bones[i].position, 0.015f);
                    }
                }
                
                if (spine.Tip != null)
                {
                    Gizmos.DrawWireSphere(spine.Tip.position, 0.015f);
                }
            }
        }
#endif
        
        #endregion
    }
}
