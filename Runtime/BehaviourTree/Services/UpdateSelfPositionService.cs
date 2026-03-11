using UnityEngine;
using Eraflo.Catalyst.Core.Blackboard;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Service that updates the owner's position in the Blackboard.
    /// Useful for sharing the agent's position with other systems.
    /// </summary>
    [BehaviourTreeNode("Services", "Update Self Position")]
    public class UpdateSelfPositionService : ServiceNode
    {
        [BlackboardKey]
        public string PositionKey = "SelfPosition";

        protected override void OnServiceUpdate()
        {
            if (Owner == null || Blackboard == null) return;

            Blackboard.Set(PositionKey, Owner.transform.position);
            // NOTE: No IsDebugEnabled property exists in the base class; DebugMessage allocates a string every tick.
            DebugMessage = $"Pos: {Owner.transform.position}";
        }
    }
}
