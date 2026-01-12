using System;
using System.Collections.Generic;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.InputSystem.Combos;
using UnityEngine;

namespace Eraflo.Catalyst.InputSystem.Network
{
    /// <summary>
    /// Bridges the Input system and the Networking system.
    /// Handles synchronization of inputs and combo executions based on AuthorityMode.
    /// </summary>
    public class InputNetworkHandler : INetworkMessageHandler, IAuthoritativeHandler
    {
        private InputManager _inputManager;
        private NetworkManager _networkManager;
        private ComboSystem _localComboSystem;
        
        // Dictionary of combo systems per client (used on server for validation)
        private readonly Dictionary<ulong, ComboSystem> _clientComboSystems = new Dictionary<ulong, ComboSystem>();
        private ComboDatabase _comboDatabase;

        public AuthorityMode Authority { get; set; } = AuthorityMode.ServerAuthoritative;

        public event Action<ComboExecutedMessage> OnRemoteComboExecuted;

        public void Initialize(ComboDatabase database)
        {
            _comboDatabase = database;
        }

        public void OnRegistered()
        {
            _inputManager = App.Get<InputManager>();
            _networkManager = App.Get<NetworkManager>();
            
            // Set default authority from settings
            Authority = PackageSettings.Instance.DefaultAuthorityMode;

            if (_inputManager != null)
            {
                _inputManager.OnInputBuffered += HandleLocalInput;
            }

            if (_networkManager != null)
            {
                _networkManager.On<InputSyncMessage>(HandleRemoteInput);
                _networkManager.On<ComboExecutedMessage>(HandleRemoteCombo);
            }
        }

        public void OnUnregistered()
        {
            if (_inputManager != null)
            {
                _inputManager.OnInputBuffered -= HandleLocalInput;
            }

            if (_networkManager != null)
            {
                _networkManager.Off<InputSyncMessage>(HandleRemoteInput);
                _networkManager.Off<ComboExecutedMessage>(HandleRemoteCombo);
            }
        }

        public void OnNetworkConnected() { }
        public void OnNetworkDisconnected() 
        {
            _clientComboSystems.Clear();
        }

        /// <summary>
        /// Sets the local combo system to monitor for local executions.
        /// </summary>
        public void SetLocalComboSystem(ComboSystem comboSystem)
        {
            if (_localComboSystem != null)
            {
                _localComboSystem.OnComboExecuted -= HandleLocalCombo;
            }
            
            _localComboSystem = comboSystem;
            
            if (_localComboSystem != null)
            {
                _localComboSystem.OnComboExecuted += HandleLocalCombo;
            }
        }

        private void HandleLocalInput(InputBufferedEvent evt)
        {
            if (_networkManager == null || !_networkManager.IsConnected) return;

            // In ServerAuthoritative mode, we send raw inputs to the server
            if (Authority == AuthorityMode.ServerAuthoritative && _networkManager.IsClient && !_networkManager.IsServer)
            {
                var msg = new InputSyncMessage
                {
                    Inputs = new List<InputSyncMessage.InputData>
                    {
                        new InputSyncMessage.InputData { ActionId = evt.ActionId, Timestamp = evt.Timestamp }
                    }
                };
                _networkManager.SendToServer(msg);
            }
        }

        private void HandleLocalCombo(ComboDefinition combo)
        {
            if (_networkManager == null || !_networkManager.IsConnected) return;

            // In ClientAuthoritative mode, the client broadcasts the execution
            if (Authority == AuthorityMode.ClientAuthoritative && _networkManager.IsClient)
            {
                var msg = new ComboExecutedMessage
                {
                    ComboId = combo.ComboId,
                    ClientId = _networkManager.LocalClientId
                };
                
                if (_networkManager.IsServer)
                    _networkManager.SendToClients(msg);
                else
                    _networkManager.SendToServer(msg);
            }
        }

        private void HandleRemoteInput(InputSyncMessage msg)
        {
            // The server receives raw inputs and validates them using a shadow ComboSystem
            if (_networkManager.IsServer)
            {
                ulong senderId = _networkManager.Router.LastMessageSenderId;
                
                if (!_clientComboSystems.TryGetValue(senderId, out var comboSystem))
                {
                    if (_comboDatabase != null)
                    {
                        comboSystem = new ComboSystem(_comboDatabase);
                        comboSystem.OnComboExecuted += (combo) => 
                        {
                            // Server validated a combo for this client
                            var confirmationMsg = new ComboExecutedMessage
                            {
                                ComboId = combo.ComboId,
                                ClientId = senderId
                            };
                            _networkManager.SendToClients(confirmationMsg);
                        };
                        _clientComboSystems[senderId] = comboSystem;
                    }
                }

                if (comboSystem != null)
                {
                    foreach (var input in msg.Inputs)
                    {
                        comboSystem.ProcessInput(input.ActionId, input.Timestamp);
                    }
                }
            }
        }

        private void HandleRemoteCombo(ComboExecutedMessage msg)
        {
            // Other clients (and the local client in ServerAuth mode) receive execution confirmations
            if (!_networkManager.IsServer || Authority == AuthorityMode.ClientAuthoritative)
            {
                // Trigger event for external systems (animations, VFX, etc.)
                OnRemoteComboExecuted?.Invoke(msg);
                
                if (PackageSettings.Instance.NetworkDebugMode)
                {
                    Debug.Log($"[Network] Combo {msg.ComboId} executed by client {msg.ClientId}");
                }
            }
        }
    }
}
