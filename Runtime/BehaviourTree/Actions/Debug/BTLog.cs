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
        
        private string _formattedMessage;

        protected override void OnStart()
        {
            // Message is a SerializeField that does not change at runtime; compute once here.
            _formattedMessage = $"[BT] {Message}";
        }

        protected override NodeState OnUpdate()
        {
            switch (Level)
            {
                case LogLevel.Info:
                    Debug.Log(_formattedMessage, Owner);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(_formattedMessage, Owner);
                    break;
                case LogLevel.Error:
                    Debug.LogError(_formattedMessage, Owner);
                    break;
            }

            return NodeState.Success;
        }
    }
}
