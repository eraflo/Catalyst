using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// A behaviour tree asset that can be assigned to agents.
    /// The tree is cloned at runtime to allow multiple agents to use the same asset.
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviourTree", menuName = "Behaviour Tree/Behaviour Tree")]
    public class BehaviourTree : ScriptableObject
    {
        /// <summary>The root node of the tree.</summary>
        [HideInInspector] public Node RootNode;
        
        /// <summary>All nodes in this tree (for serialization).</summary>
        [HideInInspector] public List<Node> Nodes = new();
        
        /// <summary>Notes for documentation in the graph.</summary>
        [HideInInspector] public List<StickyNote> StickyNotes = new();
        
        /// <summary>The shared blackboard for this tree instance.</summary>
        public Blackboard Blackboard = new();
        
        /// <summary>The GameObject this tree is attached to.</summary>
        [System.NonSerialized] public GameObject Owner;
        
        /// <summary>Current state of the tree.</summary>
        public NodeState TreeState { get; private set; } = NodeState.Running;
        
        /// <summary>The node that is currently executing.</summary>
        [System.NonSerialized] public Node CurrentRunningNode;
        
        private bool _abortRequested = false;
        
        // OPTIMIZATION: GUID-to-Node index for O(1) lookups instead of O(n) list search
        [System.NonSerialized] private Dictionary<string, Node> _nodeGuidIndex;
        [System.NonSerialized] private bool _guidIndexDirty = true;
        
        /// <summary>
        /// Evaluates the tree, starting from the root node.
        /// </summary>
        /// <returns>The state of the root node after evaluation.</returns>
        public NodeState Evaluate()
        {
            _abortRequested = false;
            if (RootNode != null)
            {
                TreeState = RootNode.Evaluate();
            }
            else
            {
                TreeState = NodeState.Failure;
            }
            return TreeState;
        }
        
        /// <summary>
        /// Signals that a conditional abort has been triggered.
        /// </summary>
        public void RequestAbort(Node source)
        {
            _abortRequested = true;
            // The Runner will check for _abortRequested and tick again if needed
        }
        
        public bool IsAbortRequested() => _abortRequested;
        
        /// <summary>
        /// Gets a node by its GUID with O(1) lookup.
        /// OPTIMIZED: Uses dictionary index instead of list search.
        /// </summary>
        /// <param name="guid">The GUID of the node to find.</param>
        /// <returns>The node with the specified GUID, or null if not found.</returns>
        public Node GetNodeByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            
            if (_guidIndexDirty || _nodeGuidIndex == null)
            {
                RebuildGuidIndex();
            }
            
            return _nodeGuidIndex.TryGetValue(guid, out var node) ? node : null;
        }
        
        /// <summary>
        /// Rebuilds the GUID-to-Node index. Call after adding/removing nodes.
        /// </summary>
        public void RebuildGuidIndex()
        {
            _nodeGuidIndex ??= new Dictionary<string, Node>();
            _nodeGuidIndex.Clear();
            
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node != null && !string.IsNullOrEmpty(node.Guid))
                {
                    _nodeGuidIndex[node.Guid] = node;
                }
            }
            
            _guidIndexDirty = false;
        }
        
        /// <summary>
        /// Marks the GUID index as dirty, requiring rebuild on next access.
        /// </summary>
        public void InvalidateGuidIndex()
        {
            _guidIndexDirty = true;
        }
        
        /// <summary>
        /// Binds this tree to a GameObject owner.
        /// </summary>
        /// <param name="owner">The GameObject that owns this tree.</param>
        public void Bind(GameObject owner)
        {
            Owner = owner;
            BindNodes(RootNode, null);
        }
        
        private void BindNodes(Node node, Node parent)
        {
            if (node == null) return;
            
            node.Tree = this;
            node.Parent = parent;
            
            foreach (var service in node.Services)
            {
                BindNodes(service, node);
            }
            
            if (node is CompositeNode composite)
            {
                foreach (var child in composite.Children)
                {
                    BindNodes(child, node);
                }
            }
            else if (node is DecoratorNode decorator)
            {
                BindNodes(decorator.Child, node);
            }
        }
        
        /// <summary>
        /// Resets the tree to its initial state.
        /// </summary>
        public void Reset()
        {
            TreeState = NodeState.Running;
            Blackboard.Clear();
            ResetNodes(RootNode);
        }
        
        private void ResetNodes(Node node)
        {
            if (node == null) return;
            
            node.State = NodeState.Running;
            node.Started = false;
            
            foreach (var service in node.Services)
            {
                ResetNodes(service);
            }
            
            if (node is CompositeNode composite)
            {
                foreach (var child in composite.Children)
                {
                    ResetNodes(child);
                }
            }
            else if (node is DecoratorNode decorator)
            {
                ResetNodes(decorator.Child);
            }
        }
        
        /// <summary>
        /// Creates a runtime clone of this tree.
        /// </summary>
        /// <returns>A cloned tree ready for runtime use.</returns>
        public BehaviourTree Clone()
        {
            var treeClone = Instantiate(this);
            treeClone.Blackboard = Blackboard.Clone();
            treeClone.Nodes = new List<Node>();
            treeClone.StickyNotes = new List<StickyNote>();

            // 1. Instantiate all nodes and map them
            var nodeMap = new Dictionary<Node, Node>();
            
            // Loop over original nodes to ensure we catch orphans
            foreach (var node in Nodes)
            {
                if (node == null) continue;
                
                var nodeClone = Instantiate(node);
                nodeClone.Tree = treeClone;
                nodeClone.Guid = node.Guid; // Keep GUID for data flow connections
                nodeClone.Services = new List<ServiceNode>(); // Clear/Init services list
                
                // Clear structural refs (they point to old assets)
                if (nodeClone is CompositeNode c) c.Children = new List<Node>();
                if (nodeClone is DecoratorNode d) d.Child = null;
                
                treeClone.Nodes.Add(nodeClone);
                nodeMap[node] = nodeClone;
            }

            // 2. Reconnect references
            foreach (var original in Nodes)
            {
                if (original == null) continue;
                
                var clone = nodeMap[original];
                
                // Link Services
                foreach (var service in original.Services)
                {
                    if (service != null && nodeMap.ContainsKey(service))
                    {
                        clone.Services.Add(nodeMap[service] as ServiceNode);
                    }
                }
                
                // Link Children (Composite)
                if (original is CompositeNode composite)
                {
                    var compositeClone = clone as CompositeNode;
                    foreach (var child in composite.Children)
                    {
                        if (child != null && nodeMap.ContainsKey(child))
                        {
                            compositeClone.Children.Add(nodeMap[child]);
                            nodeMap[child].Parent = compositeClone;
                        }
                    }
                }
                
                // Link Child (Decorator)
                if (original is DecoratorNode decorator)
                {
                    var decoratorClone = clone as DecoratorNode;
                    if (decorator.Child != null && nodeMap.ContainsKey(decorator.Child))
                    {
                        decoratorClone.Child = nodeMap[decorator.Child];
                        nodeMap[decorator.Child].Parent = decoratorClone;
                    }
                }
            }

            // 3. Set Root
            if (RootNode != null && nodeMap.ContainsKey(RootNode))
            {
                treeClone.RootNode = nodeMap[RootNode];
            }
            
            // Clone sticky notes
            if (StickyNotes != null)
            {
                foreach (var note in StickyNotes)
                {
                    if (note != null) treeClone.StickyNotes.Add(Instantiate(note));
                }
            }
            
            return treeClone;
        }
        
        private void CollectNodes(Node node, List<Node> list)
        {
            if (node == null) return;
            
            list.Add(node);
            
            foreach (var service in node.Services)
            {
                CollectNodes(service, list);
            }
            
            if (node is CompositeNode composite)
            {
                foreach (var child in composite.Children)
                {
                    CollectNodes(child, list);
                }
            }
            else if (node is DecoratorNode decorator)
            {
                CollectNodes(decorator.Child, list);
            }
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Creates a node of the specified type and adds it to this tree.
        /// Editor only.
        /// </summary>
        /// <param name="type">The type of node to create.</param>
        /// <returns>The created node.</returns>
        public Node CreateNode(System.Type type)
        {
            var node = CreateInstance(type) as Node;
            node.name = type.Name;
            node.Guid = GUID.Generate().ToString();
            
            Nodes.Add(node);
            
            AssetDatabase.AddObjectToAsset(node, this);
            AssetDatabase.SaveAssets();
            
            return node;
        }
        
        /// <summary>
        /// Deletes a node from this tree.
        /// Editor only.
        /// </summary>
        /// <param name="node">The node to delete.</param>
        public void DeleteNode(Node node)
        {
            Nodes.Remove(node);
            AssetDatabase.RemoveObjectFromAsset(node);
            AssetDatabase.SaveAssets();
        }
        
        public StickyNote CreateStickyNote(Vector2 position)
        {
            var note = CreateInstance<StickyNote>();
            note.name = "StickyNote";
            note.Position.x = position.x;
            note.Position.y = position.y;
            
            StickyNotes.Add(note);
            
            AssetDatabase.AddObjectToAsset(note, this);
            AssetDatabase.SaveAssets();
            
            return note;
        }
        
        public void DeleteStickyNote(StickyNote note)
        {
            StickyNotes.Remove(note);
            AssetDatabase.RemoveObjectFromAsset(note);
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Adds a child node to a parent composite node.
        /// Editor only.
        /// </summary>
        public void AddChild(Node parent, Node child)
        {
            if (parent is CompositeNode composite)
            {
                composite.Children.Add(child);
                EditorUtility.SetDirty(composite);
            }
            else if (parent is DecoratorNode decorator)
            {
                decorator.Child = child;
                EditorUtility.SetDirty(decorator);
            }
        }
        
        /// <summary>
        /// Removes a child node from a parent composite node.
        /// Editor only.
        /// </summary>
        public void RemoveChild(Node parent, Node child)
        {
            if (parent is CompositeNode composite)
            {
                composite.Children.Remove(child);
                EditorUtility.SetDirty(composite);
            }
            else if (parent is DecoratorNode decorator)
            {
                decorator.Child = null;
                EditorUtility.SetDirty(decorator);
            }
        }
        
        /// <summary>
        /// Gets the children of a node.
        /// Editor only.
        /// </summary>
        public List<Node> GetChildren(Node parent)
        {
            var children = new List<Node>();
            
            if (parent is CompositeNode composite)
            {
                return composite.Children;
            }
            else if (parent is DecoratorNode decorator && decorator.Child != null)
            {
                children.Add(decorator.Child);
            }
            
            return children;
        }
#endif
    }
}
