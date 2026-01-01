using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Service that checks if a target is within range and updates a Blackboard flag.
    /// </summary>
    [BehaviourTreeNode("Services", "Check Range")]
    public class CheckRangeService : ServiceNode
    {
        [BlackboardKey]
        public string TargetKey = "Target";
        
        [BlackboardKey]
        public string InRangeKey = "InRange";
        
        public float Range = 5f;

        protected override void OnServiceUpdate()
        {
            if (Owner == null || Blackboard == null) return;

            Vector3 targetPos = Vector3.zero;
            bool hasTarget = false;

            if (Blackboard.TryGet<Transform>(TargetKey, out Transform trans) && trans != null)
            {
                targetPos = trans.position;
                hasTarget = true;
            }
            else if (Blackboard.TryGet<GameObject>(TargetKey, out GameObject go) && go != null)
            {
                targetPos = go.transform.position;
                hasTarget = true;
            }
            else if (Blackboard.TryGet<Vector3>(TargetKey, out Vector3 pos))
            {
                targetPos = pos;
                hasTarget = true;
            }

            if (hasTarget)
            {
                float distance = Vector3.Distance(Owner.transform.position, targetPos);
                bool inRange = distance <= Range;
                Blackboard.Set(InRangeKey, inRange);
                DebugMessage = inRange ? $"In range ({distance:F1}m)" : $"Out of range ({distance:F1}m)";
            }
            else
            {
                Blackboard.Set(InRangeKey, false);
                DebugMessage = "No target";
            }
        }
    }
}
