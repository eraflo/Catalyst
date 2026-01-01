using System;
using System.Reflection;
using UnityEngine;
using Eraflo.Catalyst.Events;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Raises any EventChannel type (including custom ones).
    /// Assign any EventChannel ScriptableObject and optionally provide a value from Blackboard.
    /// </summary>
    [BehaviourTreeNode("Actions", "Raise Event")]
    public class RaiseEvent : ActionNode
    {
        /// <summary>
        /// The event channel to raise (any EventChannel or EventChannel&lt;T&gt; derived type).
        /// </summary>
        [Tooltip("Assign any EventChannel asset here (void or typed).")]
        public ScriptableObject Channel;
        
        /// <summary>
        /// For typed events: blackboard key to read the value from.
        /// Leave empty to use the channel's debug value.
        /// </summary>
        [Tooltip("Optional: Blackboard key to read value from (for typed events).")]
        public string BlackboardKey;
        
        // Cached reflection data
        private MethodInfo _raiseMethod;
        private MethodInfo _raiseDebugMethod;
        private Type _valueType;
        private bool _isVoidEvent;
        private bool _cacheBuilt;
        
        protected override void OnStart()
        {
            BuildMethodCache();
        }
        
        private void BuildMethodCache()
        {
            _cacheBuilt = false;
            _raiseMethod = null;
            _raiseDebugMethod = null;
            _valueType = null;
            _isVoidEvent = false;
            
            if (Channel == null) return;
            
            var channelType = Channel.GetType();
            
            // Check if it's a void EventChannel
            if (channelType == typeof(EventChannel) || channelType.IsSubclassOf(typeof(EventChannel)))
            {
                // Check if it's the base EventChannel (void) or a generic one
                // EventChannel has Raise() with no parameters
                _raiseMethod = channelType.GetMethod("Raise", Type.EmptyTypes);
                if (_raiseMethod != null)
                {
                    _isVoidEvent = true;
                    _cacheBuilt = true;
                    return;
                }
            }
            
            // Look for Raise(T) method - typed event channel
            var raiseMethods = channelType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in raiseMethods)
            {
                if (method.Name == "Raise")
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1)
                    {
                        _raiseMethod = method;
                        _valueType = parameters[0].ParameterType;
                        _isVoidEvent = false;
                        break;
                    }
                    else if (parameters.Length == 0)
                    {
                        // Void raise found
                        _raiseMethod = method;
                        _isVoidEvent = true;
                    }
                }
                else if (method.Name == "RaiseDebug" && method.GetParameters().Length == 0)
                {
                    _raiseDebugMethod = method;
                }
            }
            
            _cacheBuilt = _raiseMethod != null;
        }
        
        protected override NodeState OnUpdate()
        {
            if (Channel == null)
            {
                Debug.LogWarning("[BT] RaiseEvent: No Channel assigned", Owner);
                return NodeState.Failure;
            }
            
            if (!_cacheBuilt)
            {
                BuildMethodCache();
            }
            
            if (_raiseMethod == null)
            {
                Debug.LogWarning($"[BT] RaiseEvent: Could not find Raise method on {Channel.GetType().Name}", Owner);
                return NodeState.Failure;
            }
            
            try
            {
                if (_isVoidEvent)
                {
                    // Void event - just call Raise()
                    _raiseMethod.Invoke(Channel, null);
                }
                else if (!string.IsNullOrEmpty(BlackboardKey) && Blackboard != null)
                {
                    // Try to get value from blackboard
                    var getMethod = typeof(Blackboard).GetMethod("Get").MakeGenericMethod(_valueType);
                    var value = getMethod.Invoke(Blackboard, new object[] { BlackboardKey });
                    _raiseMethod.Invoke(Channel, new[] { value });
                }
                else if (_raiseDebugMethod != null)
                {
                    // Use RaiseDebug to use the channel's debug value
                    _raiseDebugMethod.Invoke(Channel, null);
                }
                else
                {
                    // Create default value and raise
                    var defaultValue = _valueType.IsValueType ? Activator.CreateInstance(_valueType) : null;
                    _raiseMethod.Invoke(Channel, new[] { defaultValue });
                }
                
                return NodeState.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BT] RaiseEvent: Error raising event - {ex.Message}", Owner);
                return NodeState.Failure;
            }
        }
    }
}
