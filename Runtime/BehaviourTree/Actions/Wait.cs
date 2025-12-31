using UnityEngine;
using Eraflo.UnityImportPackage.Timers;

namespace Eraflo.UnityImportPackage.BehaviourTree
{
    /// <summary>
    /// Wait action: Waits for a specified duration using the Timer system, then returns Success.
    /// </summary>
    [BehaviourTreeNode("Actions", "Wait")]
    public class Wait : ActionNode
    {
        /// <summary>Duration to wait in seconds.</summary>
        [Tooltip("Duration to wait in seconds.")]
        public float Duration = 1f;
        
        /// <summary>If true, uses unscaled time (ignores Time.timeScale).</summary>
        public bool UseUnscaledTime = false;
        
        private TimerHandle _timerHandle;
        private bool _completed;
        
        protected override void OnStart()
        {
            _completed = false;
            
            // Create a delay timer using the Timer system
            _timerHandle = Timer.Delay(Duration, () =>
            {
                _completed = true;
            }, UseUnscaledTime);
        }
        
        protected override NodeState OnUpdate()
        {
            if (_completed)
            {
                DebugMessage = "Done";
                return NodeState.Success;
            }
            
            float remaining = Duration - (Time.time - StartTime); // Fallback if handles don't give time
            // Timer doesn't easily expose remaining time, but we can track it
            DebugMessage = $"Waiting... {remaining:F1}s";
            
            return NodeState.Running;
        }
        
        protected override void OnStop()
        {
            // Cancel timer if node is stopped
            Timer.Cancel(_timerHandle);
            _timerHandle = TimerHandle.None;
            _completed = false;
        }
        
        public override void Abort()
        {
            OnStop();
            base.Abort();
        }
    }
}
