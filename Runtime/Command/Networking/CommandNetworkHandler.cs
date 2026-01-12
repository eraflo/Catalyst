using System;
using UnityEngine;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Command.Networking
{
    /// <summary>
    /// Handles incoming networked commands.
    /// </summary>
    public class CommandNetworkHandler : INetworkMessageHandler
    {
        private NetworkManager _network;
        private CommandManager _commandManager;
        private ISerializer _serializer;

        public void OnRegistered()
        {
            _network = App.Get<NetworkManager>();
            _commandManager = App.Get<CommandManager>();
            _serializer = App.Get<SaveManager>()?.Serializer;

            _network?.On<CommandNetworkMessage>(HandleCommandMessage);
        }

        public void OnUnregistered()
        {
            _network?.Off<CommandNetworkMessage>(HandleCommandMessage);
        }

        public void OnNetworkConnected() { }
        public void OnNetworkDisconnected() { }

        private async void HandleCommandMessage(CommandNetworkMessage msg)
        {
            try
            {
                Type type = Type.GetType(msg.CommandType);
                if (type == null)
                {
                    Debug.LogWarning($"[CommandNetworkHandler] Unknown command type: {msg.CommandType}");
                    return;
                }

                // Deserialization via Populate (using common pattern for Catalyst serializers)
                ICommand command = (ICommand)Activator.CreateInstance(type);
                if (command != null)
                {
                    _serializer.Populate(msg.Payload, command);
                    
                    // Execute through manager bypassing history recording
                    if (_commandManager != null) await _commandManager.ExecuteDirect(command);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandNetworkHandler] Failed to process network command: {e.Message}");
            }
        }
    }
}
