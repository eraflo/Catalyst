using UnityEngine;
using Eraflo.Catalyst.Timers;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Aborts the child if it takes too long.
    /// Returns Failure if timeout is reached, otherwise passes through child result.
    /// </summary>
    [BehaviourTreeNode("Decorators", "Time Limit")]
    public class TimeLimit : DecoratorNode
    {
        [Tooltip("Maximum time in seconds before aborting.")]
        public float Duration = 5f;
        
        private bool _timedOut;
        private TimerHandle _timerHandle;
        
        protected override void OnStart()
        {
            _timedOut = false;
            _timerHandle = Timer.Delay(Duration, () => _timedOut = true);
        }
        
        protected override NodeState OnUpdate()
        {
            if (Child == null)
                return NodeState.Failure;
            
            if (_timedOut)
            {
                Child.Abort();
                return NodeState.Failure;
            }
            
            var state = Child.Evaluate();
            
            // If child completed before timeout, cancel timer
            if (state != NodeState.Running && _timerHandle != TimerHandle.None)
            {
                Timer.Cancel(_timerHandle);
                _timerHandle = TimerHandle.None;
            }
            
            return state;
        }
        
        protected override void OnStop()
        {
            if (_timerHandle != TimerHandle.None)
            {
                Timer.Cancel(_timerHandle);
                _timerHandle = TimerHandle.None;
            }
        }
    }
}
