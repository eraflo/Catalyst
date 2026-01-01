using UnityEngine;
using Eraflo.Catalyst.Events;
using Eraflo.Catalyst.Timers;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Waits until an EventChannel is raised.
    /// Returns Running while waiting, Success when event fires.
    /// </summary>
    [BehaviourTreeNode("Actions", "Wait For Event")]
    public class WaitForEvent : ActionNode
    {
        [Tooltip("The event channel to wait for.")]
        public EventChannel Channel;
        
        [Tooltip("Maximum time to wait (0 = infinite).")]
        public float Timeout = 0f;
        
        private bool _eventReceived;
        private bool _timedOut;
        private TimerHandle _timeoutHandle;
        
        protected override void OnStart()
        {
            _eventReceived = false;
            _timedOut = false;
            
            if (Channel != null)
            {
                Channel.Subscribe(OnEventRaised);
            }
            
            if (Timeout > 0)
            {
                _timeoutHandle = Timer.Delay(Timeout, () => _timedOut = true);
            }
        }
        
        protected override NodeState OnUpdate()
        {
            if (Channel == null)
            {
                Debug.LogWarning("[BT] WaitForEvent: No Channel assigned", Owner);
                return NodeState.Failure;
            }
            
            if (_eventReceived)
                return NodeState.Success;
            
            if (_timedOut)
                return NodeState.Failure;
            
            return NodeState.Running;
        }
        
        protected override void OnStop()
        {
            if (Channel != null)
            {
                Channel.Unsubscribe(OnEventRaised);
            }
            
            if (_timeoutHandle != TimerHandle.None)
            {
                Timer.Cancel(_timeoutHandle);
                _timeoutHandle = TimerHandle.None;
            }
        }
        
        private void OnEventRaised()
        {
            _eventReceived = true;
        }
    }
}
