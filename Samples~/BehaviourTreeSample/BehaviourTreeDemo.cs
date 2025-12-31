using UnityEngine;
using Eraflo.UnityImportPackage.BehaviourTree;

namespace Eraflo.UnityImportPackage.Samples.BehaviourTree
{
    /// <summary>
    /// A simple demo script showing how to interact with the Behaviour Tree system from code.
    /// This script can be used to dynamically assign targets or control the agent's behavior.
    /// </summary>
    public class BehaviourTreeDemo : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The runner component on the agent.")]
        public BehaviourTreeRunner Runner;
        
        [Tooltip("The object the agent should move towards.")]
        public Transform Target;
        
        [Header("Blackboard Keys")]
        public string TargetKey = "MoveToTarget";
        public string SpeedKey = "MovementSpeed";

        private void Start()
        {
            if (Runner == null)
                Runner = GetComponent<BehaviourTreeRunner>();

            if (Runner == null)
            {
                Debug.LogError("[BT Demo] No BehaviourTreeRunner found!", this);
                return;
            }

            // Example: Setting a value at the start
            // runner.GetBlackboard() returns the runtime clonal blackboard
            UpdateTarget();
        }

        private void Update()
        {
            // Update the target in the blackboard every frame if it moves
            // This is useful if you don't use 'TrackTarget' in the MoveTo node
            if (Runner != null && Target != null)
            {
                UpdateTarget();
            }
        }

        public void UpdateTarget()
        {
            if (Runner != null && Target != null)
            {
                // Assigning the transform to the blackboard
                // This will be picked up by any MoveTo node using this key
                Runner.Blackboard.Set(TargetKey, Target);
            }
        }

        [ContextMenu("Increase Speed")]
        public void IncreaseSpeed()
        {
            if (Runner != null)
            {
                float currentSpeed = Runner.Blackboard.Get<float>(SpeedKey);
                Runner.Blackboard.Set(SpeedKey, currentSpeed + 1f);
                Debug.Log($"[BT Demo] Speed increased to: {currentSpeed + 1f}");
            }
        }
    }
}
