using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Checks if target is within a specified range.
    /// Returns Success if in range, Failure otherwise.
    /// </summary>
    [BehaviourTreeNode("Conditions", "Is In Range")]
    public class IsInRange : ConditionNode
    {
        [Header("Target")]
        [Tooltip("Target provider for the target to check.")]
        public TargetProvider Target;
        
        [Tooltip("Alternative: Blackboard key for target position/transform.")]
        [BlackboardKey]
        public string BlackboardKey;
        
        [Header("Settings")]
        [Tooltip("Maximum distance to consider 'in range'.")]
        public float Range = 10f;
        
        [Tooltip("Use 2D distance (ignore Y axis).")]
        public bool Use2DDistance = false;
        
        protected override bool CheckCondition()
        {
            if (Owner == null)
                return false;
            
            Vector3 myPos = Owner.transform.position;
            Vector3 targetPos = GetTargetPosition();
            
            if (targetPos == Vector3.zero && Target == null && string.IsNullOrEmpty(BlackboardKey))
                return false;
            
            float distance;
            if (Use2DDistance)
            {
                var diff = targetPos - myPos;
                diff.y = 0;
                distance = diff.magnitude;
            }
            else
            {
                distance = Vector3.Distance(myPos, targetPos);
            }
            
            return distance <= Range;
        }
        
        private Vector3 GetTargetPosition()
        {
            // Priority 1: TargetProvider
            if (Target != null)
                return Target.GetTargetPosition(this);
            
            // Priority 2: Blackboard key
            if (!string.IsNullOrEmpty(BlackboardKey) && Blackboard != null)
            {
                if (Blackboard.TryGet<Vector3>(BlackboardKey, out var pos))
                    return pos;
                if (Blackboard.TryGet<Transform>(BlackboardKey, out var t))
                    return t != null ? t.position : Vector3.zero;
                if (Blackboard.TryGet<GameObject>(BlackboardKey, out var go))
                    return go != null ? go.transform.position : Vector3.zero;
            }
            
            return Vector3.zero;
        }
    }
}
