using UnityEngine;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Settings for the ProceduralAnimation module.
    /// Configured via PackageSettings.
    /// </summary>
    [System.Serializable]
    public class ProceduralAnimationSettings
    {
        [Header("Performance")]
        
        [Tooltip("Use Burst-compiled jobs for animation calculations.")]
        [SerializeField] private bool _useBurstJobs = true;
        
        [Tooltip("Maximum number of agents to process per frame. 0 = unlimited.")]
        [SerializeField] private int _maxAgentsPerFrame = 0;
        
        [Tooltip("Batch size for parallel jobs.")]
        [SerializeField] private int _jobBatchSize = 64;
        
        [Header("Quality")]
        
        [Tooltip("Default spring preset for new animations.")]
        [SerializeField] private SpringPreset _defaultSpringPreset = SpringPreset.Smooth;
        
        [Tooltip("Enable debug visualizations in Scene view.")]
        [SerializeField] private bool _enableDebugVisualization = false;
        
        [Header("Auto-Rigger")]
        
        [Tooltip("Minimum bone length to consider valid (in meters).")]
        [SerializeField] private float _minBoneLength = 0.01f;
        
        [Tooltip("Maximum depth to search in skeleton hierarchy.")]
        [SerializeField] private int _maxSkeletonDepth = 50;
        
        /// <summary>
        /// Whether to use Burst-compiled jobs.
        /// </summary>
        public bool UseBurstJobs => _useBurstJobs;
        
        /// <summary>
        /// Maximum agents to process per frame (0 = unlimited).
        /// </summary>
        public int MaxAgentsPerFrame => _maxAgentsPerFrame;
        
        /// <summary>
        /// Batch size for parallel job scheduling.
        /// </summary>
        public int JobBatchSize => _jobBatchSize;
        
        /// <summary>
        /// Default spring preset for new animations.
        /// </summary>
        public SpringPreset DefaultSpringPreset => _defaultSpringPreset;
        
        /// <summary>
        /// Whether to show debug visualizations.
        /// </summary>
        public bool EnableDebugVisualization => _enableDebugVisualization;
        
        /// <summary>
        /// Minimum bone length for auto-rigger.
        /// </summary>
        public float MinBoneLength => _minBoneLength;
        
        /// <summary>
        /// Maximum skeleton search depth.
        /// </summary>
        public int MaxSkeletonDepth => _maxSkeletonDepth;
        
        /// <summary>
        /// Creates default settings.
        /// </summary>
        public static ProceduralAnimationSettings Default => new ProceduralAnimationSettings();
    }
}
