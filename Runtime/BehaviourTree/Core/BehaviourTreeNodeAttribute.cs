using System;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Attribute to mark a class as a Behaviour Tree node.
    /// Custom nodes with this attribute are automatically detected 
    /// by the search window and node creation systems.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BehaviourTreeNodeAttribute : Attribute
    {
        /// <summary>Display name in the search window and menus.</summary>
        public string DisplayName { get; set; }
        
        /// <summary>Category path in the search window (e.g. "Actions/Movement").</summary>
        public string Category { get; set; }
        
        /// <summary>Tooltip description.</summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Creates a new BehaviourTreeNodeAttribute.
        /// </summary>
        /// <param name="displayName">Display name in menus.</param>
        public BehaviourTreeNodeAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
        
        /// <summary>
        /// Creates a new BehaviourTreeNodeAttribute with category.
        /// </summary>
        /// <param name="category">Category path (e.g. "Actions/Combat").</param>
        /// <param name="displayName">Display name in menus.</param>
        public BehaviourTreeNodeAttribute(string category, string displayName)
        {
            Category = category;
            DisplayName = displayName;
        }
    }
}
