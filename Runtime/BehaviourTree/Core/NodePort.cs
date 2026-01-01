using System;
using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    [System.Serializable]
    public class NodePort
    {
        public string Name;
        public string Id; // Unique ID for finding this port
        public bool IsInput;
        public string ConnectedNodeId; // The node we're connected to
        public string ConnectedPortName; // The specific port on that node
        
        // Runtime typing
        [NonSerialized] public Type DataType;
        
        public bool IsConnected => !string.IsNullOrEmpty(ConnectedNodeId);
        
        public NodePort(string name, bool isInput, Type type)
        {
            Name = name;
            Id = System.Guid.NewGuid().ToString();
            IsInput = isInput;
            DataType = type;
        }
    }
}
