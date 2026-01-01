using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Provides a target from a Blackboard key.
    /// The blackboard should contain a Transform or GameObject.
    /// </summary>
    [CreateAssetMenu(menuName = "Behaviour Tree/Target Providers/Blackboard Target")]
    public class BlackboardTargetProvider : TargetProvider
    {
        [Tooltip("The blackboard key containing the target (Transform or GameObject).")]
        public string BlackboardKey = "Target";
        
        public override Transform GetTarget(Node node)
        {
            if (node.Tree?.Blackboard == null || string.IsNullOrEmpty(BlackboardKey))
                return null;
            
            var blackboard = node.Tree.Blackboard;
            
            // Try to get as Transform first
            if (blackboard.TryGet<Transform>(BlackboardKey, out var transform))
                return transform;
            
            // Try to get as GameObject
            if (blackboard.TryGet<GameObject>(BlackboardKey, out var gameObject))
                return gameObject?.transform;
            
            // Try to get as Vector3 (position only)
            if (blackboard.TryGet<Vector3>(BlackboardKey, out var position))
            {
                // For Vector3, we return null for Transform but override GetTargetPosition
                return null;
            }
            
            return null;
        }
        
        public override Vector3 GetTargetPosition(Node node)
        {
            if (node.Tree?.Blackboard == null || string.IsNullOrEmpty(BlackboardKey))
                return Vector3.zero;
            
            var blackboard = node.Tree.Blackboard;
            
            // Try Vector3 directly
            if (blackboard.TryGet<Vector3>(BlackboardKey, out var position))
                return position;
            
            // Fall back to transform position
            return base.GetTargetPosition(node);
        }
        
        public override bool HasTarget(Node node)
        {
            if (node.Tree?.Blackboard == null || string.IsNullOrEmpty(BlackboardKey))
                return false;
            
            return node.Tree.Blackboard.Contains(BlackboardKey);
        }
    }
}
