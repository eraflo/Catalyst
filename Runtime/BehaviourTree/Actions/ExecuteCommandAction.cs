using System;
using UnityEngine;
using Eraflo.Catalyst.Command;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// A Behaviour Tree node that executes a Command through the Command System.
    /// This allows AI actions to be treated identically to player inputs (Undoable, Recordable).
    /// </summary>
    [BehaviourTreeNode("Actions", "Execute Command")]
    public class ExecuteCommandAction : ActionNode
    {
        [Tooltip("The Assembly-Qualified name of the command type.")]
        public string CommandType;

        [Tooltip("Optional: Blackboard key containing parameters for the command (must be serializable or the ICommand instance itself).")]
        public string BlackboardKey;

        protected override NodeState OnUpdate()
        {
            if (string.IsNullOrEmpty(CommandType))
            {
                Debug.LogWarning("[BT] ExecuteCommandAction: CommandType is empty.");
                return NodeState.Failure;
            }

            try
            {
                Type type = Type.GetType(CommandType);
                if (type == null)
                {
                    Debug.LogWarning($"[BT] ExecuteCommandAction: Could not find type {CommandType}");
                    return NodeState.Failure;
                }

                ICommand command = null;

                if (!string.IsNullOrEmpty(BlackboardKey) && Blackboard != null)
                {
                    var data = Blackboard.Get<object>(BlackboardKey);
                    if (data is ICommand cmd)
                    {
                        command = cmd;
                    }
                    else if (data is byte[] payload)
                    {
                        var serializer = App.Get<Eraflo.Catalyst.Core.Save.SaveManager>()?.Serializer;
                        if (serializer != null)
                        {
                            command = (ICommand)Activator.CreateInstance(type);
                            serializer.Populate(payload, command);
                        }
                    }
                    else if (data != null)
                    {
                        // Direct object-to-object population: serialize then populate
                        var serializer = App.Get<Eraflo.Catalyst.Core.Save.SaveManager>()?.Serializer;
                        if (serializer != null)
                        {
                            byte[] tempPayload = serializer.Serialize(data);
                            command = (ICommand)Activator.CreateInstance(type);
                            serializer.Populate(tempPayload, command);
                        }
                    }
                }

                // Fallback to parameterless creation if no instance provided via blackboard
                if (command == null)
                {
                    command = (ICommand)Activator.CreateInstance(type);
                }

                if (command != null && command.CanExecute())
                {
                    // We don't await here because BT update is synchronous, 
                    // but the command itself is async. It will run in the background.
                    _ = App.Get<CommandManager>().Execute(command);
                    return NodeState.Success;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BT] ExecuteCommandAction Error: {e.Message}");
            }

            return NodeState.Failure;
        }
    }
}
