using UnityEngine;
using Eraflo.Catalyst.Core.Blackboard;

namespace Eraflo.Catalyst.BehaviourTree
{
    [BehaviourTreeNode("Services", "Find Target")]
    public class FindTargetService : ServiceNode
    {
        public string Tag = "Player";

        [BlackboardKey]
        public string TargetKey = "Target";

        private GameObject _cachedTarget;

        protected override void OnServiceUpdate()
        {
            // Invalidate cache if the target has been deactivated or destroyed
            if (_cachedTarget != null && !_cachedTarget.activeInHierarchy)
                _cachedTarget = null;

            // Only search when the cache is empty
            if (_cachedTarget == null)
                _cachedTarget = GameObject.FindWithTag(Tag);

            var target = _cachedTarget;
            if (target != null)
            {
                Blackboard.Set(TargetKey, target);
                DebugMessage = $"Found {target.name}";
            }
            else
            {
                DebugMessage = $"No target with tag {Tag}";
            }
        }
    }
}
