using UnityEngine;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Log action: Logs a message to the console and returns Success.
    /// Useful for debugging.
    /// </summary>
    [BehaviourTreeNode("Actions/Debug", "Log")]
    public class BTLog : ActionNode
    {
        /// <summary>The message to log.</summary>
        [TextArea]
        public string Message = "Log";
        
        /// <summary>Log level.</summary>
        public LogLevel Level = LogLevel.Info;
        
        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }
        
        protected override NodeState OnUpdate()
        {
            string formattedMessage = $"[BT] {Message}";
            
            switch (Level)
            {
                case LogLevel.Info:
                    Debug.Log(formattedMessage, Owner);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage, Owner);
                    break;
                case LogLevel.Error:
                    Debug.LogError(formattedMessage, Owner);
                    break;
            }
            
            return NodeState.Success;
        }
    }
}
