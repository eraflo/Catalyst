using UnityEngine;
using UnityEngine.UIElements;
using Eraflo.Catalyst.BehaviourTree;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    public class BTPortElement : VisualElement
    {
        public NodePort Port { get; private set; }
        public Node Node { get; private set; }
        
        private VisualElement _handle;
        private Label _label;
        
        public BTPortElement(NodePort port, Node node)
        {
            Port = port;
            Node = node;
            
            // Setup
            AddToClassList("bt-port");
            AddToClassList(port.IsInput ? "input" : "output");
            
            // Connected state
            if (port.IsConnected)
            {
                AddToClassList("connected");
            }
            
            // Handle (The circle)
            _handle = new VisualElement { name = "port-handle" };
            _handle.AddToClassList("port-handle");
            _handle.AddToClassList(GetTypeClass(port.DataType));
            
            // Label - show abbreviated name if too long
            string displayName = port.Name.Length > 6 ? port.Name.Substring(0, 5) + "…" : port.Name;
            _label = new Label(displayName);
            _label.AddToClassList("port-label");
            
            // Layout based on input/output
            if (port.IsInput)
            {
                Add(_handle);
                Add(_label);
            }
            else
            {
                Add(_label);
                Add(_handle);
            }
            
            // Tooltip with full info
            tooltip = $"{port.Name}\n{port.DataType.Name}";
        }
        
        public Vector2 GetHandlePosition()
        {
            return _handle.worldBound.center;
        }
        
        public VisualElement GetHandle() => _handle;
        
        /// <summary>
        /// Updates the connected state visual.
        /// </summary>
        public void UpdateConnectedState()
        {
            if (Port.IsConnected)
                AddToClassList("connected");
            else
                RemoveFromClassList("connected");
        }
        
        private string GetTypeClass(System.Type type)
        {
            if (type == typeof(bool)) return "type-bool";
            if (type == typeof(float) || type == typeof(int)) return "type-number";
            if (type == typeof(string)) return "type-string";
            if (type == typeof(Vector3) || type == typeof(Vector2)) return "type-vector";
            if (type == typeof(GameObject) || type == typeof(Transform)) return "type-object";
            return "type-generic";
        }
    }
}
