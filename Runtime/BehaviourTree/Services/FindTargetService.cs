using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    [BehaviourTreeNode("Services", "Find Target")]
    public class FindTargetService : ServiceNode
    {
        public string Tag = "Player";
        
        [BlackboardKey]
        public string TargetKey = "Target";

        protected override void OnServiceUpdate()
        {
            var target = GameObject.FindWithTag(Tag);
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
