using UnityEngine;
using UnityEngine.Events;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Invokes a UnityEvent.
    /// Returns Success immediately after invoking.
    /// </summary>
    [BehaviourTreeNode("Actions", "Run Unity Event")]
    public class RunUnityEvent : ActionNode
    {
        [Tooltip("The UnityEvent to invoke.")]
        public UnityEvent Event;
        
        protected override NodeState OnUpdate()
        {
            if (Event == null)
            {
                Debug.LogWarning("[BT] RunUnityEvent: No Event assigned", Owner);
                return NodeState.Failure;
            }
            
            Event.Invoke();
            return NodeState.Success;
        }
    }
}
