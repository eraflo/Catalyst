namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Base class for action (leaf) nodes that perform tasks.
    /// </summary>
    public abstract class ActionNode : Node
    {
        // Action nodes are leaf nodes with no children.
        // All logic is implemented in OnUpdate().
    }
}
