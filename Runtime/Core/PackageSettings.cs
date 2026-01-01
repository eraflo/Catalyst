using System.Collections.Generic;
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

        // Timers
        [SerializeField] private bool _useBurstTimers = false;
        [SerializeField] private bool _enableTimerDebugLogs = false;
        [SerializeField] private bool _enableDebugOverlay = false;

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
        public PackageThreadMode ThreadMode => _threadMode;
        public bool UseBurstTimers => _useBurstTimers;
        public bool EnableTimerDebugLogs => _enableTimerDebugLogs;
        public bool EnableDebugOverlay => _enableDebugOverlay;

        public static void Reload() { _instance = null; _ = Instance; }
    }
}
