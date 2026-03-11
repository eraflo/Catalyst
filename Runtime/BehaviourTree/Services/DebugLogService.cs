using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Service that periodically logs a debug message.
    /// Useful for debugging and monitoring tree execution.
    /// </summary>
    [BehaviourTreeNode("Services", "Debug Log")]
    public class DebugLogService : ServiceNode
    {
        public string Message = "Service tick";
        public bool LogToConsole = false;

        // Cached to avoid Unity object name allocation on every tick
        private string _cachedParentName;

        protected override void OnStart()
        {
            base.OnStart();
            _cachedParentName = Parent?.name ?? "None";
        }

        protected override void OnServiceUpdate()
        {
            DebugMessage = Message;

            if (LogToConsole)
            {
                Debug.Log($"[BT Service] {Message} - Node: {_cachedParentName}");
            }
        }
    }
}
