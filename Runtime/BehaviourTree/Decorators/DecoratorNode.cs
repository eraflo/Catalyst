using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Base class for decorator nodes that have a single child.
    /// </summary>
    public abstract class DecoratorNode : Node
    {
        /// <summary>The child of this decorator node.</summary>
        [HideInInspector] public Node Child;
        
        /// <summary>
        /// Clones this decorator node and its child.
        /// </summary>
        public override Node Clone()
        {
            var clone = Instantiate(this);
            clone.Child = Child?.Clone();
            return clone;
        }
        
        /// <summary>
        /// Aborts this node and its child.
        /// </summary>
        public override void Abort()
        {
            Child?.Abort();
            base.Abort();
        }
    }
}
