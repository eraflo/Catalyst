using UnityEngine;
using Eraflo.Catalyst.Timers;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Cooldown decorator: Prevents child execution until cooldown expires.
    /// Uses the Timer system for accurate timing.
    /// </summary>
    [BehaviourTreeNode("Decorators", "Cooldown")]
    public class Cooldown : DecoratorNode
    {
        /// <summary>Cooldown duration in seconds.</summary>
        [Tooltip("Cooldown duration in seconds.")]
        public float Duration = 1f;
        
        /// <summary>Whether to start on cooldown.</summary>
        public bool StartOnCooldown = false;
        
        /// <summary>What to return when on cooldown.</summary>
        public NodeState CooldownReturnState = NodeState.Failure;
        
        /// <summary>If true, uses unscaled time (ignores Time.timeScale).</summary>
        public bool UseUnscaledTime = false;
        
        private bool _isOnCooldown;
        private bool _childRunning;
        private TimerHandle _cooldownTimer;
        
        protected override void OnStart()
        {
            _childRunning = false;
            
            if (StartOnCooldown)
            {
                StartCooldown();
            }
        }
        
        protected override NodeState OnUpdate()
        {
            if (Child == null) return NodeState.Failure;
            
            // If child is running, let it continue
            if (_childRunning)
            {
                var state = Child.Evaluate();
                
                if (state != NodeState.Running)
                {
                    _childRunning = false;
                    StartCooldown();
                    return state;
                }
                
                return NodeState.Running;
            }
            
            // Check cooldown
            if (_isOnCooldown)
            {
                return CooldownReturnState;
            }
            
            // Execute child
            var childState = Child.Evaluate();
            
            if (childState == NodeState.Running)
            {
                _childRunning = true;
                return NodeState.Running;
            }
            
            // Child completed, start cooldown
            StartCooldown();
            return childState;
        }
        
        private void StartCooldown()
        {
            _isOnCooldown = true;
            
            // Cancel any existing cooldown timer
            if (_cooldownTimer != TimerHandle.None)
            {
                Timer.Cancel(_cooldownTimer);
            }
            
            // Start new cooldown timer
            _cooldownTimer = Timer.Delay(Duration, () =>
            {
                _isOnCooldown = false;
            }, UseUnscaledTime);
        }
        
        protected override void OnStop()
        {
            // Cancel cooldown timer when node is stopped
            if (_cooldownTimer != TimerHandle.None)
            {
                Timer.Cancel(_cooldownTimer);
                _cooldownTimer = TimerHandle.None;
            }
        }
        
        public override void Abort()
        {
            OnStop();
            Child?.Abort();
            base.Abort();
        }
    }
}
