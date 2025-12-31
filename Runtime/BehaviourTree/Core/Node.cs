using UnityEngine;
using System.Collections.Generic;

namespace Eraflo.UnityImportPackage.BehaviourTree
{
    /// <summary>
    /// Abstract base class for all behaviour tree nodes.
    /// Nodes are ScriptableObjects to allow inspector editing and serialization.
    /// </summary>
    public abstract class Node : ScriptableObject
    {
        /// <summary>The current state of this node.</summary>
        [HideInInspector] public NodeState State = NodeState.Running;
        
        /// <summary>Whether this node has started execution.</summary>
        [HideInInspector] public bool Started = false;
        
        /// <summary>Unique identifier for this node instance.</summary>
        [HideInInspector] public string Guid;
        
        /// <summary>Position in the visual editor (for future use).</summary>
        [HideInInspector] public Vector2 Position;
        
        /// <summary>Reference to the tree this node belongs to.</summary>
        [System.NonSerialized] public BehaviourTree Tree;

        /// <summary>Reference to the parent node.</summary>
        [System.NonSerialized] public Node Parent;
        
        /// <summary>Optional description for this node.</summary>
        [TextArea] public string Description;

        /// <summary>List of services attached to this node.</summary>
        [HideInInspector] public List<ServiceNode> Services = new();

        /// <summary>Runtime only: The time this node started its last execution.</summary>
        [System.NonSerialized] public float StartTime;

        /// <summary>Runtime only: Last time this node was evaluated.</summary>
        [System.NonSerialized] public float LastTickTime;
        
        /// <summary>Runtime only: Last evaluated state.</summary>
        [System.NonSerialized] public NodeState LastState;

        /// <summary>Runtime only: Last debug message from this node.</summary>
        [System.NonSerialized] public string DebugMessage;
        
        /// <summary>
        /// Evaluates this node and returns its state.
        /// </summary>
        /// <returns>The resulting state after evaluation.</returns>
        public NodeState Evaluate()
        {
            if (!Started)
            {
                StartTime = Time.time;
                OnStart();
                Started = true;
                
                // Start services
                foreach (var service in Services)
                {
                    if (service != null) service.Evaluate();
                }
            }
            
            // Update services
            foreach (var service in Services)
            {
                if (service != null) service.TickService();
            }
            
            State = OnUpdate();
            
            // Debugging
            LastTickTime = Time.time;
            LastState = State;
            
            if (State != NodeState.Running)
            {
                OnStop();
                Started = false;
            }
            
            return State;
        }
        
        /// <summary>
        /// Called once when the node starts executing.
        /// </summary>
        protected virtual void OnStart() { }
        
        /// <summary>
        /// Called every update while the node is running.
        /// </summary>
        /// <returns>The current state of the node.</returns>
        protected abstract NodeState OnUpdate();
        
        /// <summary>
        /// Called when the node stops executing (Success or Failure).
        /// </summary>
        protected virtual void OnStop() { }
        
        /// <summary>
        /// Aborts this node, stopping execution immediately.
        /// </summary>
        public virtual void Abort()
        {
            if (Started)
            {
                OnStop();
                Started = false;
            }
            State = NodeState.Failure;
        }
        
        /// <summary>
        /// Creates a runtime clone of this node.
        /// Override in composite/decorator nodes to clone children.
        /// </summary>
        /// <returns>A clone of this node.</returns>
        public virtual Node Clone()
        {
            var clone = Instantiate(this);
            clone.Services = new List<ServiceNode>();
            foreach (var service in Services)
            {
                if (service != null) clone.Services.Add(service.Clone() as ServiceNode);
            }
            return clone;
        }

        /// <summary>
        /// Resets runtime debugging states.
        /// </summary>
        public virtual void ResetRuntimeStates()
        {
            Started = false;
            State = NodeState.Running;
            LastTickTime = 0;
            LastState = NodeState.Running;
            DebugMessage = "";
        }
        
        /// <summary>
        /// Gets the Blackboard from the tree.
        /// </summary>
        protected Blackboard Blackboard => Tree?.Blackboard;
        
        /// <summary>
        /// Gets the GameObject this tree is attached to.
        /// </summary>
        protected GameObject Owner => Tree?.Owner;
    }
}
