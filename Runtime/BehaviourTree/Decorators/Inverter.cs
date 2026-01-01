using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Inverter decorator: Inverts the result of its child.
    /// Success becomes Failure, Failure becomes Success.
    /// </summary>
    [BehaviourTreeNode("Decorators", "Inverter")]
    public class Inverter : DecoratorNode
    {
        protected override NodeState OnUpdate()
        {
            if (Child == null) return NodeState.Failure;
            
            var state = Child.Evaluate();
            
            return state switch
            {
                NodeState.Running => NodeState.Running,
                NodeState.Success => NodeState.Failure,
                NodeState.Failure => NodeState.Success,
                _ => NodeState.Failure
            };
        }
    }
}
