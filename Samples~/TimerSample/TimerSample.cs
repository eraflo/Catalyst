using UnityEngine;
using Eraflo.Catalyst.Timers;
using Eraflo.Catalyst.EasingSystem;

namespace Eraflo.Catalyst.Samples.Timers
{
    /// <summary>
    /// Sample demonstrating the Timer system.
    /// Attach to any GameObject in the scene.
    /// </summary>
    public class TimerSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float countdownDuration = 5f;
        [SerializeField] private float delayDuration = 2f;
        [SerializeField] private float repeatInterval = 1f;

        private TimerHandle _countdownHandle;
        private TimerHandle _stopwatchHandle;
        private TimerHandle _repeatingHandle;

        private float _countdownProgress;
        private float _stopwatchTime;
        private int _repeatCount;

        private void Start()
        {
            Debug.Log("[Timer Sample] Started. Use UI buttons to test timers.");
        }

        private void OnDestroy()
        {
            // Clean up timers
            if (_countdownHandle.IsValid) Timer.Cancel(_countdownHandle);
            if (_stopwatchHandle.IsValid) Timer.Cancel(_stopwatchHandle);
            if (_repeatingHandle.IsValid) Timer.Cancel(_repeatingHandle);
        }

        private void Update()
        {
            // Update displays
            if (_countdownHandle.IsValid && Timer.IsRunning(_countdownHandle))
            {
                _countdownProgress = Timer.GetEasedProgress(_countdownHandle, EasingType.QuadOut);
            }

            if (_stopwatchHandle.IsValid && Timer.IsRunning(_stopwatchHandle))
            {
                _stopwatchTime = Timer.GetCurrentTime(_stopwatchHandle);
            }
        }

        public void StartCountdown()
        {
            if (_countdownHandle.IsValid) Timer.Cancel(_countdownHandle);

            _countdownHandle = Timer.Create<CountdownTimer>(countdownDuration);
            Timer.On<OnTick, float>(_countdownHandle, (dt) => _countdownProgress = Timer.GetProgress(_countdownHandle));
            Timer.On<OnComplete>(_countdownHandle, () => Debug.Log("<color=green>[COUNTDOWN]</color> Completed!"));

            Debug.Log($"[COUNTDOWN] Started {countdownDuration}s countdown");
        }

        public void StartStopwatch()
        {
            if (_stopwatchHandle.IsValid) Timer.Cancel(_stopwatchHandle);

            _stopwatchHandle = Timer.Create<StopwatchTimer>(0f);
            Debug.Log("[STOPWATCH] Started");
        }

        public void StopStopwatch()
        {
            if (_stopwatchHandle.IsValid)
            {
                Timer.Pause(_stopwatchHandle);
                Debug.Log($"[STOPWATCH] Stopped at {_stopwatchTime:F2}s");
            }
        }

        public void StartRepeating()
        {
            if (_repeatingHandle.IsValid) Timer.Cancel(_repeatingHandle);

            _repeatCount = 0;
            _repeatingHandle = Timer.Create<RepeatingTimer>(repeatInterval);
            Timer.On<OnRepeat, int>(_repeatingHandle, (count) =>
            {
                _repeatCount = count;
                Debug.Log($"<color=cyan>[REPEAT]</color> Tick #{count}");
            });

            Debug.Log($"[REPEAT] Started repeating every {repeatInterval}s");
        }

        public void SimpleDelay()
        {
            Timer.Delay(delayDuration, () => Debug.Log($"<color=yellow>[DELAY]</color> Fired after {delayDuration}s!"));
            Debug.Log($"[DELAY] Scheduled for {delayDuration}s");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 350));
            GUILayout.Box("Timer Sample");

            // Countdown
            GUILayout.Label($"Countdown: {_countdownProgress:P0}");
            if (GUILayout.Button("Start Countdown")) StartCountdown();

            GUILayout.Space(10);

            // Stopwatch
            GUILayout.Label($"Stopwatch: {_stopwatchTime:F2}s");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start")) StartStopwatch();
            if (GUILayout.Button("Stop")) StopStopwatch();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Repeating
            GUILayout.Label($"Repeat Count: {_repeatCount}");
            if (GUILayout.Button("Start Repeating")) StartRepeating();

            GUILayout.Space(10);

            // Simple Delay
            if (GUILayout.Button("Simple Delay")) SimpleDelay();

            GUILayout.Space(10);

            // Metrics
            var m = Timer.Metrics;
            GUILayout.Label($"Active: {Timer.Count} | Created: {m.TotalCreated}");

            GUILayout.EndArea();
        }
    }
}
