using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Abstract base for providing targets to BT nodes.
    /// Avoids using tags/layers by using explicit references or blackboard keys.
    /// </summary>
    public abstract class TargetProvider : ScriptableObject
    {
        /// <summary>
        /// Gets the target transform.
        /// </summary>
        /// <param name="node">The node requesting the target.</param>
        /// <returns>The target transform, or null if not found.</returns>
        public abstract Transform GetTarget(Node node);
        
        /// <summary>
        /// Gets the target position.
        /// </summary>
        /// <param name="node">The node requesting the target.</param>
        /// <returns>The target position.</returns>
        public virtual Vector3 GetTargetPosition(Node node)
        {
            var target = GetTarget(node);
            return target != null ? target.position : Vector3.zero;
        }
        
        /// <summary>
        /// Checks if a valid target exists.
        /// </summary>
        /// <param name="node">The node requesting the target.</param>
        /// <returns>True if a valid target exists.</returns>
        public virtual bool HasTarget(Node node)
        {
            return GetTarget(node) != null;
        }
    }
}
