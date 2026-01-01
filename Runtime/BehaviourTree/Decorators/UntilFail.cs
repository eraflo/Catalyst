using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// UntilFail decorator: Repeats its child until it fails.
    /// Returns Success when the child finally fails.
    /// </summary>
    [BehaviourTreeNode("Decorators", "Until Fail")]
    public class UntilFail : DecoratorNode
    {
        protected override NodeState OnUpdate()
        {
            if (Child == null) return NodeState.Failure;
            
            var state = Child.Evaluate();
            
            switch (state)
            {
                case NodeState.Running:
                    return NodeState.Running;
                
                case NodeState.Success:
                    // Reset child and continue
                    Child.Started = false;
                    return NodeState.Running;
                
                case NodeState.Failure:
                    // Child failed, we're done
                    return NodeState.Success;
            }
            
            return NodeState.Running;
        }
    }
}
