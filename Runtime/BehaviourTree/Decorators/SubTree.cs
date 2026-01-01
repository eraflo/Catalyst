using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Executes another BehaviourTree as a subtree.
    /// Allows for modular, reusable behavior compositions.
    /// </summary>
    [BehaviourTreeNode("Decorators", "Sub Tree")]
    public class SubTree : DecoratorNode
    {
        [Tooltip("The BehaviourTree asset to execute as a subtree.")]
        public BehaviourTree SubTreeAsset;
        
        private BehaviourTree _runtimeTree;
        
        protected override void OnStart()
        {
            if (SubTreeAsset == null)
            {
                Debug.LogWarning("[BT] SubTree: No SubTreeAsset assigned", Tree?.Owner);
                return;
            }
            
            // Clone the tree for runtime use
            _runtimeTree = SubTreeAsset.Clone();
            
            // Share the same owner and blackboard
            if (_runtimeTree != null)
            {
                _runtimeTree.Bind(Tree?.Owner);
                // Share parent's blackboard
                _runtimeTree.Blackboard = Tree?.Blackboard;
            }
        }
        
        protected override NodeState OnUpdate()
        {
            if (_runtimeTree == null || _runtimeTree.RootNode == null)
                return NodeState.Failure;
            
            return _runtimeTree.Evaluate();
        }
        
        protected override void OnStop()
        {
            // Reset runtime tree when stopped
            if (_runtimeTree != null)
            {
                _runtimeTree.Reset();
            }
        }
        
        public override Node Clone()
        {
            var clone = (SubTree)base.Clone();
            // Don't clone the runtime tree, just the reference to the asset
            clone._runtimeTree = null;
            return clone;
        }
    }
}
