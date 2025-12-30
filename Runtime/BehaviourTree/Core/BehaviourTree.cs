using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Eraflo.UnityImportPackage.BehaviourTree
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
        
        /// <summary>The shared blackboard for this tree instance.</summary>
        public Blackboard Blackboard = new();
        
        /// <summary>The GameObject this tree is attached to.</summary>
        [System.NonSerialized] public GameObject Owner;
        
        /// <summary>Current state of the tree.</summary>
        public NodeState TreeState { get; private set; } = NodeState.Running;
        
        /// <summary>
        /// Evaluates the tree, starting from the root node.
        /// </summary>
        /// <returns>The state of the root node after evaluation.</returns>
        public NodeState Evaluate()
        {
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
        /// Binds this tree to a GameObject owner.
        /// </summary>
        /// <param name="owner">The GameObject that owns this tree.</param>
        public void Bind(GameObject owner)
        {
            Owner = owner;
            BindNodes(RootNode);
        }
        
        private void BindNodes(Node node)
        {
            if (node == null) return;
            
            node.Tree = this;
            
            if (node is CompositeNode composite)
            {
                foreach (var child in composite.Children)
                {
                    BindNodes(child);
                }
            }
            else if (node is DecoratorNode decorator)
            {
                BindNodes(decorator.Child);
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
            var clone = Instantiate(this);
            clone.Blackboard = new Blackboard();
            clone.RootNode = RootNode?.Clone();
            clone.Nodes = new List<Node>();
            
            // Collect all cloned nodes
            CollectNodes(clone.RootNode, clone.Nodes);
            
            return clone;
        }
        
        private void CollectNodes(Node node, List<Node> list)
        {
            if (node == null) return;
            
            list.Add(node);
            
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
