using System;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Timers
{
    /// <summary>
    /// Network synchronization mode for timers.
    /// </summary>
    public enum TimerNetworkMode
    {
        /// <summary>
        /// Timer runs locally only, no network sync.
        /// </summary>
        Local,
        
        /// <summary>
        /// Timer is owned by this client and syncs state to others.
        /// Only the owner can Start/Stop/Reset the timer.
        /// </summary>
        Owner,
        
        /// <summary>
        /// Timer receives state updates from the network.
        /// Cannot directly control this timer, only receive updates.
        /// </summary>
        Remote
    }

    /// <summary>
    /// Data structure for timer network synchronization.
    /// </summary>
    [Serializable]
    public struct TimerNetworkState
    {
        public string TimerId;
        public float CurrentTime;
        public float InitialTime;
        public bool IsRunning;
        public double ServerTimestamp;
        public TimerStateAction Action;
    }

    /// <summary>
    /// Actions that can be synchronized over the network.
    /// </summary>
    public enum TimerStateAction
    {
        Sync,
        Start,
        Stop,
        Pause,
        Reset
    }

    /// <summary>
    /// A countdown timer with network synchronization support.
    /// Can synchronize timer state across clients in multiplayer games.
    /// </summary>
    public class NetworkedCountdownTimer : CountdownTimer
    {
        private readonly string _timerId;
        private TimerNetworkMode _networkMode;
        private float _syncInterval = 0.5f;
        private float _timeSinceLastSync;

        /// <summary>
        /// Event fired when timer state should be sent to the network.
        /// Subscribe to this to implement your network transport.
        /// </summary>
        public event Action<TimerNetworkState> OnNetworkSync;

        /// <summary>
        /// Unique identifier for this timer on the network.
        /// </summary>
        public string TimerId => _timerId;

        /// <summary>
        /// Current network mode of this timer.
        /// </summary>
        public TimerNetworkMode NetworkMode
        {
            get => _networkMode;
            set => _networkMode = value;
        }

        /// <summary>
        /// How often to sync timer state (in seconds).
        /// </summary>
        public float SyncInterval
        {
            get => _syncInterval;
            set => _syncInterval = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Creates a new networked countdown timer.
        /// </summary>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="timerId">Unique network identifier. Auto-generated if null.</param>
        /// <param name="networkMode">Network synchronization mode.</param>
        public NetworkedCountdownTimer(float duration, string timerId = null, TimerNetworkMode networkMode = TimerNetworkMode.Local) 
            : base(duration)
        {
            _timerId = timerId ?? Guid.NewGuid().ToString();
            _networkMode = networkMode;
        }

        /// <summary>
        /// Updates the timer and handles network synchronization.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (_networkMode == TimerNetworkMode.Owner && IsRunning)
            {
                _timeSinceLastSync += deltaTime;
                if (_timeSinceLastSync >= _syncInterval)
                {
                    SendNetworkState(TimerStateAction.Sync);
                    _timeSinceLastSync = 0f;
                }
            }
        }

        /// <summary>
        /// Starts the timer and broadcasts to network if owner.
        /// </summary>
        public new void Start()
        {
            if (_networkMode == TimerNetworkMode.Remote) return;
            
            base.Start();
            
            if (_networkMode == TimerNetworkMode.Owner)
            {
                SendNetworkState(TimerStateAction.Start);
            }
        }

        /// <summary>
        /// Stops the timer and broadcasts to network if owner.
        /// </summary>
        public new void Stop()
        {
            if (_networkMode == TimerNetworkMode.Remote) return;
            
            base.Stop();
            
            if (_networkMode == TimerNetworkMode.Owner)
            {
                SendNetworkState(TimerStateAction.Stop);
            }
        }

        /// <summary>
        /// Pauses the timer and broadcasts to network if owner.
        /// </summary>
        public new void Pause()
        {
            if (_networkMode == TimerNetworkMode.Remote) return;
            
            base.Pause();
            
            if (_networkMode == TimerNetworkMode.Owner)
            {
                SendNetworkState(TimerStateAction.Pause);
            }
        }

        /// <summary>
        /// Resets the timer and broadcasts to network if owner.
        /// </summary>
        public override void Reset()
        {
            if (_networkMode == TimerNetworkMode.Remote) return;
            
            base.Reset();
            
            if (_networkMode == TimerNetworkMode.Owner)
            {
                SendNetworkState(TimerStateAction.Reset);
            }
        }

        /// <summary>
        /// Applies state received from the network.
        /// Call this when receiving timer updates from other clients.
        /// </summary>
        /// <param name="state">The received timer state.</param>
        /// <param name="networkLatency">Estimated one-way latency in seconds for lag compensation.</param>
        public void ApplyNetworkState(TimerNetworkState state, float networkLatency = 0f)
        {
            if (_networkMode != TimerNetworkMode.Remote) return;

            // Apply lag compensation
            float compensatedTime = state.CurrentTime;
            if (state.IsRunning && networkLatency > 0)
            {
                compensatedTime -= networkLatency;
                if (compensatedTime < 0) compensatedTime = 0;
            }

            CurrentTime = compensatedTime;
            initialTime = state.InitialTime;

            switch (state.Action)
            {
                case TimerStateAction.Start:
                    base.Start();
                    break;
                case TimerStateAction.Stop:
                    base.Stop();
                    break;
                case TimerStateAction.Pause:
                    base.Pause();
                    break;
                case TimerStateAction.Reset:
                    base.Reset();
                    break;
            }
        }

        private void SendNetworkState(TimerStateAction action)
        {
            var state = new TimerNetworkState
            {
                TimerId = _timerId,
                CurrentTime = CurrentTime,
                InitialTime = initialTime,
                IsRunning = IsRunning,
                ServerTimestamp = Time.realtimeSinceStartupAsDouble,
                Action = action
            };

            OnNetworkSync?.Invoke(state);
        }
    }

    /// <summary>
    /// A stopwatch timer with network synchronization support.
    /// </summary>
    public class NetworkedStopwatchTimer : StopwatchTimer
    {
        private readonly string _timerId;
        private TimerNetworkMode _networkMode;
        private float _syncInterval = 0.5f;
        private float _timeSinceLastSync;

        /// <summary>
        /// Event fired when timer state should be sent to the network.
        /// </summary>
        public event Action<TimerNetworkState> OnNetworkSync;

        /// <summary>
        /// Unique identifier for this timer on the network.
        /// </summary>
        public string TimerId => _timerId;

        /// <summary>
        /// Current network mode of this timer.
        /// </summary>
        public TimerNetworkMode NetworkMode
        {
            get => _networkMode;
            set => _networkMode = value;
        }

        /// <summary>
        /// How often to sync timer state (in seconds).
        /// </summary>
        public float SyncInterval
        {
            get => _syncInterval;
            set => _syncInterval = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Creates a new networked stopwatch timer.
        /// </summary>
        /// <param name="timerId">Unique network identifier. Auto-generated if null.</param>
        /// <param name="networkMode">Network synchronization mode.</param>
        public NetworkedStopwatchTimer(string timerId = null, TimerNetworkMode networkMode = TimerNetworkMode.Local) 
            : base()
        {
            _timerId = timerId ?? Guid.NewGuid().ToString();
            _networkMode = networkMode;
        }

        /// <summary>
        /// Updates the timer and handles network synchronization.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            if (_networkMode == TimerNetworkMode.Owner && IsRunning)
            {
                _timeSinceLastSync += deltaTime;
                if (_timeSinceLastSync >= _syncInterval)
                {
                    SendNetworkState(TimerStateAction.Sync);
                    _timeSinceLastSync = 0f;
                }
            }
        }

        /// <summary>
        /// Starts the timer and broadcasts to network if owner.
        /// </summary>
        public new void Start()
        {
            if (_networkMode == TimerNetworkMode.Remote) return;
            
            base.Start();
            
            if (_networkMode == TimerNetworkMode.Owner)
            {
                SendNetworkState(TimerStateAction.Start);
            }
        }

        /// <summary>
        /// Stops the timer and broadcasts to network if owner.
        /// </summary>
        public new void Stop()
        {
            if (_networkMode == TimerNetworkMode.Remote) return;
            
            base.Stop();
            
            if (_networkMode == TimerNetworkMode.Owner)
            {
                SendNetworkState(TimerStateAction.Stop);
            }
        }

        /// <summary>
        /// Pauses the timer and broadcasts to network if owner.
        /// </summary>
        public new void Pause()
        {
            if (_networkMode == TimerNetworkMode.Remote) return;
            
            base.Pause();
            
            if (_networkMode == TimerNetworkMode.Owner)
            {
                SendNetworkState(TimerStateAction.Pause);
            }
        }

        /// <summary>
        /// Resets the timer and broadcasts to network if owner.
        /// </summary>
        public override void Reset()
        {
            if (_networkMode == TimerNetworkMode.Remote) return;
            
            base.Reset();
            
            if (_networkMode == TimerNetworkMode.Owner)
            {
                SendNetworkState(TimerStateAction.Reset);
            }
        }

        /// <summary>
        /// Applies state received from the network.
        /// </summary>
        /// <param name="state">The received timer state.</param>
        /// <param name="networkLatency">Estimated one-way latency in seconds for lag compensation.</param>
        public void ApplyNetworkState(TimerNetworkState state, float networkLatency = 0f)
        {
            if (_networkMode != TimerNetworkMode.Remote) return;

            // Apply lag compensation (add time for stopwatch)
            float compensatedTime = state.CurrentTime;
            if (state.IsRunning && networkLatency > 0)
            {
                compensatedTime += networkLatency;
            }

            CurrentTime = compensatedTime;

            switch (state.Action)
            {
                case TimerStateAction.Start:
                    base.Start();
                    break;
                case TimerStateAction.Stop:
                    base.Stop();
                    break;
                case TimerStateAction.Pause:
                    base.Pause();
                    break;
                case TimerStateAction.Reset:
                    base.Reset();
                    break;
            }
        }

        private void SendNetworkState(TimerStateAction action)
        {
            var state = new TimerNetworkState
            {
                TimerId = _timerId,
                CurrentTime = CurrentTime,
                InitialTime = 0f,
                IsRunning = IsRunning,
                ServerTimestamp = Time.realtimeSinceStartupAsDouble,
                Action = action
            };

            OnNetworkSync?.Invoke(state);
        }
    }
}
