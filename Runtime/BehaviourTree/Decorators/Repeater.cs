using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Repeater decorator: Repeats its child a specified number of times.
    /// Set repeatCount to 0 for infinite repetition.
    /// </summary>
    [BehaviourTreeNode("Decorators", "Repeater")]
    public class Repeater : DecoratorNode
    {
        /// <summary>Number of times to repeat. 0 = infinite.</summary>
        [Tooltip("Number of times to repeat. 0 = infinite.")]
        public int RepeatCount = 1;
        
        /// <summary>Whether to stop repeating if the child fails.</summary>
        public bool StopOnFailure = false;
        
        private int _currentCount;
        
        protected override void OnStart()
        {
            _currentCount = 0;
        }
        
        protected override NodeState OnUpdate()
        {
            if (Child == null) return NodeState.Failure;
            
            var state = Child.Evaluate();
            
            switch (state)
            {
                case NodeState.Running:
                    return NodeState.Running;
                
                case NodeState.Failure:
                    if (StopOnFailure)
                    {
                        return NodeState.Failure;
                    }
                    _currentCount++;
                    Child.Started = false;
                    break;
                
                case NodeState.Success:
                    _currentCount++;
                    Child.Started = false;
                    break;
            }
            
            // Check if we should continue repeating
            if (RepeatCount == 0)
            {
                // Infinite loop
                return NodeState.Running;
            }
            
            if (_currentCount >= RepeatCount)
            {
                return NodeState.Success;
            }
            
            return NodeState.Running;
        }
    }
}
