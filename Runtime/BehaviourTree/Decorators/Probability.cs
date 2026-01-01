using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Executes child with a random probability.
    /// Returns Failure if the random check fails, otherwise passes through child result.
    /// </summary>
    [BehaviourTreeNode("Decorators", "Probability")]
    public class Probability : DecoratorNode
    {
        [Tooltip("Probability of executing child (0-1).")]
        [Range(0f, 1f)]
        public float Chance = 0.5f;
        
        private bool _shouldExecute;
        private bool _checked;
        
        protected override void OnStart()
        {
            _checked = false;
            _shouldExecute = false;
        }
        
        protected override NodeState OnUpdate()
        {
            if (!_checked)
            {
                _checked = true;
                _shouldExecute = Random.value <= Chance;
            }
            
            if (!_shouldExecute)
                return NodeState.Failure;
            
            if (Child == null)
                return NodeState.Success;
            
            return Child.Evaluate();
        }
    }
}
