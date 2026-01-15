using System;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Voice
{
    /// <summary>
    /// Mock voice provider for testing without actual voice services.
    /// </summary>
    public class MockVoiceProvider : IVoiceProvider
    {
        private bool _isInitialized;
        private bool _isInChannel;
        private string _currentChannel;
        private bool _isMuted;
        private bool _isSpeaking;
        private bool _micEnabled = true;
        
        public bool IsInitialized => _isInitialized;
        public bool IsInChannel => _isInChannel;
        public string CurrentChannel => _currentChannel;
        public bool IsMuted => _isMuted;
        public bool IsSpeaking => _isSpeaking;
        public bool MicEnabled => _micEnabled;
        
        public event Action<bool> OnSpeakingStateChanged;
        public event Action<ulong, bool> OnRemoteSpeakingChanged;
        public event Action<bool, string> OnChannelJoined;
        public event Action OnChannelLeft;
        
        public void Initialize()
        {
            _isInitialized = true;
            Debug.Log("[MockVoiceProvider] Initialized");
        }
        
        public void Shutdown()
        {
            if (_isInChannel)
                LeaveChannel();
            
            _isInitialized = false;
            Debug.Log("[MockVoiceProvider] Shutdown");
        }
        
        public void JoinChannel(string channelName, bool is3D = false)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[MockVoiceProvider] Not initialized");
                OnChannelJoined?.Invoke(false, channelName);
                return;
            }
            
            _currentChannel = channelName;
            _isInChannel = true;
            
            Debug.Log($"[MockVoiceProvider] Joined channel: {channelName} (3D: {is3D})");
            OnChannelJoined?.Invoke(true, channelName);
        }
        
        public void LeaveChannel()
        {
            if (!_isInChannel)
                return;
            
            Debug.Log($"[MockVoiceProvider] Left channel: {_currentChannel}");
            
            _currentChannel = null;
            _isInChannel = false;
            _isSpeaking = false;
            
            OnChannelLeft?.Invoke();
        }
        
        public void SetMuted(bool muted)
        {
            _isMuted = muted;
            _micEnabled = !muted;
            
            if (muted && _isSpeaking)
            {
                _isSpeaking = false;
                OnSpeakingStateChanged?.Invoke(false);
            }
            
            Debug.Log($"[MockVoiceProvider] Muted: {muted}");
        }

        public void SetMicEnabled(bool enabled)
        {
            _micEnabled = enabled;
            _isMuted = !enabled;
            Debug.Log($"[MockVoiceProvider] Mic enabled: {enabled}");
        }
        
        public void SetMicrophoneVolume(float volume)
        {
            Debug.Log($"[MockVoiceProvider] Mic volume: {volume:P0}");
        }
        
        public void SetSpeakerVolume(float volume)
        {
            Debug.Log($"[MockVoiceProvider] Speaker volume: {volume:P0}");
        }
        
        public void UpdateListenerPosition(Vector3 position, Vector3 forward, Vector3 up)
        {
            // No-op for mock
        }
        
        public void SetParticipantMuted(ulong participantId, bool muted)
        {
            Debug.Log($"[MockVoiceProvider] Participant {participantId} muted: {muted}");
        }
        
        /// <summary>
        /// Simulates speaking state change (for testing).
        /// </summary>
        public void SimulateSpeaking(bool speaking)
        {
            if (!_isInChannel || _isMuted)
                return;
            
            if (_isSpeaking != speaking)
            {
                _isSpeaking = speaking;
                OnSpeakingStateChanged?.Invoke(speaking);
            }
        }
        
        /// <summary>
        /// Simulates remote participant speaking (for testing).
        /// </summary>
        public void SimulateRemoteSpeaking(ulong participantId, bool speaking)
        {
            OnRemoteSpeakingChanged?.Invoke(participantId, speaking);
        }
    }
}
