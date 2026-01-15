using UnityEngine;
using Eraflo.Catalyst.Core.Chronos;

namespace Eraflo.Catalyst.Networking.Features.Voice
{
    /// <summary>
    /// Component for per-player voice state and 3D spatialization.
    /// Attach to player prefabs to enable voice indicators and spatial audio.
    /// </summary>
    [AddComponentMenu("Catalyst/Networking/Network Voice Source")]
    public class NetworkVoiceSource : MonoBehaviour
    {
        [Header("Network")]
        [Tooltip("Network ID of the owning player.")]
        [SerializeField] private ulong _ownerId;
        
        [Header("Audio")]
        [Tooltip("Audio source for 3D spatialization.")]
        [SerializeField] private AudioSource _audioSource;
        
        [Header("Sync Settings")]
        [Tooltip("How often to sync speaking state (seconds).")]
        [SerializeField] private float _syncInterval = 0.1f;
        
        [Header("Visualization")]
        [Tooltip("Optional object to show when speaking.")]
        [SerializeField] private GameObject _speakingIndicator;
        
        private VoiceManager _voiceManager;
        private NetworkManager _networkManager;
        private ChronosManager _chronos;
        
        private NetworkProperty<bool> _isSpeaking;
        private float _lastSyncTime;
        private bool _lastSpeakingState;
        
        /// <summary>Whether this player is currently speaking.</summary>
        public bool IsSpeaking => _isSpeaking?.Value ?? false;
        
        /// <summary>Network ID of the owner.</summary>
        public ulong OwnerId => _ownerId;
        
        /// <summary>
        /// Initializes references and creates networked speaking state property.
        /// </summary>
        private void Start()
        {
            _voiceManager = App.Get<VoiceManager>();
            _networkManager = App.Get<NetworkManager>();
            _chronos = App.Get<ChronosManager>();
            
            // Create networked property for speaking state
            var idManager = App.Get<NetworkIdManager>();
            uint networkId = this.gameObject.GetNetworkId();
            
            if (networkId != 0)
            {
                _isSpeaking = new NetworkProperty<bool>(
                    name: "IsSpeaking",
                    networkId: networkId,
                    defaultValue: false
                );
                
                _isSpeaking.OnValueChanged += OnSpeakingChanged;
            }
            
            // Subscribe to remote speaking events
            if (_voiceManager != null)
            {
                _voiceManager.OnRemoteSpeakingChanged += OnRemoteSpeakingChanged;
                
                // If this is the local player, also subscribe to local events
                if (IsLocalPlayer())
                {
                    _voiceManager.OnSpeakingStateChanged += OnLocalSpeakingChanged;
                }
            }
            
            UpdateIndicator(false);
        }
        
        /// <summary>
        /// Cleans up subscriptions when destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_isSpeaking != null)
            {
                _isSpeaking.OnValueChanged -= OnSpeakingChanged;
            }
            
            if (_voiceManager != null)
            {
                _voiceManager.OnRemoteSpeakingChanged -= OnRemoteSpeakingChanged;
                _voiceManager.OnSpeakingStateChanged -= OnLocalSpeakingChanged;
            }
        }
        
        /// <summary>
        /// Updates listener position and syncs speaking state at configured interval.
        /// </summary>
        private void Update()
        {
            // Update 3D audio position for voice manager
            if (IsLocalPlayer() && _voiceManager != null)
            {
                _voiceManager.UpdateListenerPosition(transform);
            }
            
            // Get current time from Chronos (unscaled for network timing)
            float currentTime = _chronos?.UnscaledTime ?? Time.unscaledTime;
            
            // Sync speaking state at interval (local player only, server authority)
            if (IsLocalPlayer() && _networkManager != null && _networkManager.IsServer && 
                currentTime - _lastSyncTime >= _syncInterval)
            {
                _lastSyncTime = currentTime;
                
                bool currentSpeaking = _voiceManager?.IsSpeaking ?? false;
                if (currentSpeaking != _lastSpeakingState)
                {
                    _lastSpeakingState = currentSpeaking;
                    if (_isSpeaking != null)
                    {
                        _isSpeaking.Value = currentSpeaking;
                    }
                }
            }
        }
        
        /// <summary>
        /// Sets the owner ID (called by spawn system).
        /// </summary>
        /// <param name="ownerId">Network client ID of the owning player.</param>
        public void SetOwner(ulong ownerId)
        {
            _ownerId = ownerId;
        }
        
        /// <summary>
        /// Checks if this component belongs to the local player.
        /// </summary>
        private bool IsLocalPlayer()
        {
            if (_networkManager == null)
                return false;
            
            return _ownerId == _networkManager.LocalClientId;
        }
        
        /// <summary>
        /// Handles speaking state changes from the NetworkProperty.
        /// </summary>
        private void OnSpeakingChanged(bool speaking)
        {
            UpdateIndicator(speaking);
        }
        
        /// <summary>
        /// Handles local speaking state changes and syncs to network.
        /// </summary>
        private void OnLocalSpeakingChanged(bool speaking)
        {
            // Local player speaking state changed - sync to network if server
            if (_isSpeaking != null && _networkManager != null && _networkManager.IsServer)
            {
                _isSpeaking.Value = speaking;
            }
            UpdateIndicator(speaking);
        }
        
        /// <summary>
        /// Handles remote participant speaking state changes.
        /// </summary>
        private void OnRemoteSpeakingChanged(ulong participantId, bool speaking)
        {
            // Check if this is our owner and we're not the local player
            if (participantId == _ownerId && !IsLocalPlayer())
            {
                UpdateIndicator(speaking);
            }
        }
        
        /// <summary>
        /// Updates the visual speaking indicator.
        /// </summary>
        private void UpdateIndicator(bool speaking)
        {
            if (_speakingIndicator != null)
            {
                _speakingIndicator.SetActive(speaking);
            }
        }
    }
}
