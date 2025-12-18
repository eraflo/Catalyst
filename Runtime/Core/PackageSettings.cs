using UnityEngine;

namespace Eraflo.UnityImportPackage
{
    /// <summary>
    /// Global settings for the UnityImportPackage.
    /// Located in Assets/Resources/UnityImportPackageSettings.
    /// </summary>
    public class PackageSettings : ScriptableObject
    {
        private const string ResourcePath = "UnityImportPackageSettings";
        private static PackageSettings _instance;

        [Header("Global Settings")]
        [Tooltip("Thread mode for all package systems. SingleThread = faster, ThreadSafe = safe from any thread.")]
        [SerializeField] private PackageThreadMode _threadMode = PackageThreadMode.SingleThread;

        [Header("Network Events")]
        [Tooltip("If enabled, the NetworkEventManager singleton will be automatically instantiated on game start.")]
        [SerializeField] private bool _enableNetworking = false;

        [Tooltip("If enabled, log debug messages for network events.")]
        [SerializeField] private bool _networkDebugMode = false;

        [Header("Timer System")]
        [Tooltip("Use optimized array-based backend for better performance with many timers.")]
        [SerializeField] private bool _useBurstTimers = false;

        [Tooltip("If enabled, log debug messages for timer events.")]
        [SerializeField] private bool _enableTimerDebugLogs = false;

        [Tooltip("Show runtime debug overlay with active timers.")]
        [SerializeField] private bool _enableDebugOverlay = false;

        /// <summary>
        /// Gets the singleton instance of the package settings.
        /// </summary>
        public static PackageSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<PackageSettings>(ResourcePath);
                    
                    if (_instance == null)
                    {
                        Debug.LogWarning($"[PackageSettings] No settings found at Resources/{ResourcePath}. Using defaults.");
                        _instance = CreateInstance<PackageSettings>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Whether networking features are enabled.
        /// </summary>
        public bool EnableNetworking => _enableNetworking;

        /// <summary>
        /// Whether to log debug messages for network events.
        /// </summary>
        public bool NetworkDebugMode => _networkDebugMode;

        /// <summary>
        /// Global thread mode for all package systems.
        /// </summary>
        public PackageThreadMode ThreadMode => _threadMode;

        /// <summary>
        /// Whether to use optimized backend for timer updates.
        /// </summary>
        public bool UseBurstTimers => _useBurstTimers;

        /// <summary>
        /// Whether to log debug messages for timer events.
        /// </summary>
        public bool EnableTimerDebugLogs => _enableTimerDebugLogs;

        /// <summary>
        /// Whether to show the runtime debug overlay for timers.
        /// </summary>
        public bool EnableDebugOverlay => _enableDebugOverlay;

        /// <summary>
        /// Reloads the settings from Resources.
        /// </summary>
        public static void Reload()
        {
            _instance = null;
            _ = Instance;
        }
    }
}
