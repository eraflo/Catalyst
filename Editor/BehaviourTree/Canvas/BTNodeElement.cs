using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Eraflo.Catalyst.BehaviourTree;
using BT = Eraflo.Catalyst.BehaviourTree.BehaviourTree;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    /// <summary>
    /// Visual element representing a node in the behaviour tree.
    /// Simple colored box with output port at bottom.
    /// </summary>
    public class BTNodeElement : VisualElement
    {
        public System.Action<BTNodeElement> OnSelected;
        public System.Action<BTNodeElement> OnStartEdge;
        public System.Action<BTNodeElement, BTPortElement> OnStartDataEdge;
        public System.Action<BTNodeElement> OnPositionChanged;
        
        public Node Node { get; private set; }
        public bool IsSelected { get; private set; }
        
        private VisualElement _body;
        private Label _titleLabel;
        private VisualElement _outputPort;
        private VisualElement _debugBadge;
        private Label _debugLabel;
        private VisualElement _serviceBadge;
        private VisualElement _inputContainer;
        private VisualElement _outputContainer;
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
            
            // Initialise Ports (Runtime)
            node.InitializePorts();
            
            // Input Container (Left)
            _inputContainer = new VisualElement { name = "inputs" };
            _inputContainer.AddToClassList("node-ports-container");
            _inputContainer.AddToClassList("node-ports-left");
            _body.Add(_inputContainer);
            
            // Title
            _titleLabel = new Label(node.name) { name = "node-title" };
            _titleLabel.AddToClassList("node-title");
            _body.Add(_titleLabel);
            
            // Output Container (Right)
            _outputContainer = new VisualElement { name = "outputs" };
            _outputContainer.AddToClassList("node-ports-container");
            _outputContainer.AddToClassList("node-ports-right");
            _body.Add(_outputContainer);
            
            // Instantiate Ports
            foreach (var port in node.Ports)
            {
                var portElem = new BTPortElement(port, node);
                if (port.IsInput) _inputContainer.Add(portElem);
                else _outputContainer.Add(portElem);
                
                // Register callback for edge creation
                portElem.RegisterCallback<MouseDownEvent>(e => OnPortElementMouseDown(e, portElem));
            }
            
            // Debug Badge
            _debugBadge = new VisualElement { name = "node-badge" };
            _debugBadge.AddToClassList("node-badge");
            _debugLabel = new Label("") { name = "debug-label" };
            _debugLabel.AddToClassList("debug-label");
            _debugBadge.Add(_debugLabel);
            _debugBadge.style.display = DisplayStyle.None;
            Add(_debugBadge);
            
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
            
            
            // Service indicator badge
            UpdateServiceBadge();
            
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
        
        /// <summary>
        /// Updates the service badge to reflect current service count.
        /// </summary>
        public void UpdateServiceBadge()
        {
            // Remove existing badge if any
            if (_serviceBadge != null)
            {
                _serviceBadge.RemoveFromHierarchy();
                _serviceBadge = null;
            }
            
            // Create new badge if services exist
            if (Node != null && Node.Services != null && Node.Services.Count > 0)
            {
                _serviceBadge = new VisualElement { name = "service-badge" };
                _serviceBadge.AddToClassList("service-badge");
                
                var serviceIcon = new Label("⚙") { name = "service-icon" };
                serviceIcon.style.fontSize = 10;
                serviceIcon.style.color = new Color(0.3f, 0.9f, 0.5f);
                _serviceBadge.Add(serviceIcon);
                
                var countLabel = new Label(Node.Services.Count.ToString());
                countLabel.style.fontSize = 9;
                countLabel.style.color = new Color(0.3f, 0.9f, 0.5f);
                _serviceBadge.Add(countLabel);
                
                _serviceBadge.tooltip = $"{Node.Services.Count} service(s) attached";
                
                // Add to body for proper positioning within the node
                _body.Add(_serviceBadge);
            }
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

        public void UpdateDebugState()
        {
            RemoveFromClassList("running");
            RemoveFromClassList("success");
            RemoveFromClassList("failure");

            if (!Application.isPlaying || Node == null)
            {
                style.opacity = 1.0f;
                _debugBadge.style.display = DisplayStyle.None;
                return;
            }

            float timeSinceTick = Time.time - Node.LastTickTime;
            
            // If the node was ticked recently (or currently running)
            // Increased threshold to 0.3s to avoid flickering at low frame rates/tick rates
            if (timeSinceTick < 0.3f)
            {
                style.opacity = 1.0f;
                if (Node.State == NodeState.Running)
                {
                    AddToClassList("running");
                }
                else if (Node.LastState == NodeState.Success)
                {
                    AddToClassList("success");
                }
                else if (Node.LastState == NodeState.Failure)
                {
                    AddToClassList("failure");
                }
            }
            else
            {
                // Fade out nodes that haven't been ticked recently (min opacity 0.4)
                float opacity = Mathf.Lerp(1.0f, 0.4f, (timeSinceTick - 0.3f) * 1.5f);
                style.opacity = Mathf.Max(0.4f, opacity);
            }

            // Update debug badge
            if (Application.isPlaying && !string.IsNullOrEmpty(Node.DebugMessage) && timeSinceTick < 1.0f)
            {
                if (_debugLabel.text != Node.DebugMessage)
                    _debugLabel.text = Node.DebugMessage;
                _debugBadge.style.display = DisplayStyle.Flex;
            }
            else
            {
                _debugBadge.style.display = DisplayStyle.None;
            }
        }
        
        public Vector2 GetOutputPortCenter()
        {
            if (_outputPort == null) return GetCenter();
            
            // Use layout for local position relative to node, then add Node.Position
            var portRect = _outputPort.layout;
            
            // Fallback if layout is not yet ready
            float portX = portRect.width > 0 ? portRect.x + portRect.width / 2 : 0;
            float portY = portRect.height > 0 ? portRect.y + portRect.height / 2 : 60; // Estimated height
            
            return new Vector2(Node.Position.x + portX, Node.Position.y + portY);
        }
        
        public Vector2 GetInputCenter()
        {
            var rect = _body.layout;
            
            // Fallback if layout is not yet ready
            float inputX = rect.width > 0 ? rect.x + rect.width / 2 : 50; // Estimated width center
            float inputY = rect.height > 0 ? rect.y : 0;
            
            return new Vector2(Node.Position.x + inputX, Node.Position.y + inputY);
        }
        
        public Vector2 GetCenter()
        {
            var rect = _body.layout;
            
            // Fallback if layout is not yet ready
            float centerX = rect.width > 0 ? rect.x + rect.width / 2 : 50;
            float centerY = rect.height > 0 ? rect.y + rect.height / 2 : 20;
            
            return new Vector2(Node.Position.x + centerX, Node.Position.y + centerY);
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

        private void OnPortElementMouseDown(MouseDownEvent evt, BTPortElement portElem)
        {
            if (evt.button == 0)
            {
                OnStartDataEdge?.Invoke(this, portElem);
                evt.StopPropagation();
            }
        }
        
        public BTPortElement GetPortElement(string portName)
        {
            // Search in inputs and outputs (using Linq implicitly via ToList)
            var input = _inputContainer.Query<BTPortElement>().Build().First() as BTPortElement; 
            // Query returns VisualElements, we need to iterate manually or cast carefully
             
            foreach (var child in _inputContainer.Children())
            {
                if (child is BTPortElement p && p.Port.Name == portName) return p;
            }
            foreach (var child in _outputContainer.Children())
            {
                if (child is BTPortElement p && p.Port.Name == portName) return p;
            }
            return null;
        }


    }
}
