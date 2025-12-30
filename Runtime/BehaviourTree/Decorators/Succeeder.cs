using UnityEngine;

namespace Eraflo.UnityImportPackage.BehaviourTree
{
    /// <summary>
    /// Succeeder decorator: Always returns Success, regardless of child result.
    /// Useful for optional tasks that shouldn't fail the parent sequence.
    /// </summary>
    public class Succeeder : DecoratorNode
    {
        protected override NodeState OnUpdate()
        {
            if (Child == null) return NodeState.Success;
            
            var state = Child.Evaluate();
            
            if (state == NodeState.Running)
            {
                return NodeState.Running;
            }
            
            // Always succeed, regardless of child result
            return NodeState.Success;
        }
    }
}
