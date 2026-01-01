using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// MonoBehaviour that runs a BehaviourTree on a GameObject.
    /// Clones the tree at runtime to allow multiple instances.
    /// </summary>
    public class BehaviourTreeRunner : MonoBehaviour
    {
        /// <summary>The behaviour tree asset to run.</summary>
        [SerializeField] private BehaviourTree _tree;
        
        /// <summary>When to update the tree.</summary>
        [SerializeField] private UpdateMode _updateMode = UpdateMode.Update;
        
        /// <summary>Whether to restart the tree when it completes.</summary>
        [SerializeField] private bool _restartOnComplete = false;
        
        /// <summary>Tick rate when using FixedUpdate or throttled Update (ticks per second).</summary>
        [SerializeField] private float _tickRate = 10f;
        
        private BehaviourTree _runtimeTree;
        private float _lastTickTime;
        
        /// <summary>The runtime instance of the behaviour tree.</summary>
        public BehaviourTree RuntimeTree => _runtimeTree;
        
        /// <summary>The blackboard of the runtime tree.</summary>
        public Blackboard Blackboard => _runtimeTree?.Blackboard;
        
        /// <summary>Current state of the tree.</summary>
        public NodeState TreeState => _runtimeTree?.TreeState ?? NodeState.Failure;
        
        public enum UpdateMode
        {
            /// <summary>Update every frame.</summary>
            Update,
            
            /// <summary>Update in FixedUpdate.</summary>
            FixedUpdate,
            
            /// <summary>Update at a fixed tick rate.</summary>
            Throttled,
            
            /// <summary>Manual update only via Tick().</summary>
            Manual
        }
        
        private void Awake()
        {
            if (_tree != null)
            {
                _runtimeTree = _tree.Clone();
                _runtimeTree.Bind(gameObject);
            }
        }
        
        private void Update()
        {
            if (_runtimeTree == null) return;

            // Reactive Abort check
            if (_runtimeTree.IsAbortRequested())
            {
                Tick();
                return;
            }

            if (_updateMode == UpdateMode.Update)
            {
                Tick();
            }
            else if (_updateMode == UpdateMode.Throttled)
            {
                float interval = 1f / _tickRate;
                if (Time.time - _lastTickTime >= interval)
                {
                    _lastTickTime = Time.time;
                    Tick();
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (_updateMode == UpdateMode.FixedUpdate)
            {
                Tick();
            }
        }
        
        /// <summary>
        /// Manually ticks the behaviour tree.
        /// </summary>
        /// <returns>The state of the tree after the tick.</returns>
        public NodeState Tick()
        {
            if (_runtimeTree == null) return NodeState.Failure;
            
            var state = _runtimeTree.Evaluate();
            
            if (state != NodeState.Running && _restartOnComplete)
            {
                _runtimeTree.Reset();
            }
            
            return state;
        }
        
        /// <summary>
        /// Resets the tree to its initial state.
        /// </summary>
        public void ResetTree()
        {
            _runtimeTree?.Reset();
        }
        
        /// <summary>
        /// Sets a new tree at runtime.
        /// </summary>
        /// <param name="tree">The new tree to use.</param>
        public void SetTree(BehaviourTree tree)
        {
            _tree = tree;
            
            if (tree != null)
            {
                _runtimeTree = tree.Clone();
                _runtimeTree.Bind(gameObject);
            }
            else
            {
                _runtimeTree = null;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Future: Draw tree state in scene view
        }
    }
}
