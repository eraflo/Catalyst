using UnityEngine;

namespace Eraflo.UnityImportPackage.BehaviourTree
{
    /// <summary>
    /// Sequence (AND) node: Returns Failure when the first child fails.
    /// Runs each child in order, all must succeed for the sequence to succeed.
    /// </summary>
    public class Sequence : CompositeNode
    {
        protected override NodeState OnUpdate()
        {
            while (CurrentChildIndex < Children.Count)
            {
                var child = Children[CurrentChildIndex];
                var state = child.Evaluate();
                
                switch (state)
                {
                    case NodeState.Running:
                        return NodeState.Running;
                    
                    case NodeState.Failure:
                        return NodeState.Failure;
                    
                    case NodeState.Success:
                        CurrentChildIndex++;
                        break;
                }
            }
            
            // All children succeeded
            return NodeState.Success;
        }
    }
}
