using UnityEngine;
using Eraflo.UnityImportPackage.Timers;

namespace Eraflo.UnityImportPackage.BehaviourTree
{
    /// <summary>
    /// Wait action: Waits for a specified duration using the Timer system, then returns Success.
    /// </summary>
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
                return NodeState.Success;
            }
            
            return NodeState.Running;
        }
        
        protected override void OnStop()
        {
            // Cancel timer if node is stopped early
            if (!_completed && _timerHandle != TimerHandle.None)
            {
                Timer.Cancel(_timerHandle);
            }
            _timerHandle = TimerHandle.None;
        }
        
        public override void Abort()
        {
            OnStop();
            base.Abort();
        }
    }
}
