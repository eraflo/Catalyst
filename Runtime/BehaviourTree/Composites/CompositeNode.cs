using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Base class for composite nodes that have multiple children.
    /// </summary>
    public abstract class CompositeNode : Node
    {
        /// <summary>The children of this composite node.</summary>
        [HideInInspector] public List<Node> Children = new();
        
        /// <summary>The index of the currently executing child.</summary>
        protected int CurrentChildIndex;
        
        protected override void OnStart()
        {
            CurrentChildIndex = 0;
        }
        
        /// <summary>
        /// Clones this composite node and all its children.
        /// </summary>
        public override Node Clone()
        {
            var clone = Instantiate(this);
            clone.Children = new List<Node>();
            
            foreach (var child in Children)
            {
                if (child != null)
                {
                    clone.Children.Add(child.Clone());
                }
            }
            
            return clone;
        }
        
        /// <summary>
        /// Aborts this node and all its children.
        /// </summary>
        public override void Abort()
        {
            foreach (var child in Children)
            {
                child?.Abort();
            }
            base.Abort();
        }
        /// <summary>
        /// Sorts the children nodes based on their visual X position.
        /// This ensures visual order (left-to-right) matches execution order.
        /// </summary>
        public void SortChildrenByPosition()
        {
            Children.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                return a.Position.x.CompareTo(b.Position.x);
            });
        }
    }
}
