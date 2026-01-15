using System;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Voice
{
    /// <summary>
    /// Service for managing voice chat across the network.
    /// </summary>
    [Service(Priority = 10)]
    public class VoiceManager : IGameService
    {
        private IVoiceProvider _provider;
        private NetworkManager _networkManager;
        
        // Settings
        private float _microphoneVolume = 1f;
        private float _speakerVolume = 1f;
        private bool _use3DAudio = true;
        
        #region Properties
        
        /// <summary>Current voice provider.</summary>
        public IVoiceProvider Provider => _provider;
        
        /// <summary>Whether voice is available and initialized.</summary>
        public bool IsAvailable => _provider?.IsInitialized ?? false;
        
        /// <summary>Whether we're in a voice channel.</summary>
        public bool IsInChannel => _provider?.IsInChannel ?? false;
        
        /// <summary>Current channel name.</summary>
        public string CurrentChannel => _provider?.CurrentChannel;
        
        /// <summary>Whether local user is muted.</summary>
        public bool IsMuted => _provider?.IsMuted ?? true;
        
        /// <summary>Whether local user is speaking.</summary>
        public bool IsSpeaking => _provider?.IsSpeaking ?? false;
        
        /// <summary>Microphone volume (0-1).</summary>
        public float MicrophoneVolume
        {
            get => _microphoneVolume;
            set
            {
                _microphoneVolume = Mathf.Clamp01(value);
                _provider?.SetMicrophoneVolume(_microphoneVolume);
            }
        }
        
        /// <summary>Speaker volume (0-1).</summary>
        public float SpeakerVolume
        {
            get => _speakerVolume;
            set
            {
                _speakerVolume = Mathf.Clamp01(value);
                _provider?.SetSpeakerVolume(_speakerVolume);
            }
        }
        
        /// <summary>Master audio volume (alias for SpeakerVolume).</summary>
        public float MasterVolume
        {
            get => SpeakerVolume;
            set => SpeakerVolume = value;
        }
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when local speaking state changes.</summary>
        public event Action<bool> OnSpeakingStateChanged;
        
        /// <summary>Fired when a remote player starts/stops speaking.</summary>
        public event Action<ulong, bool> OnRemoteSpeakingChanged;
        
        /// <summary>Fired when joining a channel.</summary>
        public event Action<bool, string> OnChannelJoined;
        
        /// <summary>Fired when leaving a channel.</summary>
        public event Action OnChannelLeft;
        
        #endregion
        
        #region IGameService
        
        public void Initialize()
        {
            _networkManager = App.Get<NetworkManager>();
        }
        
        public void Shutdown()
        {
            if (_provider != null)
            {
                UnsubscribeFromProvider();
                _provider.Shutdown();
                _provider = null;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Sets the voice provider implementation.
        /// </summary>
        public void SetProvider(IVoiceProvider provider)
        {
            if (_provider != null)
            {
                UnsubscribeFromProvider();
                _provider.Shutdown();
            }
            
            _provider = provider;
            
            if (_provider != null)
            {
                _provider.Initialize();
                SubscribeToProvider();
                
                // Apply current settings
                _provider.SetMicrophoneVolume(_microphoneVolume);
                _provider.SetSpeakerVolume(_speakerVolume);
            }
        }
        
        /// <summary>
        /// Joins a voice channel.
        /// </summary>
        /// <param name="channelName">Channel to join. If null, uses the network session ID.</param>
        /// <param name="use3D">Whether to use 3D spatial audio.</param>
        public void JoinChannel(string channelName = null, bool? use3D = null)
        {
            if (_provider == null)
            {
                Debug.LogWarning("[VoiceManager] No voice provider set.");
                return;
            }
            
            string channel = channelName ?? $"Session_{_networkManager?.LocalClientId ?? 0}";
            _use3DAudio = use3D ?? _use3DAudio;
            
            _provider.JoinChannel(channel, _use3DAudio);
        }
        
        /// <summary>
        /// Leaves the current voice channel.
        /// </summary>
        public void LeaveChannel()
        {
            _provider?.LeaveChannel();
        }
        
        /// <summary>
        /// Sets the muted state.
        /// </summary>
        public void SetMuted(bool muted)
        {
            _provider?.SetMuted(muted);
        }
        
        public void SetMicEnabled(bool enabled)
        {
            _provider?.SetMicEnabled(enabled);
        }
        
        /// <summary>
        /// Toggles mute state.
        /// </summary>
        public void ToggleMute()
        {
            if (_provider != null)
            {
                _provider.SetMuted(!_provider.IsMuted);
            }
        }
        
        /// <summary>
        /// Updates listener position for 3D audio. Call each frame.
        /// </summary>
        public void UpdateListenerPosition(Transform listener)
        {
            if (_provider == null || listener == null || !_use3DAudio)
                return;
            
            _provider.UpdateListenerPosition(
                listener.position,
                listener.forward,
                listener.up
            );
        }
        
        /// <summary>
        /// Mutes/unmutes a specific player.
        /// </summary>
        public void SetPlayerMuted(ulong playerId, bool muted)
        {
            _provider?.SetParticipantMuted(playerId, muted);
        }
        
        #endregion
        
        #region Private Methods
        
        private void SubscribeToProvider()
        {
            if (_provider == null) return;
            
            _provider.OnSpeakingStateChanged += HandleSpeakingStateChanged;
            _provider.OnRemoteSpeakingChanged += HandleRemoteSpeakingChanged;
            _provider.OnChannelJoined += HandleChannelJoined;
            _provider.OnChannelLeft += HandleChannelLeft;
        }
        
        private void UnsubscribeFromProvider()
        {
            if (_provider == null) return;
            
            _provider.OnSpeakingStateChanged -= HandleSpeakingStateChanged;
            _provider.OnRemoteSpeakingChanged -= HandleRemoteSpeakingChanged;
            _provider.OnChannelJoined -= HandleChannelJoined;
            _provider.OnChannelLeft -= HandleChannelLeft;
        }
        
        private void HandleSpeakingStateChanged(bool speaking)
        {
            OnSpeakingStateChanged?.Invoke(speaking);
        }
        
        private void HandleRemoteSpeakingChanged(ulong participantId, bool speaking)
        {
            OnRemoteSpeakingChanged?.Invoke(participantId, speaking);
        }
        
        private void HandleChannelJoined(bool success, string channelName)
        {
            OnChannelJoined?.Invoke(success, channelName);
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[VoiceManager] Channel joined: {channelName} (success: {success})");
            }
        }
        
        private void HandleChannelLeft()
        {
            OnChannelLeft?.Invoke();
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log("[VoiceManager] Left channel");
            }
        }
        
        #endregion
    }
}
