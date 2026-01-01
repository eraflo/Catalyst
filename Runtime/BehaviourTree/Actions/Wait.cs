using UnityEngine;
using Eraflo.Catalyst.Timers;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Wait action: Waits for a specified duration using the Timer system, then returns Success.
    /// Duration can be overriden by a Data Input node.
    /// </summary>
    [BehaviourTreeNode("Actions", "Wait")]
    public class Wait : ActionNode
    {
        /// <summary>Duration to wait in seconds.</summary>
        [Tooltip("Duration to wait in seconds. Can be overriden by Input Port.")]
        [NodeInput] 
        public float Duration = 1f;
        
        /// <summary>If true, uses unscaled time (ignores Time.timeScale).</summary>
        public bool UseUnscaledTime = false;
        
        private TimerHandle _timerHandle;
        private bool _completed;
        
        protected override void OnStart()
        {
            _completed = false;
            
            // Resolve duration: Use connected input if available, else use inspector value
            float d = GetData("Duration", Duration);
            
            // Create a delay timer using the Timer system
            _timerHandle = Timer.Delay(d, () =>
            {
                _completed = true;
            }, UseUnscaledTime);
            
            DebugMessage = $"Wait {d:F1}s";
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
