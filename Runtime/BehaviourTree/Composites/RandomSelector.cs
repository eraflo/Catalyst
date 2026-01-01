using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// RandomSelector node: Shuffles children before executing, then behaves like a Selector.
    /// Useful for adding variety to AI behavior.
    /// </summary>
    [BehaviourTreeNode("Composites", "Random Selector")]
    public class RandomSelector : CompositeNode
    {
        private List<int> _shuffledIndices;
        
        protected override void OnStart()
        {
            base.OnStart();
            
            // Create and shuffle indices
            _shuffledIndices = new List<int>();
            for (int i = 0; i < Children.Count; i++)
            {
                _shuffledIndices.Add(i);
            }
            
            // Fisher-Yates shuffle
            for (int i = _shuffledIndices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_shuffledIndices[i], _shuffledIndices[j]) = (_shuffledIndices[j], _shuffledIndices[i]);
            }
        }
        
        protected override NodeState OnUpdate()
        {
            while (CurrentChildIndex < _shuffledIndices.Count)
            {
                var child = Children[_shuffledIndices[CurrentChildIndex]];
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
