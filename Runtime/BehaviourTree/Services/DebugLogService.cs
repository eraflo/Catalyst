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

        protected override void OnServiceUpdate()
        {
            DebugMessage = Message;
            
            if (LogToConsole)
            {
                Debug.Log($"[BT Service] {Message} - Node: {Parent?.name ?? "None"}");
            }
        }
    }
}
