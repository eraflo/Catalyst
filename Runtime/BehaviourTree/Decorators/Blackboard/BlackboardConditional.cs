using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// BlackboardConditional decorator: Checks a blackboard value and only executes
    /// its child if the condition is met. Returns Failure if condition fails.
    /// </summary>
    [BehaviourTreeNode("Decorators/Blackboard", "Blackboard Conditional")]
    public class BlackboardConditional : DecoratorNode
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
        
        /// <summary>Defines how this condition can abort running nodes.</summary>
        public AbortType AbortMode = AbortType.None;
        
        public enum AbortType
        {
            None,
            Self,
            LowerPriority,
            Both
        }
        
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
        
        protected override void OnStart()
        {
            if (AbortMode != AbortType.None && !string.IsNullOrEmpty(Key) && Blackboard != null)
            {
                Blackboard.RegisterListener(Key, OnBlackboardChanged);
            }
        }

        protected override void OnStop()
        {
            if (AbortMode != AbortType.None && !string.IsNullOrEmpty(Key) && Blackboard != null)
            {
                Blackboard.UnregisterListener(Key, OnBlackboardChanged);
            }
        }

        private void OnBlackboardChanged(object oldVal, object newVal)
        {
            if (AbortMode == AbortType.None || Tree == null) return;
            
            bool resultNow = CheckCondition();
            
            // Case 1: We are NOT running, but condition becomes true -> Abort lower priority
            if (!Started && resultNow && (AbortMode == AbortType.LowerPriority || AbortMode == AbortType.Both))
            {
                Tree.RequestAbort(this);
            }
            // Case 2: We ARE running, but condition becomes false -> Abort self
            else if (Started && !resultNow && (AbortMode == AbortType.Self || AbortMode == AbortType.Both))
            {
                Tree.RequestAbort(this);
            }
        }

        protected override NodeState OnUpdate()
        {
            // First check the condition
            bool passed = CheckCondition();

            if (!passed)
            {
                if (Child != null && Child.Started)
                {
                    Child.Abort();
                }
                return NodeState.Failure;
            }
            
            // Condition passed, execute child
            if (Child == null)
            {
                return NodeState.Success;
            }
            
            return Child.Evaluate();
        }
        
        private bool CheckCondition()
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
