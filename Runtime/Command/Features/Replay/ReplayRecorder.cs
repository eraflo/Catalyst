using UnityEngine;
using Eraflo.Catalyst.Core.Save;
using Eraflo.Catalyst.Events;

namespace Eraflo.Catalyst.Command
{
    /// <summary>
    /// Utility that listens to CommandManager and records frames into a ReplayTrack.
    /// </summary>
    public class ReplayRecorder
    {
        private readonly ReplayTrack _track;
        private readonly ISerializer _serializer;
        private readonly EventBus _eventBus;

        public bool IsRecording { get; private set; }
        public ReplayTrack Track => _track;

        public ReplayRecorder(string name = "New Replay")
        {
            _track = new ReplayTrack { Name = name };
            _eventBus = App.Get<EventBus>();
            _serializer = App.Get<SaveManager>()?.Serializer;
        }

        public void Start()
        {
            if (IsRecording) return;
            if (_eventBus == null || _serializer == null)
            {
                Debug.LogError("[ReplayRecorder] Missing EventBus or SaveManager.Serializer.");
                return;
            }

            _eventBus.Subscribe<CommandExecutedEvent>(OnCommandExecuted);
            IsRecording = true;
        }

        public void Stop()
        {
            if (!IsRecording) return;
            _eventBus?.Unsubscribe<CommandExecutedEvent>(OnCommandExecuted);
            IsRecording = false;
        }

        private void OnCommandExecuted(CommandExecutedEvent evt)
        {
            if (evt.Command == null) return;

            var frame = new ReplayFrame
            {
                Timestamp = evt.Timestamp,
                CommandType = evt.Command.GetType().AssemblyQualifiedName,
                CommandData = _serializer.Serialize(evt.Command)
            };

            _track.Frames.Add(frame);
        }
    }
}
