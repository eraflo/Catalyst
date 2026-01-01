using UnityEngine;
using System.Collections.Generic;

namespace Eraflo.Catalyst.BehaviourTree
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
        
        /// <summary>Data ports defined on this node.</summary>
        [HideInInspector] public List<NodePort> Ports = new();
        [System.NonSerialized] private bool _portsInitialized;
        
        // OPTIMIZATION: Static cache for FieldInfo lookups (shared across all nodes)
        private static readonly Dictionary<(System.Type, string), System.Reflection.FieldInfo> _fieldCache = new();
        
        // OPTIMIZATION: Per-instance port cache for O(1) lookup
        [System.NonSerialized] private Dictionary<string, NodePort> _inputPortCache;
        
        // OPTIMIZATION: Cached empty string to avoid repeated allocations
        protected static readonly string EmptyDebugMessage = string.Empty;
        
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
                
                // Start services - using for loop to avoid enumerator allocation
                for (int i = 0; i < Services.Count; i++)
                {
                    var service = Services[i];
                    if (service != null) service.Evaluate();
                }
            }
            
            // Update services - using for loop to avoid enumerator allocation
            for (int i = 0; i < Services.Count; i++)
            {
                var service = Services[i];
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
            DebugMessage = EmptyDebugMessage;
            _inputPortCache = null; // Clear port cache on reset
        }
        
        /// <summary>
        /// Gets the Blackboard from the tree.
        /// </summary>
        protected Blackboard Blackboard => Tree?.Blackboard;
        
        /// <summary>
        /// Gets the GameObject this tree is attached to.
        /// </summary>
        protected GameObject Owner => Tree?.Owner;

        
        // ============================================
        // DATA FLOW
        // ============================================
        
        /// <summary>
        /// Initializes ports by scanning for attributes.
        /// </summary>
        public void InitializePorts()
        {
            if (_portsInitialized) return;
            
            var type = GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var inputAttr = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<NodeInputAttribute>(field);
                if (inputAttr != null)
                {
                    AddPort(inputAttr.Name ?? field.Name, true, field.FieldType);
                }
                
                var outputAttr = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<NodeOutputAttribute>(field);
                if (outputAttr != null)
                {
                    AddPort(outputAttr.Name ?? field.Name, false, field.FieldType);
                }
            }
            
            _portsInitialized = true;
        }
        
        private void AddPort(string name, bool isInput, System.Type type)
        {
            var existing = Ports.Find(p => p.Name == name && p.IsInput == isInput);
            if (existing != null)
            {
                existing.DataType = type;
                return;
            }
            Ports.Add(new NodePort(name, isInput, type));
        }

        /// <summary>
        /// Retrieves data from an input port.
        /// If connected, pulls from the source. If not, returns the local value (fallback).
        /// OPTIMIZED: Uses cached port lookups and field info caching.
        /// </summary>
        protected T GetData<T>(string portName, T fallbackValue = default)
        {
            // Build port cache on first access
            _inputPortCache ??= BuildInputPortCache();
            
            // O(1) port lookup instead of O(n)
            if (!_inputPortCache.TryGetValue(portName, out var port))
                return fallbackValue;
                
            if (port != null && port.IsConnected)
            {
                // Use GUID index for O(1) node lookup
                var sourceNode = Tree.GetNodeByGuid(port.ConnectedNodeId);
                if (sourceNode != null)
                {
                    // Use cached FieldInfo instead of reflection each time
                    var field = GetCachedField(sourceNode.GetType(), port.ConnectedPortName);
                    if (field != null)
                    {
                        return (T)field.GetValue(sourceNode);
                    }
                }
            }
            return fallbackValue;
        }
        
        /// <summary>
        /// Builds a dictionary cache of input ports for O(1) lookup.
        /// </summary>
        private Dictionary<string, NodePort> BuildInputPortCache()
        {
            var cache = new Dictionary<string, NodePort>();
            for (int i = 0; i < Ports.Count; i++)
            {
                var port = Ports[i];
                if (port.IsInput) cache[port.Name] = port;
            }
            return cache;
        }
        
        /// <summary>
        /// Gets a cached FieldInfo for a type/field combination.
        /// Thread-safe due to dictionary's read operations being safe.
        /// </summary>
        private static System.Reflection.FieldInfo GetCachedField(System.Type type, string fieldName)
        {
            var key = (type, fieldName);
            if (!_fieldCache.TryGetValue(key, out var field))
            {
                field = type.GetField(fieldName, 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                _fieldCache[key] = field;
            }
            return field;
        }
    }
}
