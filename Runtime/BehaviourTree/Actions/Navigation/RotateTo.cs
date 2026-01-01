using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Rotates the agent to face a target.
    /// Returns Running while rotating, Success when facing target.
    /// </summary>
    [BehaviourTreeNode("Actions/Navigation", "Rotate To")]
    public class RotateTo : ActionNode
    {
        [Header("Target")]
        [Tooltip("Target provider for the look-at direction.")]
        public TargetProvider Target;
        
        [Tooltip("Alternative: Blackboard key for target position/transform.")]
        [BlackboardKey]
        public string BlackboardKey;
        
        [Header("Settings")]
        [Tooltip("Rotation speed in degrees per second.")]
        public float RotationSpeed = 360f;
        
        [Tooltip("Angle tolerance in degrees to consider facing complete.")]
        public float AngleTolerance = 5f;
        
        [Tooltip("Only rotate on the Y axis (typical for ground agents).")]
        public bool YAxisOnly = true;
        
        private Transform _ownerTransform;
        
        protected override void OnStart()
        {
            _ownerTransform = Owner?.transform;
        }
        
        protected override NodeState OnUpdate()
        {
            if (_ownerTransform == null)
                return NodeState.Failure;
            
            Vector3 targetPos = GetTargetPosition();
            if (targetPos == Vector3.zero)
                return NodeState.Failure;
            
            Vector3 direction = targetPos - _ownerTransform.position;
            
            if (YAxisOnly)
                direction.y = 0;
            
            if (direction.sqrMagnitude < 0.001f)
                return NodeState.Success;
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float angle = Quaternion.Angle(_ownerTransform.rotation, targetRotation);
            
            if (angle <= AngleTolerance)
            {
                _ownerTransform.rotation = targetRotation;
                return NodeState.Success;
            }
            
            float step = RotationSpeed * Time.deltaTime;
            _ownerTransform.rotation = Quaternion.RotateTowards(
                _ownerTransform.rotation, 
                targetRotation, 
                step
            );
            
            return NodeState.Running;
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
