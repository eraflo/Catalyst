using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Selector (OR) node: Returns Success when the first child succeeds.
    /// Tries each child in order until one succeeds.
    /// </summary>
    [BehaviourTreeNode("Composites", "Selector")]
    public class Selector : CompositeNode
    {
        protected override NodeState OnUpdate()
        {
            for (int i = CurrentChildIndex; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child == null) continue;

                var state = child.Evaluate();
                
                switch (state)
                {
                    case NodeState.Running:
                        CurrentChildIndex = i;
                        return NodeState.Running;
                    
                    case NodeState.Success:
                        return NodeState.Success;
                    
                    case NodeState.Failure:
                        // Continue to next child
                        break;
                }
            }
            
            // All children failed
            return NodeState.Failure;
        }
    }
}
