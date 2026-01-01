using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// BlackboardCondition: Checks a blackboard value against a condition.
    /// Returns Success if condition is met, Failure otherwise.
    /// </summary>
    [BehaviourTreeNode("Conditions/Blackboard", "Blackboard Condition")]
    public class BlackboardCondition : ConditionNode
    {
        /// <summary>The key to check in the blackboard.</summary>
        [BlackboardKey]
        public string Key;
        
        /// <summary>The comparison operator.</summary>
        public Operator CompareOperator = Operator.Equals;
        
        /// <summary>The type of value to compare.</summary>
        public ValueType Type = ValueType.Bool;
        
        /// <summary>Bool value to compare against.</summary>
        public bool BoolValue;
        
        /// <summary>Int value to compare against.</summary>
        public int IntValue;
        
        /// <summary>Float value to compare against.</summary>
        public float FloatValue;
        
        /// <summary>String value to compare against.</summary>
        public string StringValue;
        
        public enum Operator
        {
            Equals,
            NotEquals,
            LessThan,
            GreaterThan,
            LessThanOrEqual,
            GreaterThanOrEqual
        }
        
        public enum ValueType
        {
            Bool,
            Int,
            Float,
            String,
            Exists
        }
        
        protected override bool CheckCondition()
        {
            if (string.IsNullOrEmpty(Key) || Blackboard == null)
            {
                return false;
            }
            
            // Special case: just check if key exists
            if (Type == ValueType.Exists)
            {
                bool exists = Blackboard.Contains(Key);
                return CompareOperator == Operator.Equals ? exists : !exists;
            }
            
            switch (Type)
            {
                case ValueType.Bool:
                    return CompareBool();
                case ValueType.Int:
                    return CompareInt();
                case ValueType.Float:
                    return CompareFloat();
                case ValueType.String:
                    return CompareString();
            }
            
            return false;
        }
        
        private bool CompareBool()
        {
            if (!Blackboard.TryGet<bool>(Key, out bool value))
                return false;
            
            return CompareOperator switch
            {
                Operator.Equals => value == BoolValue,
                Operator.NotEquals => value != BoolValue,
                _ => false
            };
        }
        
        private bool CompareInt()
        {
            if (!Blackboard.TryGet<int>(Key, out int value))
                return false;
            
            return CompareOperator switch
            {
                Operator.Equals => value == IntValue,
                Operator.NotEquals => value != IntValue,
                Operator.LessThan => value < IntValue,
                Operator.GreaterThan => value > IntValue,
                Operator.LessThanOrEqual => value <= IntValue,
                Operator.GreaterThanOrEqual => value >= IntValue,
                _ => false
            };
        }
        
        private bool CompareFloat()
        {
            if (!Blackboard.TryGet<float>(Key, out float value))
                return false;
            
            return CompareOperator switch
            {
                Operator.Equals => Mathf.Approximately(value, FloatValue),
                Operator.NotEquals => !Mathf.Approximately(value, FloatValue),
                Operator.LessThan => value < FloatValue,
                Operator.GreaterThan => value > FloatValue,
                Operator.LessThanOrEqual => value <= FloatValue,
                Operator.GreaterThanOrEqual => value >= FloatValue,
                _ => false
            };
        }
        
        private bool CompareString()
        {
            if (!Blackboard.TryGet<string>(Key, out string value))
                return false;
            
            return CompareOperator switch
            {
                Operator.Equals => value == StringValue,
                Operator.NotEquals => value != StringValue,
                _ => false
            };
        }
    }
}
