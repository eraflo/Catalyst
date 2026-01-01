using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Eraflo.Catalyst.BehaviourTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BT = Eraflo.Catalyst.BehaviourTree.BehaviourTree;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
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
        public System.Action<Vector2, Vector2, System.Type> OnShowSearchWindow; // screenPos, canvasPos, filterType
        public System.Action<System.Type> OnServiceTypeSelected; // Called when a service type is selected
        
        private VisualElement _contentContainer;
        private VisualElement _noteLayer;
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
        private List<BTDataEdgeElement> _selectedDataEdges = new List<BTDataEdgeElement>();
        private List<BTStickyNoteElement> _selectedStickyNotes = new List<BTStickyNoteElement>();
        
        private BTMinimapElement _minimap;
        
        // OPTIMIZATION: Node-to-Element cache for O(1) lookups
        private Dictionary<Node, BTNodeElement> _nodeElementCache = new Dictionary<Node, BTNodeElement>();
        
        // Clipboard
        private System.Type _clipboardType;
        private string _clipboardName;
        
        // Search window
        private BTSearchWindow _searchWindow;
        
        // For edge creation
        private BTNodeElement _edgeStartNode;
        private BTTempEdgeElement _tempEdge;
        
        // Data Flow State
        private BTDataTempEdgeElement _dataTempEdge;
        private BTNodeElement _dataEdgeSourceNode;
        private BTPortElement _dataEdgeSourcePort;
        private bool _isConnectingData;
        
        // For marquee selection
        private VisualElement _selectionRect;
        private bool _isSelecting;
        private Vector2 _selectionStart;
        
        // For node dragging
        private bool _isDraggingNodes;
        
        // For service addition
        private BTNodeElement _pendingServiceTarget;
        
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
            
            // Note layer (bottom layer)
            _noteLayer = new VisualElement { name = "note-layer" };
            _noteLayer.pickingMode = PickingMode.Ignore; // Notes capture events, layer ignores
            _contentContainer.Add(_noteLayer);
            
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
            RegisterCallback<MouseUpEvent>(OnDataMouseUp);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            
            focusable = true;
            
            // Minimap (added last to be on top)
            _minimap = new BTMinimapElement(this);
            Add(_minimap);
        }
        
        public void LoadTree(BT tree)
        {
            _tree = tree;
            ClearAll();
            
            if (tree == null) return;
            
            // Create node elements
            foreach (var node in tree.Nodes)
            {
                if (node != null)
                {
                    CreateNodeElement(node);
                }
            }
            
            // Create edge elements
            foreach (var node in tree.Nodes)
            {
                if (node != null)
                {
                    CreateEdgesForNode(node);
                }
            }
            
            // Create sticky notes
            if (tree.StickyNotes != null)
            {
                foreach (var note in tree.StickyNotes)
                {
                    if (note != null) CreateStickyNoteView(note);
                }
            }
            
            // Initial sort and index update
            foreach (var node in tree.Nodes)
            {
                if (node is CompositeNode composite)
                {
                    composite.SortChildrenByPosition();
                    var element = FindNodeElement(node);
                    if (element != null) UpdateEdgeIndices(element);
                }
            }
            
            // Load Data Edges
            foreach (var node in tree.Nodes)
            {
                node.InitializePorts(); // Ensure ports
                foreach (var port in node.Ports)
                {
                    if (port.IsInput && port.IsConnected)
                    {
                        var sourceNode = tree.Nodes.Find(n => n.Guid == port.ConnectedNodeId);
                        var targetNode = node;
                        if (sourceNode != null)
                        {
                            // Output port lookup
                            sourceNode.InitializePorts();
                            var sourcePort = sourceNode.Ports.Find(p => p.Name == port.ConnectedPortName && !p.IsInput);
                            if (sourcePort != null)
                            {
                                var sourceView = FindNodeElement(sourceNode);
                                var targetView = FindNodeElement(targetNode);
                                if (sourceView != null && targetView != null)
                                {
                                    CreateDataEdgeView(sourceView, sourcePort, targetView, port);
                                }
                            }
                        }
                    }
                }
            }
            
            _minimap?.UpdateMinimap();
            
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
            // Use for loop to avoid enumerator allocation
            for (int i = 0; i < _nodeElements.Count; i++)
            {
                _nodeLayer.Remove(_nodeElements[i]);
            }
            _nodeElements.Clear();
            _nodeElementCache.Clear(); // OPTIMIZATION: Clear cache
            
            for (int i = 0; i < _edgeElements.Count; i++)
            {
                _edgeElements[i].ClearCallbacks();
            }
            _edgeElements.Clear();
            _edgeLayer.Clear();
            
            _selectedNodes.Clear();
            _selectedEdges.Clear();
        }
        
        private BTNodeElement CreateNodeElement(Node node)
        {
            if (node == null) return null;
            var element = new BTNodeElement(node, _tree);
            element.OnSelected += nodeEl => SelectNode(nodeEl, EditorGUI.actionKey);
            element.OnStartEdge += OnStartEdgeCreation;
            element.OnStartDataEdge += OnNodeStartDataEdge;
            element.OnPositionChanged += OnNodePositionChanged;
            
            _nodeElements.Add(element);
            _nodeElementCache[node] = element; // OPTIMIZATION: Cache lookup
            _nodeLayer.Add(element);
            
            return element;
        }
        
        private void CreateStickyNote(Vector2 canvasPos)
        {
            if (_tree == null) return;
            
            Undo.RecordObject(_tree, "Create Sticky Note");
            var note = _tree.CreateStickyNote(canvasPos);
            CreateStickyNoteView(note);
        }
        
        private void CreateStickyNoteView(StickyNote note)
        {
            var element = new BTStickyNoteElement(note);
            _noteLayer.Add(element);
            
            element.OnSelected += (el) => SelectStickyNote(el, EditorGUI.actionKey || Event.current.shift);
            element.OnDelete += DeleteStickyNoteView;
        }

        private void SelectStickyNote(BTStickyNoteElement element, bool additive)
        {
            if (!additive)
            {
                ClearSelection();
            }
            
            if (!_selectedStickyNotes.Contains(element))
            {
                element.SetSelected(true);
                _selectedStickyNotes.Add(element);
            }
        }
        
        private void DeleteStickyNoteView(BTStickyNoteElement element)
        {
            if (element == null || _tree == null) return;
            
            Undo.RecordObject(_tree, "Delete Sticky Note");
            
            _noteLayer.Remove(element);
            _tree.DeleteStickyNote(element.Note);
            
            Undo.DestroyObjectImmediate(element.Note);
            AssetDatabase.SaveAssets();
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
            edge.schedule.Execute(() => {
                edge.UpdatePath();
                UpdateEdgeIndices(from);
            }).ExecuteLater(1);
        }
        
        private void UpdateEdgeIndices(BTNodeElement parent)
        {
            if (parent?.Node is CompositeNode composite)
            {
                // OPTIMIZATION: Avoid LINQ Where().ToList(), use for loop
                for (int i = 0; i < composite.Children.Count; i++)
                {
                    var child = composite.Children[i];
                    if (child == null) continue;
                    
                    // Find matching edge
                    for (int j = 0; j < _edgeElements.Count; j++)
                    {
                        var edge = _edgeElements[j];
                        if (edge.FromNode == parent && edge.ToNode != null && edge.ToNode.Node == child)
                        {
                            edge.SetIndex(i);
                            break;
                        }
                    }
                }
            }
            else
            {
                // Non-composite parents: clear index on all outgoing edges
                for (int i = 0; i < _edgeElements.Count; i++)
                {
                    var edge = _edgeElements[i];
                    if (edge.FromNode == parent)
                    {
                        edge.SetIndex(-1);
                    }
                }
            }
        }
        
        /// <summary>
        /// Finds the visual element for a node. OPTIMIZED: Uses dictionary cache for O(1) lookup.
        /// </summary>
        private BTNodeElement FindNodeElement(Node node)
        {
            if (node == null) return null;
            return _nodeElementCache.TryGetValue(node, out var element) ? element : null;
        }
        
        private void OnNodeElementSelected(BTNodeElement element)
        {
            // If Shift/Ctrl is not held, clear existing selection unless we already clicked a selected node
            // Note: Key modifiers are better handled in OnMouseDown directly
        }

        public void ClearSelection()
        {
            // OPTIMIZATION: Use for loops to avoid enumerator allocations
            for (int i = 0; i < _selectedNodes.Count; i++)
                _selectedNodes[i].SetSelected(false);
            _selectedNodes.Clear();
            
            for (int i = 0; i < _selectedEdges.Count; i++)
                _selectedEdges[i].SetSelected(false);
            _selectedEdges.Clear();
            
            for (int i = 0; i < _selectedDataEdges.Count; i++)
                _selectedDataEdges[i].SetSelected(false);
            _selectedDataEdges.Clear();
            
            for (int i = 0; i < _selectedStickyNotes.Count; i++)
                _selectedStickyNotes[i].SetSelected(false);
            _selectedStickyNotes.Clear();
            
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

        public void UpdateDebugStates()
        {
            // OPTIMIZATION: Use for loops to avoid enumerator allocations
            for (int i = 0; i < _nodeElements.Count; i++)
            {
                var node = _nodeElements[i];
                if (!Application.isPlaying && node.Node != null)
                {
                    node.Node.ResetRuntimeStates();
                }
                node.UpdateDebugState();
            }

            for (int i = 0; i < _edgeElements.Count; i++)
            {
                _edgeElements[i].UpdateDebugState();
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
        
        public Rect GetTreeBounds()
        {
            if (_nodeElements.Count == 0) return new Rect(0, 0, 0, 0);
            
            Rect bounds = new Rect(_nodeElements[0].Node.Position, Vector2.zero);
            
            // OPTIMIZATION: Use for loop
            for (int i = 0; i < _nodeElements.Count; i++)
            {
                var pos = _nodeElements[i].Node.Position;
                bounds.xMin = Mathf.Min(bounds.xMin, pos.x);
                bounds.yMin = Mathf.Min(bounds.yMin, pos.y);
                bounds.xMax = Mathf.Max(bounds.xMax, pos.x + 150);
                bounds.yMax = Mathf.Max(bounds.yMax, pos.y + 100);
            }
            
            return bounds;
        }

        public List<BTNodeElement> GetNodes() => _nodeElements;

        public Rect GetViewport()
        {
            return new Rect(-_pan.x / _zoom, -_pan.y / _zoom, layout.width / _zoom, layout.height / _zoom);
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
                // Only show canvas context menu if clicking background
                 bool isBackground = evt.target == this || 
                                    evt.target == _contentContainer || 
                                    evt.target == _edgeLayer || 
                                    evt.target == _nodeLayer || 
                                    evt.target == _noteLayer ||
                                    (evt.target as VisualElement)?.name == "grid-background";

                if (isBackground)
                {
                    var canvasPos = ScreenToCanvas(evt.localMousePosition);
                    ShowContextMenu(evt.localMousePosition, canvasPos);
                    evt.StopPropagation();
                }
            }
            // Double click for node creation
            else if (evt.button == 0 && evt.clickCount == 2)
            {
                var canvasPos = ScreenToCanvas(evt.localMousePosition);
                OnShowSearchWindow?.Invoke(evt.localMousePosition, canvasPos, null);
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
            
            menu.AddItem(new GUIContent("Create Node"), false, () => OnShowSearchWindow?.Invoke(screenPos, canvasPos, typeof(Node)));
            menu.AddItem(new GUIContent("Create Sticky Note"), false, () => CreateStickyNote(canvasPos));
            
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
                    if (node != null && _tree != null)
                    {
                        if (_tree.RootNode != node)
                        {
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Set as Root"), false, () => SetAsRoot(node));
                        }
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Add Service"), false, () => {
                            _pendingServiceTarget = _selectedNodes[0];
                            OnShowSearchWindow?.Invoke(screenPos, canvasPos, typeof(ServiceNode));
                        });
                    }
                }
            }
            
            menu.ShowAsContext();
        }

        /// <summary>
        /// Called when a service type is selected from the search window.
        /// Routes to AddService with the pending target node.
        /// </summary>
        public void HandleServiceSelection(Type serviceType)
        {
            if (_pendingServiceTarget != null)
            {
                AddService(_pendingServiceTarget, serviceType);
                _pendingServiceTarget = null;
            }
        }

        /// <summary>
        /// Updates the service badge for a specific node.
        /// </summary>
        public void UpdateBadgeForNode(Node node)
        {
            if (node == null) return;
            
            foreach (var element in _nodeElements)
            {
                if (element.Node == node)
                {
                    element.UpdateServiceBadge();
                    break;
                }
            }
        }

        private void AddService(BTNodeElement nodeElement, Type serviceType)
        {
            if (_tree == null || nodeElement == null || nodeElement.Node == null) return;

            Undo.RecordObject(nodeElement.Node, "Add Service");

            var service = ScriptableObject.CreateInstance(serviceType) as ServiceNode;
            service.name = serviceType.Name;
            service.Guid = GUID.Generate().ToString();
            service.Tree = _tree;
            service.Parent = nodeElement.Node;

            nodeElement.Node.Services.Add(service);

            AssetDatabase.AddObjectToAsset(service, _tree);
            EditorUtility.SetDirty(nodeElement.Node);
            EditorUtility.SetDirty(_tree);
            AssetDatabase.SaveAssets();
            
            // Update the visual badge
            nodeElement.UpdateServiceBadge();
            
            // Refresh visuals or inspector
            OnNodeSelected?.Invoke(nodeElement);
        }

        private void CopySelectedNode(BTNodeElement node)
        {
            if (node == null || node.Node == null) return;
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
        
        public void DuplicateSelected()
        {
            if (_selectedNodes.Count == 0 || _tree == null) return;
            
            var newSelection = new List<BTNodeElement>();
            var nodeMap = new Dictionary<Node, Node>();
            
            // 1. Duplicate all selected nodes
            foreach (var nodeElement in _selectedNodes)
            {
                var originalNode = nodeElement.Node;
                if (originalNode == null) continue;
                
                var newNode = originalNode.Clone();
                newNode.name = originalNode.name;
                newNode.Guid = GUID.Generate().ToString();
                newNode.Position += new Vector2(30, 30);
                
                // Important: Clear old references in the clone as they point to original nodes
                if (newNode is CompositeNode composite) composite.Children.Clear();
                if (newNode is DecoratorNode decorator) decorator.Child = null;
                
                _tree.Nodes.Add(newNode);
                AssetDatabase.AddObjectToAsset(newNode, _tree);
                
                // Clone services
                newNode.Services.Clear();
                foreach (var service in originalNode.Services)
                {
                    var newService = service.Clone() as ServiceNode;
                    newService.name = service.name;
                    newService.Guid = GUID.Generate().ToString();
                    newService.Parent = newNode;
                    newService.Tree = _tree;
                    newNode.Services.Add(newService);
                    AssetDatabase.AddObjectToAsset(newService, _tree);
                }
                
                nodeMap[originalNode] = newNode;
            }
            
            // 2. Reconstruct connections between DUPLICATED nodes only
            foreach (var kvp in nodeMap)
            {
                var original = kvp.Key;
                var clone = kvp.Value;
                
                if (original is CompositeNode originalComposite && clone is CompositeNode cloneComposite)
                {
                    foreach (var child in originalComposite.Children)
                    {
                        if (nodeMap.ContainsKey(child))
                        {
                            cloneComposite.Children.Add(nodeMap[child]);
                        }
                    }
                }
                else if (original is DecoratorNode originalDecorator && clone is DecoratorNode cloneDecorator)
                {
                    if (originalDecorator.Child != null && nodeMap.ContainsKey(originalDecorator.Child))
                    {
                        cloneDecorator.Child = nodeMap[originalDecorator.Child];
                    }
                }
            }
            
            // 3. Create views and update selection
            ClearSelection();
            
            foreach (var newNode in nodeMap.Values)
            {
                var newNodeElement = CreateNodeElement(newNode);
                if (newNodeElement != null)
                {
                    SelectNode(newNodeElement, true);
                }
            }
            
            // 4. Create edges for the new internal connections
            foreach (var newNode in nodeMap.Values)
            {
                CreateEdgesForNode(newNode);
            }
            
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(_tree);
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
            if (_isConnectingData && _dataTempEdge != null)
            {
                // Convert mouse to local space of canvas (assuming canvas is at 0,0 relative to itself)
                // Start pos needs to be in canvas space
                if (_dataEdgeSourcePort != null)
                {
                    Vector2 startLocal = _dataEdgeSourcePort.ChangeCoordinatesTo(this, _dataEdgeSourcePort.GetHandle().layout.center);
                    _dataTempEdge.Update(startLocal, evt.localMousePosition);
                }
                evt.StopPropagation(); // Consume data edge drag
                return;
            }

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
                    var nodesToRecord = _selectedNodes.Select(n => n.Node).Where(n => n != null).ToArray();
                    if (nodesToRecord.Length > 0)
                    {
                        Undo.RecordObjects(nodesToRecord, "Move Nodes");
                    }
                }
                
                // 1. Update all node positions first
                foreach (var nodeElement in _selectedNodes)
                {
                    nodeElement.Node.Position += delta;
                    nodeElement.style.left = nodeElement.Node.Position.x;
                    nodeElement.style.top = nodeElement.Node.Position.y;
                }
                
                // 2. Update all connected edges and parent sorting in a second pass
                HashSet<BTNodeElement> parentsToUpdate = new HashSet<BTNodeElement>();
                foreach (var nodeElement in _selectedNodes)
                {
                    OnNodePositionChanged(nodeElement);
                    
                    // Find if this node has a parent to update its sorting
                    var parentEdge = _edgeElements.FirstOrDefault(e => e.ToNode == nodeElement);
                    if (parentEdge != null && parentEdge.FromNode.Node is CompositeNode)
                    {
                        parentsToUpdate.Add(parentEdge.FromNode);
                    }
                }
                
                foreach (var parentEl in parentsToUpdate)
                {
                    (parentEl.Node as CompositeNode).SortChildrenByPosition();
                    UpdateEdgeIndices(parentEl);
                }
                
                _minimap?.UpdateMinimap();
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

        /// <summary>
        /// Gets the node at a screen position. OPTIMIZED: Uses for loop.
        /// </summary>
        private BTNodeElement GetNodeAtPosition(Vector2 localMousePos)
        {
            Vector2 worldPos = this.LocalToWorld(localMousePos);
            for (int i = 0; i < _nodeElements.Count; i++)
            {
                if (_nodeElements[i].worldBound.Contains(worldPos))
                    return _nodeElements[i];
            }
            return null;
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
                
                // Snap to grid
                float gridSize = 20f;
                foreach (var nodeElement in _selectedNodes)
                {
                    if (nodeElement.Node != null)
                    {
                        var pos = nodeElement.Node.Position;
                        pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
                        pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
                        
                        nodeElement.Node.Position = pos;
                        nodeElement.style.left = pos.x;
                        nodeElement.style.top = pos.y;
                        
                        // Update connected edges one last time
                        OnNodePositionChanged(nodeElement);
                        
                        EditorUtility.SetDirty(nodeElement.Node);
                    }
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
                        
                        if (_edgeStartNode.Node is CompositeNode comp)
                        {
                            comp.SortChildrenByPosition();
                            UpdateEdgeIndices(_edgeStartNode);
                        }
                        
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
                if (_selectedNodes.Count > 0 || _selectedEdges.Count > 0 || _selectedDataEdges.Count > 0)
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
                else if (evt.keyCode == KeyCode.D)
                {
                    DuplicateSelected();
                    evt.StopPropagation();
                }
            }
            else if (evt.keyCode == KeyCode.F)
            {
                FocusSelection();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Z)
            {
                ZoomToFit();
                evt.StopPropagation();
            }
        }
        
        private void DeleteSelection()
        {
            if (_tree == null) return;
            
            Undo.RecordObject(_tree, "Delete Selection");
            
            // Delete edges
            foreach (var edge in _selectedEdges)
            {
                if (edge == null) continue;
                
                // Remove from parent data
                var parentNode = edge.FromNode?.Node;
                var childNode = edge.ToNode?.Node;
                
                if (parentNode != null && childNode != null)
                {
                    if (parentNode is CompositeNode composite)
                    {
                        Undo.RecordObject(composite, "Remove Child");
                        composite.Children.Remove(childNode);
                    }
                    else if (parentNode is DecoratorNode decorator)
                    {
                        Undo.RecordObject(decorator, "Remove Child");
                        decorator.Child = null;
                    }
                }
                
                edge.ClearCallbacks();
                _edgeLayer.Remove(edge);
                _edgeElements.Remove(edge);
                
                // Update indices for remaining siblings
                if (edge.FromNode != null) UpdateEdgeIndices(edge.FromNode);
            }
            _selectedEdges.Clear();
            
            // Delete data edges
            foreach (var edge in _selectedDataEdges)
            {
                if (edge == null) continue;
                
                // Disconnect data
                if (edge.ToPort != null)
                {
                    // Update node containing the input port
                    var node = edge.ToNode?.Node;
                    if (node != null)
                    {
                         Undo.RecordObject(node, "Disconnect Data");
                         // We need to modify the 'Ports' list in the node which is rebuilt on load?
                         // Actually Ports runtime list is rebuilt.
                         // But serialization?
                         // The attributes are metadata. The stored data is likely "Fields".
                         // BUT my implementation of NodePort uses reflection to READ.
                         // It doesn't WRITE back to fields yet!
                         // Wait. Step 5064: Node.InitializePorts() reads values.
                         // GetData<T> reads values.
                         // The connection info (ConnectedNodeId) is stored in the NodePort object in the list?
                         // The list is [HideInInspector] public List<NodePort> Ports.
                         // Unity serializes this list!
                         // So initializing ports overwrites it?
                         // "InitializePorts" creates NEW NodePort instances from attributes.
                         // It should PRESERVE existing connections if they match?
                         // OR, I need to make sure InitializePorts handles serialization correctly.
                         // Currently InitializePorts:
                         // Ports.Clear(); loop...
                         // This WIPES data!
                         // I need to fix Node.cs to PERSIST data.
                         // Or "InitializePorts" should only update structure, or "OnEnable" loads it.
                         // Actually, if I serialize the connection data in the LIST, I must not clear it blindly.
                         
                         // Fix Required: Update Node.cs first to support persistence.
                         // But for now, let's assume I need to update the port in the list.
                         // Since I just have 'Ports' list on Node, I update it.
                         
                         var port = node.Ports.Find(p => p.Id == edge.ToPort.Id);
                         if (port != null)
                         {
                             port.ConnectedNodeId = null;
                             port.ConnectedPortName = null;
                         }
                    }
                }
                
                _edgeLayer.Remove(edge);
            }
            _selectedDataEdges.Clear();

            // Delete nodes
            var nodesToDelete = new List<BTNodeElement>(_selectedNodes);
            for (int n = 0; n < nodesToDelete.Count; n++)
            {
                var nodeElement = nodesToDelete[n];
                var node = nodeElement.Node;
                if (node != null)
                {
                    _tree.DeleteNode(node);
                    _nodeElementCache.Remove(node);
                }
                
                _nodeLayer.Remove(nodeElement);
                _nodeElements.Remove(nodeElement);
                
                // Remove edges connected to this node
                for (int i = _edgeElements.Count - 1; i >= 0; i--)
                {
                    var edge = _edgeElements[i];
                    if (edge.FromNode == nodeElement || edge.ToNode == nodeElement)
                    {
                        _edgeLayer.Remove(edge);
                        _edgeElements.RemoveAt(i);
                    }
                }
                
                // Remove DATA edges connected to this node
                var connectedDataEdges = new List<BTDataEdgeElement>();
                foreach (var child in _edgeLayer.Children())
                {
                    if (child is BTDataEdgeElement de && (de.FromNode == nodeElement || de.ToNode == nodeElement))
                    {
                        connectedDataEdges.Add(de);
                    }
                }
                
                foreach (var edge in connectedDataEdges)
                {
                    // If deleting the source, clear the connection on the target
                    if (edge.FromNode == nodeElement && edge.ToNode != null && edge.ToNode.Node != null)
                    {
                        var targetNode = edge.ToNode.Node;
                        // Find port by ID logic (assuming Port ID usage) or Name/IsInput
                        // Note: DataEdgeElement holds reference to NodePort object, but that object might be stale if reload happened?
                        // Actually NodePort is class, but list is rebuilt on deserialization.
                        // Best to look up by Name/Input on the target node.
                        // edge.ToPort is the runtime port instance used for the visual.
                        
                        // We need to find the matching port in the surviving node's current list
                        var targetPort = targetNode.Ports.Find(p => p.Name == edge.ToPort.Name && p.IsInput);
                        if (targetPort != null)
                        {
                            Undo.RecordObject(targetNode, "Disconnect Data");
                            targetPort.ConnectedNodeId = null;
                            targetPort.ConnectedPortName = null;
                        }
                    }
                    
                    _edgeLayer.Remove(edge);
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
            _minimap?.UpdateMinimap();
        }
        
        private Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            return (screenPos - _pan) / _zoom;
        }
        
        public void CenterOnPosition(Vector2 position)
        {
            // If not laid out yet, we can't center accurately
            if (resolvedStyle.width <= 0) return;
            
            var center = new Vector2(resolvedStyle.width / 2, resolvedStyle.height / 2);
            _pan = center - position * _zoom;
            ApplyTransform();
        }

        public void ZoomToFit()
        {
            Rect bounds = GetTreeBounds();
            if (bounds.width <= 0 || bounds.height <= 0) return;
            
            // Wait for layout
            if (resolvedStyle.width <= 0) return;
            
            float padding = 40f;
            float availableWidth = resolvedStyle.width - padding * 2;
            float availableHeight = resolvedStyle.height - padding * 2;
            
            float zoomX = availableWidth / bounds.width;
            float zoomY = availableHeight / bounds.height;
            
            _zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), MinZoom, MaxZoom);
            CenterOnPosition(bounds.center);
        }

        public void FocusSelection()
        {
            if (_selectedNodes.Count > 0)
            {
                Rect bounds = new Rect(_selectedNodes[0].Node.Position, Vector2.zero);
                foreach (var nodeEl in _selectedNodes)
                {
                    var pos = nodeEl.Node.Position;
                    bounds.xMin = Mathf.Min(bounds.xMin, pos.x);
                    bounds.yMin = Mathf.Min(bounds.yMin, pos.y);
                    bounds.xMax = Mathf.Max(bounds.xMax, pos.x + 150);
                    bounds.yMax = Mathf.Max(bounds.yMax, pos.y + 100);
                }
                CenterOnPosition(bounds.center);
            }
            else
            {
                ZoomToFit();
            }
        }

        private BTPortElement GetPortElementFromPick(VisualElement pick)
        {
            var el = pick;
            while (el != null)
            {
                if (el is BTPortElement p) return p;
                el = el.parent;
            }
            return null;
        }
        
        private void TryConnectData(BTNodeElement fromNode, BTPortElement fromElem, BTNodeElement toNode, BTPortElement toElem)
        {
            // Validate
            if (fromNode == toNode) return; // No self connect
            
            var fromPort = fromElem.Port;
            var toPort = toElem.Port;
            
            // Must connect Output to Input (or swap)
            // Normalize: Source = Output, Target = Input
            NodePort outputPort = null;
            NodePort inputPort = null;
            BTNodeElement outputNode = null;
            BTNodeElement inputNode = null;
            
            if (!fromPort.IsInput && toPort.IsInput)
            {
                outputPort = fromPort; outputNode = fromNode;
                inputPort = toPort; inputNode = toNode;
            }
            else if (fromPort.IsInput && !toPort.IsInput)
            {
                outputPort = toPort; outputNode = toNode;
                inputPort = fromPort; inputNode = fromNode;
            }
            else return; // Output-Output or Input-Input
            
            // Check types
            if (!inputPort.DataType.IsAssignableFrom(outputPort.DataType))
            {
               Debug.LogWarning($"Type mismatch: Cannot connect {outputPort.DataType.Name} to {inputPort.DataType.Name}");
               return;
            }
            
            // Create data connection
            Undo.RecordObject(_tree, "Connect Data");
            
            // Disconnect old if exists (Input can only have 1 source)
            if (inputPort.IsConnected)
            {
                // Find old edge element and remove it?
                // Logic should handle refreshing or removing explicitly.
                // Ideal: Find data edge with this target and RemoveFromHierarchy.
                RemoveDataEdge(inputPort.Id);
            }
            
            inputPort.ConnectedNodeId = outputNode.Node.Guid;
            inputPort.ConnectedPortName = outputPort.Name;
            
            // Create Visual
            CreateDataEdgeView(outputNode, outputPort, inputNode, inputPort);
            
            EditorUtility.SetDirty(_tree);
        }
        
        private void RemoveDataEdge(string targetPortId)
        {
            // Simple linear search as we don't have a map
            foreach (var child in _edgeLayer.Children())
            {
                if (child is BTDataEdgeElement edge && edge.ToPort.Id == targetPortId)
                {
                    _edgeLayer.Remove(edge);
                    return;
                }
            }
        }
        
        private void CreateDataEdgeView(BTNodeElement from, NodePort fromPort, BTNodeElement to, NodePort toPort)
        {
            var edge = new BTDataEdgeElement(from, fromPort, to, toPort);
            
            edge.OnSelected = (e) => {
                SelectDataEdge(e, EditorGUI.actionKey || Event.current.shift);
            };
            
            _edgeLayer.Add(edge);
            edge.UpdatePath();
        }
        
        public void SelectDataEdge(BTDataEdgeElement edge, bool additive)
        {
            if (!additive)
            {
                ClearSelection();
            }

            if (!_selectedDataEdges.Contains(edge))
            {
                edge.SetSelected(true);
                _selectedDataEdges.Add(edge);
            }
        }

        private void OnNodeStartDataEdge(BTNodeElement node, BTPortElement port)
        {
            _dataEdgeSourceNode = node;
            _dataEdgeSourcePort = port;
            _isConnectingData = true;
            
            // Create temp edge
            _dataTempEdge = new BTDataTempEdgeElement(node, port.Port);
            Add(_dataTempEdge);
        }

        private void OnDataMouseUp(MouseUpEvent evt)
        {
            if (_isConnectingData)
            {
                // Check if dropped on a port
                var targetElement = panel.Pick(evt.mousePosition); // Screen space pick
                // Walk up to find BTPortElement
                var targetPortElem = GetPortElementFromPick(targetElement);
                
                if (targetPortElem != null && targetPortElem != _dataEdgeSourcePort)
                {
                    var targetNodeElem = FindNodeElement(targetPortElem.Node);
                    if (targetNodeElem != null)
                    {
                        TryConnectData(_dataEdgeSourceNode, _dataEdgeSourcePort, targetNodeElem, targetPortElem);
                    }
                }
                
                // Cleanup temp edge
                if (_dataTempEdge != null)
                {
                    _dataTempEdge.RemoveFromHierarchy();
                    _dataTempEdge = null;
                }
                _isConnectingData = false;
                _dataEdgeSourceNode = null;
                _dataEdgeSourcePort = null;
                evt.StopPropagation(); // Consume event
            }
        }
    }
}
