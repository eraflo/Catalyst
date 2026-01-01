using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// SetBlackboardValue action: Sets a value in the blackboard.
    /// </summary>
    [BehaviourTreeNode("Actions/Blackboard", "Set Blackboard Value")]
    public class SetBlackboardValue : ActionNode
    {
        /// <summary>The key to set in the blackboard.</summary>
        [BlackboardKey]
        public string Key;
        
        /// <summary>The type of value to set.</summary>
        public ValueType Type = ValueType.Bool;
        
        /// <summary>Bool value (if Type is Bool).</summary>
        public bool BoolValue;
        
        /// <summary>Int value (if Type is Int).</summary>
        public int IntValue;
        
        /// <summary>Float value (if Type is Float).</summary>
        public float FloatValue;
        
        /// <summary>String value (if Type is String).</summary>
        public string StringValue;
        
        /// <summary>Vector3 value (if Type is Vector3).</summary>
        public Vector3 Vector3Value;
        
        public enum ValueType
        {
            Bool,
            Int,
            Float,
            String,
            Vector3
        }
        
        protected override NodeState OnUpdate()
        {
            if (string.IsNullOrEmpty(Key) || Blackboard == null)
            {
                Debug.LogWarning("[BT] SetBlackboardValue: Invalid key or no blackboard", Owner);
                return NodeState.Failure;
            }
            
            switch (Type)
            {
                case ValueType.Bool:
                    Blackboard.Set(Key, BoolValue);
                    break;
                case ValueType.Int:
                    Blackboard.Set(Key, IntValue);
                    break;
                case ValueType.Float:
                    Blackboard.Set(Key, FloatValue);
                    break;
                case ValueType.String:
                    Blackboard.Set(Key, StringValue);
                    break;
                case ValueType.Vector3:
                    Blackboard.Set(Key, Vector3Value);
                    break;
            }
            
            return NodeState.Success;
        }
    }
}
