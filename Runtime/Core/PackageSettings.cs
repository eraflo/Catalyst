using System.Collections.Generic;
using Eraflo.Catalyst.Events;
using Eraflo.Catalyst.Networking;
using UnityEngine;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Handler registration mode.
    /// </summary>
    public enum NetworkHandlerMode
    {
        /// <summary>Auto-register all handlers found via reflection.</summary>
        Auto,
        /// <summary>Only register handlers selected in the list.</summary>
        Manual
    }

    /// <summary>
    /// Supported asset loading methods.
    /// </summary>
    public enum AssetProviderType
    {
        Resources,
        Addressables
    }

    /// <summary>
    /// Input provider types.
    /// </summary>
    public enum InputProviderType
    {
        Legacy,
        InputSystem
    }

    /// <summary>
    /// Global settings for the package.
    /// </summary>
    public class PackageSettings : ScriptableObject
    {
        private const string ResourcePath = "CatalystSettings";
        private static PackageSettings _instance;

        // Global
        [SerializeField] private PackageThreadMode _threadMode = PackageThreadMode.SingleThread;

        // Networking
        [SerializeField] private string _networkBackendId = "";
        [SerializeField] private bool _networkDebugMode = false;
        [SerializeField] private NetworkHandlerMode _handlerMode = NetworkHandlerMode.Auto;
        [SerializeField] private List<string> _enabledHandlers = new List<string>();
        [SerializeField] private AuthorityMode _defaultAuthorityMode = AuthorityMode.ServerAuthoritative;
        
        // Network Simulation (Editor/Development only)
        [SerializeField] private int _simulateLatencyMs = 0;
        [SerializeField] private float _simulatePacketLossPercent = 0f;
        [SerializeField] private int _simulateJitterMs = 0;
        
        // Culling
        [SerializeField] private float _cullingCellSize = 50f;
        [SerializeField] private int _cullingClientsPerFrame = 4;
        [SerializeField] private float _cullingHysteresis = 5f;

        // Timers
        [SerializeField] private bool _useBurstTimers = false;
        [SerializeField] private bool _enableTimerDebugLogs = false;
        [SerializeField] private bool _enableDebugOverlay = false;

        // Assets
        [SerializeField] private AssetProviderType _assetProviderType = AssetProviderType.Resources;

        // Scene Flow
        [SerializeField] private SceneTransitionChannel _onTransitionStarted;
        [SerializeField] private SceneTransitionChannel _onTransitionCompleted;
        
        // Settings
        [SerializeField] private string _settingsFilename = "settings.json";

        // Input
        [SerializeField] private InputProviderType _inputProvider = InputProviderType.Legacy;
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset _inputActionAsset;
        [SerializeField] private bool _enableInputDebugger = false;

        public static PackageSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<PackageSettings>(ResourcePath);
                    if (_instance == null)
                    {
                        Debug.LogWarning($"[PackageSettings] Not found at Resources/{ResourcePath}");
                        _instance = CreateInstance<PackageSettings>();
                    }
                }
                return _instance;
            }
        }

        public string NetworkBackendId => _networkBackendId;
        public bool EnableNetworking => !string.IsNullOrEmpty(_networkBackendId);
        public bool NetworkDebugMode => _networkDebugMode;
        public NetworkHandlerMode HandlerMode => _handlerMode;
        public IReadOnlyList<string> EnabledHandlers => _enabledHandlers;
        public AuthorityMode DefaultAuthorityMode => _defaultAuthorityMode;
        
        // Network Simulation
        public int SimulateLatencyMs => _simulateLatencyMs;
        public float SimulatePacketLossPercent => _simulatePacketLossPercent;
        public int SimulateJitterMs => _simulateJitterMs;
        
        // Culling
        public float CullingCellSize => _cullingCellSize;
        public int CullingClientsPerFrame => _cullingClientsPerFrame;
        public float CullingHysteresis => _cullingHysteresis;
        public PackageThreadMode ThreadMode => _threadMode;
        public bool UseBurstTimers => _useBurstTimers;
        public bool EnableTimerDebugLogs => _enableTimerDebugLogs;
        public bool EnableDebugOverlay => _enableDebugOverlay;
        public AssetProviderType AssetProviderType => _assetProviderType;
        public SceneTransitionChannel OnTransitionStarted => _onTransitionStarted;
        public SceneTransitionChannel OnTransitionCompleted => _onTransitionCompleted;
        public string SettingsFilename => _settingsFilename;
        public InputProviderType InputProvider => _inputProvider;
        public UnityEngine.InputSystem.InputActionAsset InputActionAsset => _inputActionAsset;
        public bool EnableInputDebugger => _enableInputDebugger;

        public static void Reload() { _instance = null; _ = Instance; }
    }
}
