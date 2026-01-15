using System;

namespace Eraflo.Catalyst.Networking.Features.Voice
{
    /// <summary>
    /// Interface for voice chat providers (Vivox, Photon Voice, etc.).
    /// </summary>
    public interface IVoiceProvider
    {
        /// <summary>Whether the provider is initialized and ready.</summary>
        bool IsInitialized { get; }
        
        /// <summary>Whether the local user is currently in a channel.</summary>
        bool IsInChannel { get; }
        
        /// <summary>Name of the current channel, or null if not in a channel.</summary>
        string CurrentChannel { get; }
        
        /// <summary>Whether the local user is muted.</summary>
        bool IsMuted { get; }
        
        /// <summary>Whether the local user is currently speaking.</summary>
        bool IsSpeaking { get; }
        
        /// <summary>Fired when speaking state changes (true = started speaking).</summary>
        event Action<bool> OnSpeakingStateChanged;
        
        /// <summary>Fired when a remote participant starts/stops speaking.</summary>
        event Action<ulong, bool> OnRemoteSpeakingChanged; // participantId, isSpeaking
        
        /// <summary>Fired when joining a channel succeeds or fails.</summary>
        event Action<bool, string> OnChannelJoined; // success, channelName
        
        /// <summary>Fired when leaving a channel.</summary>
        event Action OnChannelLeft;
        
        /// <summary>
        /// Initializes the voice provider.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Shuts down the voice provider.
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// Joins a voice channel.
        /// </summary>
        /// <param name="channelName">Name of the channel to join.</param>
        /// <param name="is3D">Whether to use 3D spatial audio.</param>
        void JoinChannel(string channelName, bool is3D = false);
        
        /// <summary>
        /// Leaves the current voice channel.
        /// </summary>
        void LeaveChannel();
        
        /// <summary>
        /// Sets the muted state for the local user.
        /// </summary>
        void SetMuted(bool muted);

        /// <summary>
        /// Sets whether the local microphone is enabled.
        /// </summary>
        void SetMicEnabled(bool enabled);
        
        /// <summary>
        /// Sets the local microphone volume (0-1).
        /// </summary>
        void SetMicrophoneVolume(float volume);
        
        /// <summary>
        /// Sets the speaker/output volume (0-1).
        /// </summary>
        void SetSpeakerVolume(float volume);
        
        /// <summary>
        /// Updates 3D position for spatial audio.
        /// </summary>
        /// <param name="position">Listener position.</param>
        /// <param name="forward">Listener forward direction.</param>
        /// <param name="up">Listener up direction.</param>
        void UpdateListenerPosition(UnityEngine.Vector3 position, UnityEngine.Vector3 forward, UnityEngine.Vector3 up);
        
        /// <summary>
        /// Mutes/unmutes a specific remote participant.
        /// </summary>
        void SetParticipantMuted(ulong participantId, bool muted);
    }
}
