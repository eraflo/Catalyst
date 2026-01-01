using System;
using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Marks a string field as a Blackboard key selector.
    /// In the inspector, this will show a dropdown with available keys from the BehaviourTree's Blackboard.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class BlackboardKeyAttribute : PropertyAttribute
    {
        /// <summary>
        /// Optional filter for the type of value expected.
        /// </summary>
        public Type ExpectedType { get; set; }
        
        public BlackboardKeyAttribute() { }
        
        public BlackboardKeyAttribute(Type expectedType)
        {
            ExpectedType = expectedType;
        }
    }
}
