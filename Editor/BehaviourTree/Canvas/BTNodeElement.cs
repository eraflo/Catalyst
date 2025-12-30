using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Eraflo.UnityImportPackage.BehaviourTree;
using BT = Eraflo.UnityImportPackage.BehaviourTree.BehaviourTree;

namespace Eraflo.UnityImportPackage.Editor.BehaviourTree.Canvas
{
    /// <summary>
    /// Visual element representing a node in the behaviour tree.
    /// Simple colored box with output port at bottom.
    /// </summary>
    public class BTNodeElement : VisualElement
    {
        public System.Action<BTNodeElement> OnSelected;
        public System.Action<BTNodeElement> OnStartEdge;
        public System.Action<BTNodeElement> OnPositionChanged;
        
        public Node Node { get; private set; }
        public bool IsSelected { get; private set; }
        
        private VisualElement _body;
        private Label _titleLabel;
        private VisualElement _outputPort;
        private BT _tree;
        
        private bool _isDragging;
        private Vector2 _dragStartPos;
        private Vector2 _nodeStartPos;
        
        public BTNodeElement(Node node, BT tree)
        {
            Node = node;
            _tree = tree;
            
            // Setup element
            name = "bt-node";
            style.position = Position.Absolute;
            style.left = node.Position.x;
            style.top = node.Position.y;
            pickingMode = PickingMode.Position;
            
            // Body container
            _body = new VisualElement { name = "node-body" };
            _body.AddToClassList("node-body");
            _body.AddToClassList(GetNodeTypeClass());
            Add(_body);
            
            // Title
            _titleLabel = new Label(node.name) { name = "node-title" };
            _titleLabel.AddToClassList("node-title");
            _body.Add(_titleLabel);
            
            // Output port (only for non-leaf nodes)
            if (node is CompositeNode || node is DecoratorNode)
            {
                _outputPort = new VisualElement { name = "output-port" };
                _outputPort.AddToClassList("output-port");
                Add(_outputPort);
                
                // Port interaction
                _outputPort.RegisterCallback<MouseDownEvent>(OnPortMouseDown);
            }
            
            // Root badge
            if (_tree != null && _tree.RootNode == node)
            {
                AddToClassList("root-node");
            }
            
            // Events
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }
        
        private string GetNodeTypeClass()
        {
            if (Node is CompositeNode) return "composite";
            if (Node is DecoratorNode) return "decorator";
            if (Node is ActionNode) return "action";
            if (Node is ConditionNode) return "condition";
            return "unknown";
        }
        
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selected)
                AddToClassList("selected");
            else
                RemoveFromClassList("selected");
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlighted)
                AddToClassList("highlight");
            else
                RemoveFromClassList("highlight");
        }
        
        public Vector2 GetOutputPortCenter()
        {
            if (_outputPort == null) return GetCenter();
            
            var portRect = _outputPort.worldBound;
            return new Vector2(
                portRect.x + portRect.width / 2 - parent.worldBound.x,
                portRect.y + portRect.height / 2 - parent.worldBound.y
            );
        }
        
        public Vector2 GetInputCenter()
        {
            var rect = _body.worldBound;
            return new Vector2(
                rect.x + rect.width / 2 - parent.worldBound.x,
                rect.y - parent.worldBound.y
            );
        }
        
        public Vector2 GetCenter()
        {
            var rect = _body.worldBound;
            return new Vector2(
                rect.x + rect.width / 2 - parent.worldBound.x,
                rect.y + rect.height / 2 - parent.worldBound.y
            );
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                // Let the canvas handle selection logic with Shift/Ctrl
                OnSelected?.Invoke(this);
                evt.StopPropagation();
            }
        }
        
        private void OnMouseMove(MouseMoveEvent evt) { }
        
        private void OnMouseUp(MouseUpEvent evt) { }
        
        private void OnPortMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                OnStartEdge?.Invoke(this);
                evt.StopPropagation();
            }
        }
        
        public void RefreshTitle()
        {
            _titleLabel.text = Node.name;
        }
    }
}
