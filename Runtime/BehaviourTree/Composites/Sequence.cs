using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Sequence (AND) node: Returns Failure when the first child fails.
    /// Runs each child in order, all must succeed for the sequence to succeed.
    /// </summary>
    [BehaviourTreeNode("Composites", "Sequence")]
    public class Sequence : CompositeNode
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
                    
                    case NodeState.Failure:
                        return NodeState.Failure;
                    
                    case NodeState.Success:
                        // Continue to next child
                        break;
                }
            }
            
            // All children succeeded
            return NodeState.Success;
        }
    }
}
