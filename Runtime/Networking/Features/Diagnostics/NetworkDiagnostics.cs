using System.Text;
using UnityEngine;
using Eraflo.Catalyst.Core.Chronos;

namespace Eraflo.Catalyst.Networking.Features.Diagnostics
{
    /// <summary>
    /// Service for network simulation and real-time diagnostics.
    /// </summary>
    [Service(Priority = 4)]
    public class NetworkDiagnostics : IGameService
    {
        private NetworkManager _networkManager;
        private ChronosManager _chronos;
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.25f; // 4Hz updates
        
        // Cached metrics
        private float _rtt;
        private float _packetLoss;
        private float _bandwidthIn;
        private float _bandwidthOut;
        
        // Simulation settings (read from PackageSettings)
        private int _simulatedLatencyMs;
        private float _simulatedPacketLossPercent;
        private int _simulatedJitterMs;
        
        #region Properties
        
        /// <summary>Real-time Round Trip Time in milliseconds.</summary>
        public float RTT { get { UpdateMetrics(); return _rtt; } }
        
        /// <summary>Measured packet loss percentage (0-100).</summary>
        public float PacketLoss { get { UpdateMetrics(); return _packetLoss; } }
        
        /// <summary>Inbound bandwidth in KB/s.</summary>
        public float BandwidthIn { get { UpdateMetrics(); return _bandwidthIn; } }
        
        /// <summary>Outbound bandwidth in KB/s.</summary>
        public float BandwidthOut { get { UpdateMetrics(); return _bandwidthOut; } }
        
        /// <summary>Whether simulation is currently active.</summary>
        public bool IsSimulationActive => _simulatedLatencyMs > 0 || _simulatedPacketLossPercent > 0;
        
        /// <summary>Current simulated latency in ms.</summary>
        public int SimulatedLatencyMs => _simulatedLatencyMs;
        
        /// <summary>Current simulated packet loss percent.</summary>
        public float SimulatedPacketLossPercent => _simulatedPacketLossPercent;
        
        /// <summary>Current simulated jitter in ms.</summary>
        public int SimulatedJitterMs => _simulatedJitterMs;
        
        #endregion
        
        #region IGameService
        
        /// <summary>
        /// Initializes the diagnostics service, loading simulation settings from PackageSettings.
        /// </summary>
        public void Initialize()
        {
            _networkManager = App.Get<NetworkManager>();
            _chronos = App.Get<ChronosManager>();
            
            // Allow immediate first update
            _lastUpdateTime = -UpdateInterval;
            
            // Load simulation settings from PackageSettings
            LoadSimulationSettings();
            
            // Subscribe to backend changes to apply simulation
            if (_networkManager != null)
            {
                _networkManager.OnBackendChanged += OnBackendChanged;
                
                // Apply to current backend if exists
                if (_networkManager.Backend != null)
                {
                    ApplySimulation(_networkManager.Backend);
                }
            }
        }
        
        /// <summary>
        /// Shuts down the diagnostics service and unsubscribes from events.
        /// </summary>
        public void Shutdown()
        {
            if (_networkManager != null)
            {
                _networkManager.OnBackendChanged -= OnBackendChanged;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Updates metrics from the backend. Call periodically for fresh data.
        /// Uses ChronosManager for timing to respect time scale.
        /// </summary>
        public void UpdateMetrics()
        {
            float currentTime = _chronos?.UnscaledTime ?? Time.unscaledTime;
            
            if (currentTime - _lastUpdateTime < UpdateInterval)
                return;
            
            _lastUpdateTime = currentTime;
            
            if (_networkManager?.Backend is ISimulationBackend simBackend)
            {
                _rtt = simBackend.GetRTT();
                _packetLoss = simBackend.GetPacketLoss();
                _bandwidthIn = simBackend.GetBandwidthIn();
                _bandwidthOut = simBackend.GetBandwidthOut();
            }
        }
        
        /// <summary>
        /// Sets simulation parameters at runtime.
        /// </summary>
        public void SetSimulation(int latencyMs, float packetLossPercent, int jitterMs)
        {
            _simulatedLatencyMs = Mathf.Max(0, latencyMs);
            _simulatedPacketLossPercent = Mathf.Clamp(packetLossPercent, 0f, 100f);
            _simulatedJitterMs = Mathf.Max(0, jitterMs);
            
            if (_networkManager?.Backend != null)
            {
                ApplySimulation(_networkManager.Backend);
            }
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkDiagnostics] Simulation: {latencyMs}ms latency, {packetLossPercent}% loss, {jitterMs}ms jitter");
            }
        }
        
        /// <summary>
        /// Disables simulation.
        /// </summary>
        public void DisableSimulation()
        {
            SetSimulation(0, 0, 0);
        }
        
        /// <summary>
        /// Gets a formatted string of current metrics.
        /// </summary>
        public string GetMetricsString()
        {
            UpdateMetrics();
            
            var sb = new StringBuilder(128);
            sb.Append($"RTT: {_rtt:F1}ms");
            sb.Append($" | Loss: {_packetLoss:F1}%");
            sb.Append($" | In: {_bandwidthIn:F1} KB/s");
            sb.Append($" | Out: {_bandwidthOut:F1} KB/s");
            
            if (IsSimulationActive)
            {
                sb.Append(" [SIM]");
            }
            
            return sb.ToString();
        }
        
        #endregion
        
        #region Private Methods
        
        private void OnBackendChanged(INetworkBackend backend)
        {
            if (backend != null)
            {
                ApplySimulation(backend);
            }
        }
        
        private void ApplySimulation(INetworkBackend backend)
        {
            if (backend is ISimulationBackend simBackend)
            {
                simBackend.ApplySimulationParameters(
                    _simulatedLatencyMs,
                    _simulatedPacketLossPercent,
                    _simulatedJitterMs);
            }
            else if (IsSimulationActive)
            {
                Debug.LogWarning("[NetworkDiagnostics] Current backend does not support simulation (ISimulationBackend).");
            }
        }
        
        /// <summary>
        /// Loads simulation settings from PackageSettings.
        /// </summary>
        private void LoadSimulationSettings()
        {
            var settings = PackageSettings.Instance;
            _simulatedLatencyMs = settings.SimulateLatencyMs;
            _simulatedPacketLossPercent = settings.SimulatePacketLossPercent;
            _simulatedJitterMs = settings.SimulateJitterMs;
        }
        
        #endregion
    }
}
