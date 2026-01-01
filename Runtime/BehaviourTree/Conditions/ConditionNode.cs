namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Base class for condition nodes that return Success or Failure (never Running).
    /// </summary>
    public abstract class ConditionNode : Node
    {
        /// <summary>
        /// Override this to implement the condition check.
        /// </summary>
        /// <returns>True if the condition is met.</returns>
        protected abstract bool CheckCondition();
        
        protected override NodeState OnUpdate()
        {
            return CheckCondition() ? NodeState.Success : NodeState.Failure;
        }
    }
}
