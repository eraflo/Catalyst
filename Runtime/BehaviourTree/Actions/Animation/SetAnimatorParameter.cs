using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Sets an Animator parameter.
    /// Returns Success immediately.
    /// </summary>
    [BehaviourTreeNode("Actions/Animation", "Set Animator Parameter")]
    public class SetAnimatorParameter : ActionNode
    {
        public enum ParameterType
        {
            Bool,
            Int,
            Float,
            Trigger
        }
        
        [Tooltip("Name of the Animator parameter.")]
        public string ParameterName;
        
        [Tooltip("Type of the parameter.")]
        public ParameterType Type = ParameterType.Bool;
        
        [Tooltip("Bool value (for Bool type).")]
        public bool BoolValue;
        
        [Tooltip("Int value (for Int type).")]
        public int IntValue;
        
        [Tooltip("Float value (for Float type).")]
        public float FloatValue;
        
        [Tooltip("Optional: Read value from Blackboard instead.")]
        [BlackboardKey]
        public string BlackboardKey;
        
        private Animator _animator;
        private int _paramHash;
        
        protected override void OnStart()
        {
            _animator = Owner?.GetComponent<Animator>();
            _paramHash = Animator.StringToHash(ParameterName);
        }
        
        protected override NodeState OnUpdate()
        {
            if (_animator == null || string.IsNullOrEmpty(ParameterName))
            {
                Debug.LogWarning("[BT] SetAnimatorParameter: Invalid setup", Owner);
                return NodeState.Failure;
            }
            
            switch (Type)
            {
                case ParameterType.Bool:
                    bool bVal = GetValue<bool>(BoolValue);
                    _animator.SetBool(_paramHash, bVal);
                    break;
                    
                case ParameterType.Int:
                    int iVal = GetValue<int>(IntValue);
                    _animator.SetInteger(_paramHash, iVal);
                    break;
                    
                case ParameterType.Float:
                    float fVal = GetValue<float>(FloatValue);
                    _animator.SetFloat(_paramHash, fVal);
                    break;
                    
                case ParameterType.Trigger:
                    _animator.SetTrigger(_paramHash);
                    break;
            }
            
            return NodeState.Success;
        }
        
        private T GetValue<T>(T defaultValue)
        {
            if (!string.IsNullOrEmpty(BlackboardKey) && Blackboard != null)
            {
                if (Blackboard.TryGet<T>(BlackboardKey, out var value))
                    return value;
            }
            return defaultValue;
        }
    }
}
