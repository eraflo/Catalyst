using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Checks if there is a clear line of sight to the target.
    /// Returns Success if visible, Failure if obstructed.
    /// </summary>
    [BehaviourTreeNode("Conditions", "Has Line Of Sight")]
    public class HasLineOfSight : ConditionNode
    {
        [Header("Target")]
        [Tooltip("Optional: Connect a Vector3 here to override the Target Source.")]
        [NodeInput] public Vector3 InputTarget;
        
        [Tooltip("Target provider for the target to check.")]
        public TargetProvider Target;
        
        [Tooltip("Alternative: Blackboard key for target position/transform.")]
        [BlackboardKey]
        public string BlackboardKey;
        
        [Header("Raycast Settings")]
        [Tooltip("Offset from owner's position for ray origin (e.g., eye height).")]
        public Vector3 RayOriginOffset = new Vector3(0, 1.5f, 0);
        
        [Tooltip("Offset from target's position for ray destination.")]
        public Vector3 RayTargetOffset = new Vector3(0, 1f, 0);
        
        [Tooltip("LayerMask for obstacles. Default = Everything.")]
        public LayerMask ObstacleLayer = ~0;
        
        [Tooltip("Maximum raycast distance.")]
        public float MaxDistance = 50f;
        
        [Tooltip("Draw debug ray in Scene view.")]
        public bool DebugDraw = false;
        
        protected override bool CheckCondition()
        {
            if (Owner == null)
                return false;
            
            Vector3 origin = Owner.transform.position + RayOriginOffset;
            Vector3 targetPos = GetTargetPosition();
            
            if (targetPos == Vector3.zero && Target == null && string.IsNullOrEmpty(BlackboardKey))
                return false;
            
            targetPos += RayTargetOffset;
            
            Vector3 direction = targetPos - origin;
            float distance = Mathf.Min(direction.magnitude, MaxDistance);
            
            if (distance <= 0.01f)
                return true; // Basically at the same position
            
            if (DebugDraw)
            {
                Debug.DrawRay(origin, direction.normalized * distance, Color.yellow, 0.1f);
            }
            
            // If raycast hits something, LOS is blocked
            if (Physics.Raycast(origin, direction.normalized, out var hit, distance, ObstacleLayer))
            {
                // Check if what we hit IS the target
                Transform targetTransform = GetTargetTransform();
                if (targetTransform != null && hit.transform == targetTransform)
                    return true;
                
                // We hit something else - LOS blocked
                if (DebugDraw)
                {
                    Debug.DrawLine(origin, hit.point, Color.red, 0.1f);
                }
                return false;
            }
            
            // Nothing hit, clear LOS
            if (DebugDraw)
            {
                Debug.DrawLine(origin, targetPos, Color.green, 0.1f);
            }
            return true;
        }
        
        private Vector3 GetTargetPosition()
        {
            // Check Input Port override
            var port = Ports.Find(p => p.Name == "InputTarget" && p.IsInput);
            if (port != null && port.IsConnected)
            {
                return GetData<Vector3>("InputTarget");
            }

            if (Target != null)
                return Target.GetTargetPosition(this);
            
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
        
        private Transform GetTargetTransform()
        {
            if (Target != null)
                return Target.GetTarget(this);
            
            if (!string.IsNullOrEmpty(BlackboardKey) && Blackboard != null)
            {
                if (Blackboard.TryGet<Transform>(BlackboardKey, out var t))
                    return t;
                if (Blackboard.TryGet<GameObject>(BlackboardKey, out var go))
                    return go?.transform;
            }
            
            return null;
        }
    }
}
