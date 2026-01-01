using UnityEngine;
using UnityEngine.AI;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Moves the agent to a target position using NavMeshAgent.
    /// Returns Running while moving, Success when arrived, Failure if path is invalid.
    /// </summary>
    [BehaviourTreeNode("Actions/Navigation", "Move To")]
    public class MoveTo : ActionNode
    {
        public enum TargetSource
        {
            Provider,
            Blackboard,
            StaticPosition,
            Tag
        }

        [Header("Target")]
        [Tooltip("Optional: Connect a Vector3 here to override the Target Source.")]
        [NodeInput] public Vector3 InputTarget;
        
        public TargetSource Source = TargetSource.Blackboard;

        [Tooltip("Target provider for the destination (used if Source is Provider).")]
        public TargetProvider Target;
        
        [Tooltip("Blackboard key for target (used if Source is Blackboard).")]
        [BlackboardKey]
        public string BlackboardKey;

        [Tooltip("Static position in world space (used if Source is StaticPosition).")]
        public Vector3 StaticPosition;

        [Tooltip("Tag of the target object (used if Source is Tag).")]
        public string TargetTag;
        
        [Header("Settings")]
        [Tooltip("How close to get before considering arrived.")]
        public float StoppingDistance = 0.5f;
        
        [Tooltip("Movement speed override. 0 = use NavMeshAgent default.")]
        public float Speed = 0f;
        
        [Tooltip("Update destination every frame (for moving targets).")]
        public bool TrackTarget = false;
        
        private NavMeshAgent _agent;
        private Vector3 _lastDestination;
        private bool _pathSet;
        private GameObject _cachedTagTarget;
        
        protected override void OnStart()
        {
            _agent = Owner?.GetComponent<NavMeshAgent>();
            _pathSet = false;
            _cachedTagTarget = null;
            
            if (_agent == null)
            {
                DebugMessage = "Error: No NavMeshAgent found";
                Debug.LogWarning("[BT] MoveTo: No NavMeshAgent found on Owner", Owner);
                return;
            }
            
            if (Speed > 0)
                _agent.speed = Speed;
            
            _agent.stoppingDistance = StoppingDistance;
            
            SetDestination();
        }
        
        protected override NodeState OnUpdate()
        {
            if (_agent == null)
                return NodeState.Failure;
            
            if (!_agent.enabled || !_agent.isOnNavMesh)
                return NodeState.Failure;
            
            // Update destination for moving targets
            if (TrackTarget)
            {
                SetDestination();
            }
            
            // Check if we have a valid path
            if (!_pathSet)
                return NodeState.Failure;
            
            // Check if path is still being calculated
            if (_agent.pathPending)
                return NodeState.Running;
            
            // Check if path is invalid
            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return NodeState.Failure;
            
            // Check if we've arrived
            if (!_agent.hasPath || _agent.remainingDistance <= StoppingDistance)
            {
                _agent.ResetPath();
                return NodeState.Success;
            }
            
            return NodeState.Running;
        }
        
        protected override void OnStop()
        {
            // Optionally stop the agent when the node completes
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
        }
        
        private void SetDestination()
        {
            Vector3 destination = GetTargetPosition();
            
            if (destination == Vector3.zero && Source != TargetSource.StaticPosition)
            {
                _pathSet = false;
                return;
            }
            
            // Only update if destination changed significantly
            if (_pathSet && Vector3.Distance(_lastDestination, destination) < 0.1f)
                return;
            
            _lastDestination = destination;
            _pathSet = _agent.SetDestination(destination);
            
            if (_pathSet)
                DebugMessage = $"Moving to {destination}";
            else
                DebugMessage = $"Error: Failed to set path to {destination}";
        }
        
        private Vector3 GetTargetPosition()
        {
            // Check Input Port override
            var port = Ports.Find(p => p.Name == "InputTarget" && p.IsInput);
            if (port != null && port.IsConnected)
            {
                return GetData<Vector3>("InputTarget");
            }

            switch (Source)
            {
                case TargetSource.Provider:
                    return Target != null ? Target.GetTargetPosition(this) : Vector3.zero;

                case TargetSource.Blackboard:
                    if (!string.IsNullOrEmpty(BlackboardKey) && Blackboard != null)
                    {
                        if (Blackboard.TryGet<Vector3>(BlackboardKey, out var pos))
                            return pos;
                        if (Blackboard.TryGet<Transform>(BlackboardKey, out var t))
                            return t != null ? t.position : Vector3.zero;
                        if (Blackboard.TryGet<GameObject>(BlackboardKey, out var go))
                            return go != null ? go.transform.position : Vector3.zero;
                    }
                    break;

                case TargetSource.StaticPosition:
                    return StaticPosition;

                case TargetSource.Tag:
                    if (!string.IsNullOrEmpty(TargetTag))
                    {
                        if (_cachedTagTarget == null)
                        {
                            _cachedTagTarget = GameObject.FindWithTag(TargetTag);
                        }
                        return _cachedTagTarget != null ? _cachedTagTarget.transform.position : Vector3.zero;
                    }
                    break;
            }
            
            return Vector3.zero;
        }
    }
}
