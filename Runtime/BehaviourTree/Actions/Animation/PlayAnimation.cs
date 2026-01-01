using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Plays an animation state on the Animator.
    /// Returns Success immediately after triggering, or Running if WaitForCompletion is true.
    /// </summary>
    [BehaviourTreeNode("Actions/Animation", "Play Animation")]
    public class PlayAnimation : ActionNode
    {
        [Tooltip("Name of the animation state to play.")]
        public string StateName;
        
        [Tooltip("Layer index (-1 for default).")]
        public int Layer = -1;
        
        [Tooltip("Normalized time to start at (0-1).")]
        [Range(0f, 1f)]
        public float NormalizedTime = 0f;
        
        [Tooltip("Wait for animation to complete before returning Success.")]
        public bool WaitForCompletion = false;
        
        [Tooltip("Crossfade duration (0 = instant).")]
        public float CrossfadeDuration = 0.1f;
        
        private Animator _animator;
        private int _stateHash;
        private bool _started;
        
        protected override void OnStart()
        {
            _animator = Owner?.GetComponent<Animator>();
            _stateHash = Animator.StringToHash(StateName);
            _started = false;
            
            if (_animator == null)
            {
                Debug.LogWarning("[BT] PlayAnimation: No Animator found on Owner", Owner);
                return;
            }
            
            if (CrossfadeDuration > 0)
            {
                _animator.CrossFade(_stateHash, CrossfadeDuration, Layer, NormalizedTime);
            }
            else
            {
                _animator.Play(_stateHash, Layer, NormalizedTime);
            }
        }
        
        protected override NodeState OnUpdate()
        {
            if (_animator == null || string.IsNullOrEmpty(StateName))
                return NodeState.Failure;
            
            if (!WaitForCompletion)
                return NodeState.Success;
            
            // Wait one frame for animation to start
            if (!_started)
            {
                _started = true;
                return NodeState.Running;
            }
            
            // Check if animation is complete
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(Layer >= 0 ? Layer : 0);
            
            if (stateInfo.shortNameHash == _stateHash || stateInfo.fullPathHash == _stateHash)
            {
                if (stateInfo.normalizedTime >= 1f)
                    return NodeState.Success;
                
                return NodeState.Running;
            }
            
            // Animation changed, consider it done
            return NodeState.Success;
        }
    }
}
