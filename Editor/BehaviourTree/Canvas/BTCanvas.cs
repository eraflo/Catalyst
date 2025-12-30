using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Eraflo.UnityImportPackage.BehaviourTree;
using System.Collections.Generic;
using System.Linq;
using BT = Eraflo.UnityImportPackage.BehaviourTree.BehaviourTree;

namespace Eraflo.UnityImportPackage.Editor.BehaviourTree.Canvas
{
    /// <summary>
    /// Custom canvas with zoom and pan capabilities.
    /// Replaces Unity's GraphView with a simpler, fully customizable solution.
    /// </summary>
    public class BTCanvas : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BTCanvas, UxmlTraits> { }
        
        public System.Action<BTNodeElement> OnNodeSelected;
        public System.Action OnSelectionCleared;
        public System.Action<Vector2, Vector2> OnShowSearchWindow; // screenPos, canvasPos
        
        private VisualElement _contentContainer;
        private VisualElement _edgeLayer;
        private VisualElement _nodeLayer;
        
        private float _zoom = 1f;
        private Vector2 _pan = Vector2.zero;
        private Vector2 _lastMousePos;
        private bool _isPanning;
        
        private BT _tree;
        private List<BTNodeElement> _nodeElements = new List<BTNodeElement>();
        private List<BTEdgeElement> _edgeElements = new List<BTEdgeElement>();
        private List<BTNodeElement> _selectedNodes = new List<BTNodeElement>();
        private List<BTEdgeElement> _selectedEdges = new List<BTEdgeElement>();
        
        // Clipboard
        private System.Type _clipboardType;
        private string _clipboardName;
        
        // Search window
        private BTSearchWindow _searchWindow;
        
        // For edge creation
        private BTNodeElement _edgeStartNode;
        private BTTempEdgeElement _tempEdge;
        
        // For marquee selection
        private VisualElement _selectionRect;
        private bool _isSelecting;
        private Vector2 _selectionStart;
        
        // For node dragging
        private bool _isDraggingNodes;
        
        public const float MinZoom = 0.25f;
        public const float MaxZoom = 2f;
        
        public BTCanvas()
        {
            // Setup styles
            style.flexGrow = 1;
            style.overflow = Overflow.Hidden;
            style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            
            // Grid background
            Add(new BTGridBackground());
            
            // Content container (transformed for zoom/pan)
            _contentContainer = new VisualElement { name = "content-container" };
            _contentContainer.style.position = Position.Absolute;
            _contentContainer.style.left = 0;
            _contentContainer.style.top = 0;
            _contentContainer.pickingMode = PickingMode.Ignore;
            Add(_contentContainer);
            
            // Edge layer (below nodes) - needs Position for clicking on edges
            _edgeLayer = new VisualElement { name = "edge-layer" };
            _edgeLayer.style.position = Position.Absolute;
            _edgeLayer.style.left = 0;
            _edgeLayer.style.top = 0;
            _edgeLayer.style.right = 0;
            _edgeLayer.style.bottom = 0;
            _edgeLayer.pickingMode = PickingMode.Ignore; // Let clicks pass to edges, not the layer
            _contentContainer.Add(_edgeLayer);
            
            // Node layer
            _nodeLayer = new VisualElement { name = "node-layer" };
            _nodeLayer.pickingMode = PickingMode.Ignore;
            _contentContainer.Add(_nodeLayer);
            
            // Selection rect (top layer)
            _selectionRect = new VisualElement { name = "selection-rect" };
            _selectionRect.AddToClassList("selection-rect");
            _selectionRect.style.visibility = Visibility.Hidden;
            _selectionRect.pickingMode = PickingMode.Ignore;
            Add(_selectionRect);
            
            // Register events
            RegisterCallback<WheelEvent>(OnWheel);
            // Use BubbleUp so edges/nodes get clicks first before canvas handles them
            RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.NoTrickleDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            
            focusable = true;
        }
        
        public void LoadTree(BT tree)
        {
            _tree = tree;
            ClearAll();
            
            if (tree == null) return;
            
            // Create node elements
            foreach (var node in tree.Nodes)
            {
                CreateNodeElement(node);
            }
            
            // Create edge elements
            foreach (var node in tree.Nodes)
            {
                CreateEdgesForNode(node);
            }
            
            // Center view on root if exists - wait for layout to be ready
            if (tree.RootNode != null)
            {
                // We need to wait for layout to have dimensions before centering
                RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
            }
        }

        private void OnFirstLayout(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
            
            if (_tree != null && _tree.RootNode != null)
            {
                CenterOnPosition(_tree.RootNode.Position);
            }
        }
        
        private void ClearAll()
        {
            foreach (var node in _nodeElements)
            {
                _nodeLayer.Remove(node);
            }
            _nodeElements.Clear();
            
            foreach (var edge in _edgeElements)
            {
                _edgeLayer.Remove(edge);
            }
            _edgeElements.Clear();
            
            _selectedNodes.Clear();
            _selectedEdges.Clear();
        }
        
        private BTNodeElement CreateNodeElement(Node node)
        {
            var element = new BTNodeElement(node, _tree);
            element.OnSelected += nodeEl => SelectNode(nodeEl, EditorGUI.actionKey);
            element.OnStartEdge += OnStartEdgeCreation;
            element.OnPositionChanged += OnNodePositionChanged;
            
            _nodeElements.Add(element);
            _nodeLayer.Add(element);
            
            return element;
        }
        
        private void CreateEdgesForNode(Node node)
        {
            List<Node> children = new List<Node>();
            
            if (node is CompositeNode composite)
            {
                children.AddRange(composite.Children.Where(c => c != null));
            }
            else if (node is DecoratorNode decorator && decorator.Child != null)
            {
                children.Add(decorator.Child);
            }
            
            var parentElement = FindNodeElement(node);
            if (parentElement == null) return;
            
            foreach (var child in children)
            {
                var childElement = FindNodeElement(child);
                if (childElement != null)
                {
                    CreateEdge(parentElement, childElement);
                }
            }
        }
        
        private void CreateEdge(BTNodeElement from, BTNodeElement to)
        {
            var edge = new BTEdgeElement(from, to);
            edge.OnSelected += e => SelectEdge(e, EditorGUI.actionKey);
            _edgeElements.Add(edge);
            _edgeLayer.Add(edge);
            
            // Schedule UpdatePath so it runs after layout
            edge.schedule.Execute(() => edge.UpdatePath()).ExecuteLater(1);
        }
        
        private BTNodeElement FindNodeElement(Node node)
        {
            return _nodeElements.FirstOrDefault(n => n.Node == node);
        }
        
        private void OnNodeElementSelected(BTNodeElement element)
        {
            // If Shift/Ctrl is not held, clear existing selection unless we already clicked a selected node
            // Note: Key modifiers are better handled in OnMouseDown directly
        }

        public void ClearSelection()
        {
            foreach (var node in _selectedNodes) node.SetSelected(false);
            _selectedNodes.Clear();
            
            foreach (var edge in _selectedEdges) edge.SetSelected(false);
            _selectedEdges.Clear();
            
            OnSelectionCleared?.Invoke();
        }

        public void SelectNode(BTNodeElement element, bool additive)
        {
            if (!additive)
            {
                ClearSelection();
            }

            if (!_selectedNodes.Contains(element))
            {
                element.SetSelected(true);
                _selectedNodes.Add(element);
                OnNodeSelected?.Invoke(element);
            }
        }

        public void SelectEdge(BTEdgeElement edge, bool additive)
        {
            if (!additive)
            {
                ClearSelection();
            }

            if (!_selectedEdges.Contains(edge))
            {
                edge.SetSelected(true);
                _selectedEdges.Add(edge);
            }
        }
        
        private void OnStartEdgeCreation(BTNodeElement fromNode)
        {
            // If the start node is not selected, clear other selections
            if (!_selectedNodes.Contains(fromNode))
            {
                ClearSelection();
            }
            
            _edgeStartNode = fromNode;
            
            // Create temp edge for visual feedback
            _tempEdge = new BTTempEdgeElement(fromNode);
            _edgeLayer.Add(_tempEdge);
        }
        
        private void OnNodePositionChanged(BTNodeElement element)
        {
            // Update edges connected to this node
            // Performance: only update paths for edges touch this node
            for (int i = 0; i < _edgeElements.Count; i++)
            {
                var edge = _edgeElements[i];
                if (edge.FromNode == element || edge.ToNode == element)
                {
                    edge.UpdatePath();
                }
            }
        }
        
        private void OnWheel(WheelEvent evt)
        {
            float zoomDelta = -evt.delta.y * 0.05f;
            float newZoom = Mathf.Clamp(_zoom + zoomDelta, MinZoom, MaxZoom);
            
            if (newZoom != _zoom)
            {
                // Zoom towards mouse position
                var mousePos = evt.localMousePosition;
                var beforeZoom = ScreenToCanvas(mousePos);
                
                _zoom = newZoom;
                ApplyTransform();
                
                var afterZoom = ScreenToCanvas(mousePos);
                _pan += (afterZoom - beforeZoom) * _zoom;
                ApplyTransform();
            }
            
            evt.StopPropagation();
        }
        
        private void OnMouseDown(MouseDownEvent evt)
        {
            Focus();
            
            // Middle mouse or Alt+Left for panning
            if (evt.button == 2 || (evt.button == 0 && evt.altKey))
            {
                _isPanning = true;
                _lastMousePos = evt.localMousePosition;
                evt.StopPropagation();
            }
            // Right click for context menu
            else if (evt.button == 1)
            {
                var canvasPos = ScreenToCanvas(evt.localMousePosition);
                ShowContextMenu(evt.localMousePosition, canvasPos);
                evt.StopPropagation();
            }
            // Double click for node creation
            else if (evt.button == 0 && evt.clickCount == 2)
            {
                var canvasPos = ScreenToCanvas(evt.localMousePosition);
                OnShowSearchWindow?.Invoke(evt.localMousePosition, canvasPos);
                evt.StopPropagation();
            }
            // Left click to clear selection or start selection
            else if (evt.button == 0)
            {
                // If the target is not the canvas itself or one of the layers, 
                // it means we clicked an element (Node, Edge, or their children).
                // Those elements stop propagation, but if they don't, we still ignore them here.
                bool isBackground = evt.target == this || 
                                    evt.target == _contentContainer || 
                                    evt.target == _edgeLayer || 
                                    evt.target == _nodeLayer || 
                                    (evt.target as VisualElement)?.name == "grid-background";
                
                if (!isBackground) return;

                _isSelecting = true;
                _selectionStart = evt.localMousePosition;
                _selectionRect.style.visibility = Visibility.Hidden;
                _selectionRect.style.left = _selectionStart.x;
                _selectionRect.style.top = _selectionStart.y;
                _selectionRect.style.width = 0;
                _selectionRect.style.height = 0;
                
                // Don't clear immediately, wait to see if it's a drag or a click
                evt.StopPropagation();
            }
        }
        
        private void ShowContextMenu(Vector2 screenPos, Vector2 canvasPos)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Create Node"), false, () => OnShowSearchWindow?.Invoke(screenPos, canvasPos));
            
            if (_clipboardType != null)
            {
                menu.AddItem(new GUIContent("Paste"), false, () => PasteNode(canvasPos));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.AddSeparator("");
            
            if (_selectedNodes.Count > 0 || _selectedEdges.Count > 0)
            {
                if (_selectedNodes.Count == 1)
                {
                    menu.AddItem(new GUIContent("Cut"), false, () => CutSelectedNode(_selectedNodes[0]));
                    menu.AddItem(new GUIContent("Copy"), false, () => CopySelectedNode(_selectedNodes[0]));
                }
                
                menu.AddItem(new GUIContent("Delete"), false, DeleteSelection);
                
                if (_selectedNodes.Count == 1)
                {
                    var node = _selectedNodes[0].Node;
                    if (_tree != null && _tree.RootNode != node)
                    {
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Set as Root"), false, () => SetAsRoot(node));
                    }
                }
            }
            
            menu.ShowAsContext();
        }

        private void CopySelectedNode(BTNodeElement node)
        {
            if (node == null) return;
            _clipboardType = node.Node.GetType();
            _clipboardName = node.Node.name;
        }

        private void CutSelectedNode(BTNodeElement node)
        {
            if (node == null) return;
            CopySelectedNode(node);
            _selectedNodes.Clear();
            _selectedNodes.Add(node);
            DeleteSelection();
        }

        private void PasteNode(Vector2 position)
        {
            if (_clipboardType == null || _tree == null) return;
            
            var node = CreateNode(_clipboardType, position);
            if (node != null)
            {
                node.Node.name = _clipboardName;
                EditorUtility.SetDirty(_tree);
            }
        }
        
        private void SetAsRoot(Node node)
        {
            if (_tree == null) return;
            
            Undo.RecordObject(_tree, "Set Root Node");
            _tree.RootNode = node;
            EditorUtility.SetDirty(_tree);
            
            // Refresh to update root styling
            LoadTree(_tree);
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (_isPanning)
            {
                Vector2 delta = evt.localMousePosition - _lastMousePos;
                _pan += delta;
                _lastMousePos = evt.localMousePosition;
                ApplyTransform();
            }
            else if (_isSelecting)
            {
                Vector2 currentPos = evt.localMousePosition;
                
                // Only show marquee after moving a certain distance (threshold)
                if (!_selectionRect.visible && (currentPos - _selectionStart).magnitude > 5f)
                {
                    _selectionRect.style.visibility = Visibility.Visible;
                }

                float x = Mathf.Min(currentPos.x, _selectionStart.x);
                float y = Mathf.Min(currentPos.y, _selectionStart.y);
                float w = Mathf.Abs(currentPos.x - _selectionStart.x);
                float h = Mathf.Abs(currentPos.y - _selectionStart.y);
                
                _selectionRect.style.left = x;
                _selectionRect.style.top = y;
                _selectionRect.style.width = w;
                _selectionRect.style.height = h;
                
                Rect selectionBounds = new Rect(x, y, w, h);
                foreach (var node in _nodeElements)
                {
                    var nodeWorld = node.worldBound;
                    var nodeLocal = this.WorldToLocal(nodeWorld);
                    
                    bool inRect = selectionBounds.Overlaps(nodeLocal);
                    
                    if (evt.actionKey || evt.shiftKey)
                    {
                        // Additive: keep original selection + highlight new ones
                        node.SetSelected(_selectedNodes.Contains(node) || inRect);
                    }
                    else
                    {
                        // Normal: only highlight what's in rect
                        node.SetSelected(inRect);
                    }
                }
            }
            else if (_edgeStartNode != null)
            {
                UpdateTempEdge(evt.localMousePosition);
                
                var targetNode = GetNodeAtPosition(evt.localMousePosition);
                foreach (var node in _nodeElements)
                {
                    bool isValid = targetNode != null && node == targetNode && node != _edgeStartNode;
                    if (isValid)
                    {
                        if (IsInSubtree(node.Node, _edgeStartNode.Node))
                            isValid = false;
                    }
                    node.SetHighlighted(isValid);
                }
            }
            else if (_selectedNodes.Count > 0 && evt.pressedButtons == 1) // Dragging
            {
                Vector2 delta = evt.mouseDelta / _zoom;
                
                if (!_isDraggingNodes)
                {
                    _isDraggingNodes = true;
                    Undo.RecordObjects(_selectedNodes.Select(n => n.Node).ToArray(), "Move Nodes");
                }
                
                foreach (var nodeElement in _selectedNodes)
                {
                    nodeElement.Node.Position += delta;
                    nodeElement.style.left = nodeElement.Node.Position.x;
                    nodeElement.style.top = nodeElement.Node.Position.y;
                    
                    OnNodePositionChanged(nodeElement);
                }
            }
        }
        
        private void UpdateTempEdge(Vector2 mousePos)
        {
            if (_tempEdge != null)
            {
                var canvasPos = ScreenToCanvas(mousePos);
                _tempEdge.UpdateTarget(canvasPos);
            }
        }

        /// <summary>
        /// Check if 'nodeToFind' is in the subtree of 'subtreeRoot'.
        /// If we want to connect subtreeRoot -> nodeToFind as child,
        /// we must ensure nodeToFind is NOT already a parent of subtreeRoot.
        /// </summary>
        private bool IsInSubtree(Node subtreeRoot, Node nodeToFind)
        {
            if (subtreeRoot == null || nodeToFind == null) return false;
            if (subtreeRoot == nodeToFind) return true;

            // Check if nodeToFind is found anywhere in subtreeRoot's children
            if (subtreeRoot is CompositeNode composite)
            {
                foreach (var child in composite.Children)
                {
                    if (child == null) continue;
                    if (IsInSubtree(child, nodeToFind)) return true;
                }
            }
            else if (subtreeRoot is DecoratorNode decorator)
            {
                if (decorator.Child != null && IsInSubtree(decorator.Child, nodeToFind))
                    return true;
            }

            return false;
        }

        private BTNodeElement GetNodeAtPosition(Vector2 localMousePos)
        {
            Vector2 worldPos = this.LocalToWorld(localMousePos);
            return _nodeElements.FirstOrDefault(n => n.worldBound.Contains(worldPos));
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (_isPanning && (evt.button == 2 || evt.button == 0))
            {
                _isPanning = false;
            }
            
            // Reset node dragging state
            if (_isDraggingNodes && evt.button == 0)
            {
                _isDraggingNodes = false;
                // Mark dirty at the end of drag
                foreach (var nodeElement in _selectedNodes)
                {
                    EditorUtility.SetDirty(nodeElement.Node);
                }
            }

            if (_isSelecting)
            {
                _isSelecting = false;
                bool boxWasVisible = _selectionRect.visible;
                _selectionRect.style.visibility = Visibility.Hidden;
                
                if (boxWasVisible)
                {
                    // Commit marquee selection
                    bool additive = evt.actionKey || evt.shiftKey;
                    if (!additive)
                    {
                        _selectedNodes.Clear();
                        _selectedEdges.Clear();
                    }
                    
                    foreach (var node in _nodeElements)
                    {
                        if (node.IsSelected && !_selectedNodes.Contains(node))
                        {
                            _selectedNodes.Add(node);
                        }
                    }
                    
                    if (_selectedNodes.Count > 0)
                        OnNodeSelected?.Invoke(_selectedNodes.Last());
                }
                else if (!evt.actionKey && !evt.shiftKey)
                {
                    // Simple click on background -> clear select
                    ClearSelection();
                }
            }
            
            // Finish edge creation
            if (_edgeStartNode != null)
            {
                var targetNode = GetNodeAtPosition(evt.localMousePosition);
                
                if (targetNode != null && targetNode != _edgeStartNode && !IsInSubtree(targetNode.Node, _edgeStartNode.Node))
                {
                    bool alreadyExists = false;
                    
                    if (_edgeStartNode.Node is CompositeNode composite)
                    {
                        if (composite.Children.Contains(targetNode.Node))
                            alreadyExists = true;
                        else
                        {
                            Undo.RecordObject(composite, "Add Child");
                            composite.Children.Add(targetNode.Node);
                        }
                    }
                    else if (_edgeStartNode.Node is DecoratorNode decorator)
                    {
                        if (decorator.Child == targetNode.Node)
                            alreadyExists = true;
                        else
                        {
                            Undo.RecordObject(decorator, "Set Child");
                            decorator.Child = targetNode.Node;
                        }
                    }

                    if (!alreadyExists)
                    {
                        Undo.RecordObject(_tree, "Create Connection");
                        CreateEdge(_edgeStartNode, targetNode);
                        EditorUtility.SetDirty(_tree);
                    }
                }
                
                // Clean up highlight and temp edge
                if (_tempEdge != null)
                {
                    _edgeLayer.Remove(_tempEdge);
                    _tempEdge = null;
                }
                
                foreach (var node in _nodeElements) node.SetHighlighted(false);
                _edgeStartNode = null;
            }
        }
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete)
            {
                if (_selectedNodes.Count > 0 || _selectedEdges.Count > 0)
                {
                    DeleteSelection();
                    evt.StopPropagation();
                }
            }
            // Add Ctrl+C, Ctrl+V, Ctrl+X
            else if (evt.ctrlKey)
            {
                if (evt.keyCode == KeyCode.C && _selectedNodes.Count == 1)
                {
                    CopySelectedNode(_selectedNodes[0]);
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.V)
                {
                    var canvasPos = ScreenToCanvas(new Vector2(resolvedStyle.width/2, resolvedStyle.height/2));
                    PasteNode(canvasPos);
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.X && _selectedNodes.Count == 1)
                {
                    CutSelectedNode(_selectedNodes[0]);
                    evt.StopPropagation();
                }
            }
        }
        
        private void DeleteSelection()
        {
            if (_tree == null) return;
            
            Undo.RecordObject(_tree, "Delete Selection");
            
            // Delete edges
            foreach (var edge in _selectedEdges)
            {
                // Remove from parent data
                var parentNode = edge.FromNode.Node;
                if (parentNode is CompositeNode composite)
                {
                    Undo.RecordObject(composite, "Remove Child");
                    composite.Children.Remove(edge.ToNode.Node);
                }
                else if (parentNode is DecoratorNode decorator)
                {
                    Undo.RecordObject(decorator, "Remove Child");
                    decorator.Child = null;
                }
                
                _edgeLayer.Remove(edge);
                _edgeElements.Remove(edge);
            }
            _selectedEdges.Clear();

            // Delete nodes
            foreach (var nodeElement in _selectedNodes.ToList())
            {
                var node = nodeElement.Node;
                _tree.DeleteNode(node);
                
                _nodeLayer.Remove(nodeElement);
                _nodeElements.Remove(nodeElement);
                
                // Remove edges connected to this node
                var connectedEdges = _edgeElements.Where(e => 
                    e.FromNode == nodeElement || e.ToNode == nodeElement).ToList();
                foreach (var edge in connectedEdges)
                {
                    _edgeLayer.Remove(edge);
                    _edgeElements.Remove(edge);
                }
            }
            _selectedNodes.Clear();
            
            EditorUtility.SetDirty(_tree);
            OnSelectionCleared?.Invoke();
        }
        
        public BTNodeElement CreateNode(System.Type nodeType, Vector2 position)
        {
            if (_tree == null) return null;
            
            var node = _tree.CreateNode(nodeType);
            node.Position = position;
            
            var element = CreateNodeElement(node);
            OnNodeElementSelected(element);
            
            return element;
        }
        
        private void ApplyTransform()
        {
            _contentContainer.transform.position = new Vector3(_pan.x, _pan.y, 0);
            _contentContainer.transform.scale = new Vector3(_zoom, _zoom, 1);
        }
        
        private Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            return (screenPos - _pan) / _zoom;
        }
        
        private void CenterOnPosition(Vector2 position)
        {
            // If not laid out yet, we can't center accurately
            if (resolvedStyle.width <= 0) return;
            
            var center = new Vector2(resolvedStyle.width / 2, resolvedStyle.height / 2);
            _pan = center - position * _zoom;
            ApplyTransform();
        }
    }
}
