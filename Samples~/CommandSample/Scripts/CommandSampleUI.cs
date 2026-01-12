using UnityEngine;
using UnityEngine.UI;
using Eraflo.Catalyst.Command;
using Eraflo.Catalyst.Command.Examples;
using Eraflo.Catalyst.Command.Features.Replay;
using Eraflo.Catalyst.Command.UI;

namespace Eraflo.Catalyst.Samples.Command
{
    public class CommandSampleUI : MonoBehaviour
    {
        public GameObject Actor;
        public Text StatusText;
        
        private ReplayRecorder _recorder;
        private ReplayTrack _lastTrack;
        private GameObject _currentGhost;

        private void Start()
        {
            StatusText.text = "Click to move Actor | Undo/Redo to revert";
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    _ = App.Get<CommandManager>().Execute(new MoveCommand(Actor, hit.point));
                }
            }

            // Keyboard shortcuts for easier testing
            if (Input.GetKeyDown(KeyCode.R)) StartRecording();
            if (Input.GetKeyDown(KeyCode.S)) StopRecording();
            if (Input.GetKeyDown(KeyCode.P)) PlayReplay();
        }

        public void StartRecording()
        {
            _recorder = new ReplayRecorder("Sample_Recording");
            _recorder.Start();
            StatusText.text = "RECORDING...";
        }

        public void StopRecording()
        {
            if (_recorder != null)
            {
                _recorder.Stop();
                _lastTrack = _recorder.Track;
                StatusText.text = "Recorded! Click Play to see Ghost.";
            }
        }

        public void PlayReplay()
        {
            if (_lastTrack != null)
            {
                // Cleanup previous ghost if it exists
                if (_currentGhost != null)
                {
                    Destroy(_currentGhost);
                }

                // Spawn a new ghost (simple cube)
                _currentGhost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _currentGhost.name = "Ghost";
                
                // Color it differently
                var renderer = _currentGhost.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(1, 1, 1, 0.5f);

                // Create Ghost and play back on it using automatic ReplaySubject redirection
                var player = new ReplayPlayer(_lastTrack, this, _currentGhost);
                player.Play();
                
                StatusText.text = "PLAYING REPLAY (GHOST)...";
                player.OnPlaybackFinished += () => {
                    StatusText.text = "Playback Finished.";
                    Destroy(_currentGhost);
                };
            }
        }
    }
}
