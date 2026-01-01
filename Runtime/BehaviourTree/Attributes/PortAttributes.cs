using System;

namespace Eraflo.Catalyst.BehaviourTree
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NodeInputAttribute : Attribute
    {
        public string Name; // Optional override name
        public NodeInputAttribute(string name = null) { Name = name; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NodeOutputAttribute : Attribute
    {
        public string Name; // Optional override name
        public NodeOutputAttribute(string name = null) { Name = name; }
    }
}
