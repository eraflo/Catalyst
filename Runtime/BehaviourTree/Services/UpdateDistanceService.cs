using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Service that updates the distance to a target in the Blackboard.
    /// </summary>
    [BehaviourTreeNode("Services", "Update Distance")]
    public class UpdateDistanceService : ServiceNode
    {
        [BlackboardKey]
        public string TargetKey = "Target";
        
        [BlackboardKey]
        public string DistanceKey = "Distance";

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
                Blackboard.Set(DistanceKey, distance);
                DebugMessage = $"Distance: {distance:F1}m";
            }
            else
            {
                DebugMessage = "No target";
            }
        }
    }
}
