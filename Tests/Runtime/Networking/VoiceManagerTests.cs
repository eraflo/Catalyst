using System;
using NUnit.Framework;
using UnityEngine;
using Eraflo.Catalyst;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Features.Voice;

namespace Eraflo.Catalyst.Tests.Runtime.Networking
{
    [TestFixture]
    public class VoiceManagerTests
    {
        private VoiceManager _voiceManager;
        private MockVoiceProvider _mockProvider;

        [SetUp]
        public void SetUp()
        {
            _voiceManager = new VoiceManager();
            _mockProvider = new MockVoiceProvider();
            
            App.Register<VoiceManager>(_voiceManager);
            _voiceManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _voiceManager?.Shutdown();
            App.Shutdown();
        }

        [Test]
        public void SetProvider_SetsActiveProvider()
        {
            // Act
            _voiceManager.SetProvider(_mockProvider);
            
            // Assert
            Assert.IsNotNull(_voiceManager.Provider);
            Assert.AreEqual(_mockProvider, _voiceManager.Provider);
        }

        [Test]
        public void JoinChannel_CallsProvider()
        {
            // Arrange
            _voiceManager.SetProvider(_mockProvider);
            
            // Act
            _voiceManager.JoinChannel("TestChannel", use3D: true);
            
            // Assert
            Assert.AreEqual("TestChannel", _mockProvider.LastJoinedChannel);
            Assert.IsTrue(_mockProvider.LastUse3D);
        }

        [Test]
        public void LeaveChannel_CallsProvider()
        {
            // Arrange
            _voiceManager.SetProvider(_mockProvider);
            _voiceManager.JoinChannel("TestChannel");
            
            // Act
            _voiceManager.LeaveChannel();
            
            // Assert
            Assert.IsTrue(_mockProvider.ChannelLeft);
        }

        [Test]
        public void SetMicEnabled_CallsProvider()
        {
            // Arrange
            _voiceManager.SetProvider(_mockProvider);
            
            // Act
            _voiceManager.SetMicEnabled(true);
            
            // Assert
            Assert.IsTrue(_mockProvider.MicEnabled);
            
            // Act again
            _voiceManager.SetMicEnabled(false);
            
            // Assert
            Assert.IsFalse(_mockProvider.MicEnabled);
        }

        [Test]
        public void MasterVolume_CanBeSet()
        {
            // Act
            _voiceManager.MasterVolume = 0.5f;
            
            // Assert
            Assert.AreEqual(0.5f, _voiceManager.MasterVolume);
        }

        [Test]
        public void MasterVolume_Clamped()
        {
            // Act
            _voiceManager.MasterVolume = 2.0f;
            Assert.AreEqual(1.0f, _voiceManager.MasterVolume);
            
            _voiceManager.MasterVolume = -0.5f;
            Assert.AreEqual(0.0f, _voiceManager.MasterVolume);
        }

        [Test]
        public void IsSpeaking_DefaultsFalse()
        {
            Assert.IsFalse(_voiceManager.IsSpeaking);
        }

        [Test]
        public void IsInChannel_DefaultsFalse()
        {
            Assert.IsFalse(_voiceManager.IsInChannel);
        }

        [Test]
        public void JoinChannel_SetsIsInChannel()
        {
            // Arrange
            _voiceManager.SetProvider(_mockProvider);
            
            // Act
            _voiceManager.JoinChannel("TestChannel");
            
            // Assert
            Assert.IsTrue(_voiceManager.IsInChannel);
        }

        /// <summary>
        /// Mock voice provider for testing.
        /// </summary>
        private class MockVoiceProvider : IVoiceProvider
        {
            public bool IsInitialized { get; private set; }
            public bool IsInChannel => !string.IsNullOrEmpty(CurrentChannel);
            public string CurrentChannel { get; private set; }
            public bool IsMuted { get; private set; }
            public bool IsSpeaking { get; set; }
            public bool MicEnabled { get; private set; }

            public event Action<bool> OnSpeakingStateChanged;
            public event Action<ulong, bool> OnRemoteSpeakingChanged;
            public event Action<bool, string> OnChannelJoined;
            public event Action OnChannelLeft;

            public string LastJoinedChannel { get; private set; }
            public bool LastUse3D { get; private set; }
            public bool ChannelLeft { get; private set; }

            public void Initialize()
            {
                IsInitialized = true;
            }

            public void Shutdown()
            {
                IsInitialized = false;
            }

            public void JoinChannel(string channelName, bool use3D = false)
            {
                LastJoinedChannel = channelName;
                LastUse3D = use3D;
                CurrentChannel = channelName;
                OnChannelJoined?.Invoke(true, channelName);
            }

            public void LeaveChannel()
            {
                ChannelLeft = true;
                CurrentChannel = null;
                OnChannelLeft?.Invoke();
            }

            public void SetMuted(bool muted)
            {
                IsMuted = muted;
                MicEnabled = !muted;
            }

            public void SetMicEnabled(bool enabled)
            {
                MicEnabled = enabled;
            }

            public void SetMicrophoneVolume(float volume) { }
            public void SetSpeakerVolume(float volume) { }

            public void UpdateListenerPosition(Vector3 position, Vector3 forward, Vector3 up)
            {
                // No-op for mock
            }

            public void SetParticipantMuted(ulong participantId, bool muted) { }
        }
    }
}
