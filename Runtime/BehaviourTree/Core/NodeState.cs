namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Represents the state of a behaviour tree node after evaluation.
    /// </summary>
    public enum NodeState
    {
        /// <summary>The node is still running and needs more updates.</summary>
        Running,
        
        /// <summary>The node completed successfully.</summary>
        Success,
        
        /// <summary>The node failed.</summary>
        Failure
    }
}
