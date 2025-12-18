using Eraflo.UnityImportPackage.Timers;
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

        [Header("Network Events")]
        [Tooltip("If enabled, the NetworkEventManager singleton will be automatically instantiated on game start.")]
        [SerializeField] private bool _enableNetworking = false;

        [Tooltip("If enabled, log debug messages for network events.")]
        [SerializeField] private bool _networkDebugMode = false;

        [Header("Timer System")]
        [Tooltip("Thread mode for timer operations. SingleThread = faster, ThreadSafe = safe from any thread.")]
        [SerializeField] private TimerThreadMode _timerThreadMode = TimerThreadMode.SingleThread;

        [Tooltip("If enabled, log debug messages for timer events.")]
        [SerializeField] private bool _enableTimerDebugLogs = false;

        [Header("Timer Pool")]
        [Tooltip("Enable timer pooling to reduce garbage collection.")]
        [SerializeField] private bool _enableTimerPooling = true;

        [Tooltip("Default pool capacity per timer type.")]
        [SerializeField] [Range(5, 50)] private int _timerPoolDefaultCapacity = 10;

        [Tooltip("Maximum pool capacity per timer type.")]
        [SerializeField] [Range(10, 200)] private int _timerPoolMaxCapacity = 50;

        [Tooltip("Prewarm pools on startup with this many timers per type.")]
        [SerializeField] [Range(0, 20)] private int _timerPoolPrewarmCount = 0;

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
        /// Thread mode for timer operations.
        /// </summary>
        public TimerThreadMode TimerThreadMode => _timerThreadMode;

        /// <summary>
        /// Whether to log debug messages for timer events.
        /// </summary>
        public bool EnableTimerDebugLogs => _enableTimerDebugLogs;

        /// <summary>
        /// Whether timer pooling is enabled.
        /// </summary>
        public bool EnableTimerPooling => _enableTimerPooling;

        /// <summary>
        /// Default pool capacity per timer type.
        /// </summary>
        public int TimerPoolDefaultCapacity => _timerPoolDefaultCapacity;

        /// <summary>
        /// Maximum pool capacity per timer type.
        /// </summary>
        public int TimerPoolMaxCapacity => _timerPoolMaxCapacity;

        /// <summary>
        /// Number of timers to prewarm per type on startup.
        /// </summary>
        public int TimerPoolPrewarmCount => _timerPoolPrewarmCount;

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
