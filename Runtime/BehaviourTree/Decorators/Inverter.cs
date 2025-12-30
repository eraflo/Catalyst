using UnityEngine;

namespace Eraflo.UnityImportPackage.BehaviourTree
{
    /// <summary>
    /// Inverter decorator: Inverts the result of its child.
    /// Success becomes Failure, Failure becomes Success.
    /// </summary>
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
