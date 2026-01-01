using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Base class for Service nodes.
    /// Services are executed at a fixed interval while their parent node is active.
    /// </summary>
    public abstract class ServiceNode : Node
    {
        /// <summary>How often this service should tick (seconds).</summary>
        public float Interval = 0.5f;

        private float _lastServiceTickTime;

        protected override void OnStart()
        {
            _lastServiceTickTime = -Interval; // Force immediate tick on start
        }

        public void TickService()
        {
            if (Time.time - _lastServiceTickTime >= Interval)
            {
                _lastServiceTickTime = Time.time;
                OnServiceUpdate();
            }
        }

        /// <summary>
        /// Called every 'Interval' seconds while the parent node is active.
        /// </summary>
        protected abstract void OnServiceUpdate();

        protected override NodeState OnUpdate()
        {
            // Services don't return a state in the same way, but inherited from Node
            return NodeState.Success;
        }

        public override Node Clone()
        {
            return Instantiate(this);
        }
    }
}
