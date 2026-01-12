using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Eraflo.Catalyst.Core.Chronos;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Command
{
    /// <summary>
    /// Plays back a ReplayTrack.
    /// Commands are re-executed through the CommandManager (optionally bypassing history).
    /// </summary>
    public class ReplayPlayer
    {
        private readonly ReplayTrack _track;
        private readonly ISerializer _serializer;
        private readonly ChronosManager _chronos;
        private readonly MonoBehaviour _coroutineHost;

        public bool IsPlaying { get; private set; }
        public GameObject ReplaySubject { get; set; }
        public event Action OnPlaybackFinished;

        public ReplayPlayer(ReplayTrack track, MonoBehaviour coroutineHost, GameObject replaySubject = null)
        {
            _track = track;
            _coroutineHost = coroutineHost;
            ReplaySubject = replaySubject;
            _serializer = App.Get<SaveManager>()?.Serializer;
            _chronos = App.Get<ChronosManager>();
        }

        public void Play()
        {
            if (IsPlaying || _track == null || _track.Frames.Count == 0) return;
            _coroutineHost.StartCoroutine(PlaybackRoutine());
        }

        private IEnumerator PlaybackRoutine()
        {
            IsPlaying = true;
            
            float startTime = _chronos != null ? _chronos.AppTime : Time.time;
            float recordingStartTime = _track.Frames[0].Timestamp;
            int frameIndex = 0;

            while (frameIndex < _track.Frames.Count)
            {
                float currentTime = (_chronos != null ? _chronos.AppTime : Time.time) - startTime;
                float frameRelativeTime = _track.Frames[frameIndex].Timestamp - recordingStartTime;

                if (currentTime >= frameRelativeTime)
                {
                    ExecuteFrame(_track.Frames[frameIndex]);
                    frameIndex++;
                }
                
                yield return null;
            }

            IsPlaying = false;
            OnPlaybackFinished?.Invoke();
        }

        private async void ExecuteFrame(ReplayFrame frame)
        {
            try
            {
                Type type = Type.GetType(frame.CommandType);
                if (type == null)
                {
                    Debug.LogWarning($"[ReplayPlayer] Unknown command type: {frame.CommandType}");
                    return;
                }

                ICommand command = (ICommand)Activator.CreateInstance(type);
                if (command != null)
                {
                    _serializer.Populate(frame.CommandData, command);
                    
                    // Handle Target Redirection
                    if (command is IRebindableCommand rebindable && ReplaySubject != null)
                    {
                        rebindable.Rebind(ReplaySubject);
                    }

                    // Execute through manager bypassing history recording
                    var manager = App.Get<CommandManager>();
                    if (manager != null) await manager.ExecuteDirect(command);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReplayPlayer] Failed to execute replay frame: {e.Message}");
            }
        }
    }
}
