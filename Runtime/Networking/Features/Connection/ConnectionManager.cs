/*
 * ============================================================================
 * CONNECTION MANAGER
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * Service for managing connection approval with optional security features:
 * - Payload validation via HMAC signatures
 * - Replay attack prevention (timestamp + nonce)
 * - Brute-force protection (attempt limiting)
 * 
 * SECURITY MODEL:
 * ---------------
 * When EnableSecureConnections is true (default):
 * 1. Client creates SecureConnectionPayload with signature
 * 2. Server validates signature before processing
 * 3. Failed validations are logged and rejected
 * 
 * CUSTOMIZATION:
 * --------------
 * - Set custom validator via OnValidateConnection event
 * - Configure security settings in PackageSettings
 * - Disable secure mode for development: EnableSecureConnections = false
 * 
 * ============================================================================
 */

using System;
using System.Collections.Generic;
using Eraflo.Catalyst.Security;
using UnityEngine;

namespace Eraflo.Catalyst.Networking.Features.Connection
{
    /// <summary>
    /// Service for managing connection approval with security features.
    /// </summary>
    [Service(Priority = 5)]
    public class ConnectionManager : IGameService
    {
        private byte[] _localPayload = Array.Empty<byte>();
        private byte[] _connectionKey;
        
        // Brute-force protection
        private readonly Dictionary<string, ConnectionAttemptTracker> _attemptTrackers = new();
        private SecurityManager _security;

        /// <summary>
        /// Event triggered on the server to validate an incoming connection.
        /// </summary>
        public event Func<ConnectionRequest, ConnectionResponse> OnValidateConnection;

        /// <summary>
        /// Event triggered when the local connection payload is updated.
        /// </summary>
        public event Action<byte[]> OnPayloadChanged;

        /// <summary>
        /// Connection security configuration.
        /// </summary>
        public ConnectionSecurityConfig SecurityConfig { get; private set; }

        public void Initialize()
        {
            _security = App.Get<SecurityManager>();
            
            // Generate connection key from session
            if (_security != null)
            {
                _connectionKey = _security.Hash.Hash("connection_key");
            }
            
            SecurityConfig = new ConnectionSecurityConfig
            {
                EnableSecurePayloads = PackageSettings.Instance.EnableSecureConnections,
                MaxPayloadAgeSeconds = PackageSettings.Instance.MaxConnectionPayloadAge,
                MaxAttemptsPerMinute = PackageSettings.Instance.MaxConnectionAttemptsPerMinute,
                BanDurationSeconds = PackageSettings.Instance.ConnectionBanDurationSeconds
            };
        }

        public void Shutdown()
        {
            OnValidateConnection = null;
            _localPayload = null;
            _connectionKey = null;
            _attemptTrackers.Clear();
        }

        /// <summary>
        /// Sets the payload to be sent when connecting as a client.
        /// Automatically signs if security is enabled.
        /// </summary>
        public void SetPayload<T>(T payload)
        {
            byte[] rawData = NetworkSerializer.SerializeValue(payload);
            
            if (SecurityConfig.EnableSecurePayloads && _security != null)
            {
                var secure = SecureConnectionPayload.Create(rawData, _security, _connectionKey);
                _localPayload = secure.Serialize();
            }
            else
            {
                _localPayload = rawData;
            }
            
            OnPayloadChanged?.Invoke(_localPayload);
        }

        /// <summary>
        /// Sets raw payload bytes (use SetPayload&lt;T&gt; for automatic serialization).
        /// </summary>
        public void SetRawPayload(byte[] payload)
        {
            if (SecurityConfig.EnableSecurePayloads && _security != null)
            {
                var secure = SecureConnectionPayload.Create(payload, _security, _connectionKey);
                _localPayload = secure.Serialize();
            }
            else
            {
                _localPayload = payload;
            }
            
            OnPayloadChanged?.Invoke(_localPayload);
        }

        public byte[] GetLocalPayload() => _localPayload;

        /// <summary>
        /// Internal: Handles the validation request from the backend.
        /// </summary>
        internal ConnectionResponse HandleApproval(ulong clientId, byte[] rawPayload, string clientAddress = null)
        {
            // Brute-force protection
            if (!string.IsNullOrEmpty(clientAddress) && !CheckBruteForce(clientAddress))
            {
                return ConnectionResponse.Reject("Too many connection attempts. Try again later.");
            }

            byte[] actualPayload = rawPayload;

            // Validate secure payload if enabled
            if (SecurityConfig.EnableSecurePayloads && _security != null)
            {
                try
                {
                    var secure = SecureConnectionPayload.Deserialize(rawPayload);
                    
                    if (!secure.Validate(_security.Signature, _connectionKey, SecurityConfig.MaxPayloadAgeSeconds))
                    {
                        Debug.LogWarning($"[ConnectionManager] Invalid signature from client {clientId}");
                        RecordFailedAttempt(clientAddress);
                        return ConnectionResponse.Reject("Invalid connection signature");
                    }
                    
                    actualPayload = secure.Data;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ConnectionManager] Payload parse error: {e.Message}");
                    RecordFailedAttempt(clientAddress);
                    return ConnectionResponse.Reject("Malformed connection payload");
                }
            }

            // Run custom validator
            if (OnValidateConnection == null)
            {
                return ConnectionResponse.Success();
            }

            var request = new ConnectionRequest
            {
                ClientId = clientId,
                Payload = actualPayload
            };

            try
            {
                return OnValidateConnection.Invoke(request);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return ConnectionResponse.Reject("Internal server error");
            }
        }

        private bool CheckBruteForce(string address)
        {
            if (string.IsNullOrEmpty(address)) return true;
            
            if (!_attemptTrackers.TryGetValue(address, out var tracker))
            {
                return true;
            }

            // Check if banned
            if (tracker.BannedUntil > DateTime.UtcNow)
            {
                return false;
            }

            // Clean old attempts
            tracker.CleanOldAttempts(60);
            
            return tracker.AttemptCount < SecurityConfig.MaxAttemptsPerMinute;
        }

        private void RecordFailedAttempt(string address)
        {
            if (string.IsNullOrEmpty(address)) return;
            
            if (!_attemptTrackers.TryGetValue(address, out var tracker))
            {
                tracker = new ConnectionAttemptTracker();
                _attemptTrackers[address] = tracker;
            }

            tracker.RecordAttempt();
            
            if (tracker.AttemptCount >= SecurityConfig.MaxAttemptsPerMinute)
            {
                tracker.BannedUntil = DateTime.UtcNow.AddSeconds(SecurityConfig.BanDurationSeconds);
                Debug.LogWarning($"[ConnectionManager] Temporarily banned {address} for brute-force attempts");
            }
        }
    }

    internal class ConnectionAttemptTracker
    {
        private readonly List<DateTime> _attempts = new();
        public DateTime BannedUntil { get; set; }
        
        public int AttemptCount => _attempts.Count;

        public void RecordAttempt()
        {
            _attempts.Add(DateTime.UtcNow);
        }

        public void CleanOldAttempts(int windowSeconds)
        {
            var threshold = DateTime.UtcNow.AddSeconds(-windowSeconds);
            _attempts.RemoveAll(a => a < threshold);
        }
    }
}
