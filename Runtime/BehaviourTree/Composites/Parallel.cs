using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Parallel node: Runs all children simultaneously.
    /// Configurable success/failure policy.
    /// </summary>
    [BehaviourTreeNode("Composites", "Parallel")]
    public class Parallel : CompositeNode
    {
        /// <summary>Policy for determining when the parallel node succeeds.</summary>
        public Policy SuccessPolicy = Policy.RequireAll;
        
        /// <summary>Policy for determining when the parallel node fails.</summary>
        public Policy FailurePolicy = Policy.RequireOne;
        
        public enum Policy
        {
            /// <summary>Requires all children to meet the condition.</summary>
            RequireAll,
            
            /// <summary>Requires at least one child to meet the condition.</summary>
            RequireOne
        }
        
        protected override NodeState OnUpdate()
        {
            int successCount = 0;
            int failureCount = 0;
            int runningCount = 0;
            
            foreach (var child in Children)
            {
                var state = child.Evaluate();
                
                switch (state)
                {
                    case NodeState.Success:
                        successCount++;
                        break;
                    case NodeState.Failure:
                        failureCount++;
                        break;
                    case NodeState.Running:
                        runningCount++;
                        break;
                }
            }
            
            // Check failure policy
            if (FailurePolicy == Policy.RequireOne && failureCount > 0)
            {
                AbortRunningChildren();
                return NodeState.Failure;
            }
            
            if (FailurePolicy == Policy.RequireAll && failureCount == Children.Count)
            {
                return NodeState.Failure;
            }
            
            // Check success policy
            if (SuccessPolicy == Policy.RequireOne && successCount > 0)
            {
                AbortRunningChildren();
                return NodeState.Success;
            }
            
            if (SuccessPolicy == Policy.RequireAll && successCount == Children.Count)
            {
                return NodeState.Success;
            }
            
            // Still running if any child is running
            if (runningCount > 0)
            {
                return NodeState.Running;
            }
            
            // Default: all children completed
            return NodeState.Success;
        }
        
        private void AbortRunningChildren()
        {
            foreach (var child in Children)
            {
                if (child.State == NodeState.Running)
                {
                    child.Abort();
                }
            }
        }
    }
}
