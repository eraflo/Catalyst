using UnityEngine;

namespace Eraflo.UnityImportPackage.BehaviourTree
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
            while (CurrentChildIndex < Children.Count)
            {
                var child = Children[CurrentChildIndex];
                if (child == null)
                {
                    CurrentChildIndex++;
                    continue;
                }
                var state = child.Evaluate();
                
                switch (state)
                {
                    case NodeState.Running:
                        return NodeState.Running;
                    
                    case NodeState.Success:
                        return NodeState.Success;
                    
                    case NodeState.Failure:
                        CurrentChildIndex++;
                        break;
                }
            }
            
            // All children failed
            return NodeState.Failure;
        }
    }
}
