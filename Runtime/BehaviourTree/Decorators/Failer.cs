namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Always returns Failure after executing its child (regardless of child result).
    /// Useful in Parallel nodes to ensure a branch always fails.
    /// </summary>
    [BehaviourTreeNode("Decorators", "Failer")]
    public class Failer : DecoratorNode
    {
        protected override NodeState OnUpdate()
        {
            if (Child == null)
                return NodeState.Failure;
            
            var state = Child.Evaluate();
            
            // If child is still running, keep running
            if (state == NodeState.Running)
                return NodeState.Running;
            
            // Always return Failure when complete
            return NodeState.Failure;
        }
    }
}
