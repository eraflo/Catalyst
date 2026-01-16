/*
 * ============================================================================
 * NETWORK MESSAGE ROUTER
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * Routes incoming network messages to registered handlers with:
 * - Type-safe message dispatch
 * - Rate limiting per client
 * - Deserialization caching
 * 
 * SECURITY:
 * ---------
 * - Rate limiting via [RateLimit] attribute
 * - Recursion guard
 * - Exception isolation per handler
 * 
 * ============================================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Routes network messages to handlers with rate limiting.
    /// </summary>
    public class NetworkMessageRouter
    {
        private readonly Dictionary<Type, ushort> _typeToId = new();
        private readonly Dictionary<ushort, Type> _idToType = new();
        private readonly Dictionary<ushort, List<Delegate>> _handlers = new();
        private readonly Dictionary<ushort, System.Reflection.MethodInfo> _deserializeCache = new();
        private readonly Dictionary<ushort, RateLimitAttribute> _rateLimits = new();
        private readonly Dictionary<(ulong ClientId, ushort MsgId), RateLimitTracker> _rateLimitTrackers = new();
        
        private ushort _nextId = 1;
        private bool _isRouting = false;
        
        /// <summary>The ID of the client that sent the last routed message.</summary>
        public ulong LastMessageSenderId { get; private set; }

        public event Action<ushort> OnTypeRegistered;
        public event Action<ushort> OnTypeUnregistered;
        
        /// <summary>
        /// Event triggered when a client violates rate limits.
        /// Subscribe to this to disconnect clients: (clientId, action) => networkManager.DisconnectClient(clientId)
        /// </summary>
        public event Action<ulong, RateLimitAction> OnClientViolation;
        
        /// <summary>Types currently registered in the router.</summary>
        public IEnumerable<Type> RegisteredTypes => _typeToId.Keys;

        /// <summary>Gets the ID associated with a type.</summary>
        public ushort GetIdByType(Type type) => _typeToId.TryGetValue(type, out var id) ? id : (ushort)0;

        public void On<T>(Action<T> handler) where T : struct, INetworkMessage
        {
            var msgId = GetOrCreateId<T>();

            if (!_handlers.TryGetValue(msgId, out var list))
            {
                list = new List<Delegate>();
                _handlers[msgId] = list;
                OnTypeRegistered?.Invoke(msgId);
                
                // Cache rate limit attribute if present
                var attr = Attribute.GetCustomAttribute(typeof(T), typeof(RateLimitAttribute)) as RateLimitAttribute;
                if (attr != null)
                {
                    _rateLimits[msgId] = attr;
                }
            }
            list.Add(handler);
        }

        public void Off<T>(Action<T> handler) where T : struct, INetworkMessage
        {
            var msgId = GetOrCreateId<T>();

            if (_handlers.TryGetValue(msgId, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    _handlers.Remove(msgId);
                    _rateLimits.Remove(msgId);
                    OnTypeUnregistered?.Invoke(msgId);
                }
            }
        }

        public ushort GetId<T>() where T : struct, INetworkMessage => GetOrCreateId<T>();

        public void Route(ushort msgId, byte[] data, ulong senderId)
        {
            if (_isRouting) return; // Recursion loop guard
            
            LastMessageSenderId = senderId;
            if (!_idToType.TryGetValue(msgId, out var type)) return;
            if (!_handlers.TryGetValue(msgId, out var handlers)) return;

            // Rate limiting check
            if (!CheckRateLimit(msgId, senderId))
            {
                return; // Message dropped
            }

            // Deserialize
            if (!_deserializeCache.TryGetValue(msgId, out var deserialize))
            {
                deserialize = typeof(NetworkSerializer).GetMethod("Deserialize").MakeGenericMethod(type);
                _deserializeCache[msgId] = deserialize;
            }

            object message;
            try
            {
                message = deserialize.Invoke(null, new object[] { data });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetworkMessageRouter] Deserialization failed for {type.Name}: {e.Message}");
                return;
            }

            _isRouting = true;
            try
            {
                foreach (var handler in handlers.ToArray())
                {
                    try { handler.DynamicInvoke(message); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
            finally
            {
                _isRouting = false;
            }

            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkMessageRouter] Routed {type.Name}");
            }
        }

        private bool CheckRateLimit(ushort msgId, ulong senderId)
        {
            if (!_rateLimits.TryGetValue(msgId, out var limit))
                return true; // No limit configured
            
            var key = (senderId, msgId);
            if (!_rateLimitTrackers.TryGetValue(key, out var tracker))
            {
                tracker = new RateLimitTracker(limit.WindowSeconds);
                _rateLimitTrackers[key] = tracker;
            }

            if (!tracker.TryConsume())
            {
                // Rate limit exceeded
                switch (limit.Action)
                {
                    case RateLimitAction.Warn:
                        Debug.LogWarning($"[NetworkMessageRouter] Rate limit exceeded for client {senderId} on message {msgId}");
                        break;
                    case RateLimitAction.Disconnect:
                        Debug.LogWarning($"[NetworkMessageRouter] Disconnecting client {senderId} for rate limit violation");
                        OnClientViolation?.Invoke(senderId, RateLimitAction.Disconnect);
                        break;
                }
                return false;
            }

            return true;
        }

        private ushort GetOrCreateId<T>() where T : struct, INetworkMessage
        {
            var type = typeof(T);
            if (!_typeToId.TryGetValue(type, out var id))
            {
                id = _nextId++;
                _typeToId[type] = id;
                _idToType[id] = type;
            }
            return id;
        }

        public void ClearEventSubscribers()
        {
            OnTypeRegistered = null;
            OnTypeUnregistered = null;
        }

        public void Clear()
        {
            _handlers.Clear();
            _typeToId.Clear();
            _idToType.Clear();
            _deserializeCache.Clear();
            _rateLimits.Clear();
            _rateLimitTrackers.Clear();
            _nextId = 1;
            ClearEventSubscribers();
        }

        /// <summary>
        /// Cleans up expired rate limit trackers (call periodically).
        /// </summary>
        public void CleanupTrackers()
        {
            var now = Time.unscaledTime;
            var toRemove = new List<(ulong, ushort)>();
            
            foreach (var kvp in _rateLimitTrackers)
            {
                if (now - kvp.Value.LastAccessTime > 60f) // Remove after 60s idle
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in toRemove)
            {
                _rateLimitTrackers.Remove(key);
            }
        }
    }

    /// <summary>
    /// Tracks rate limit for a specific client+message combination.
    /// </summary>
    internal class RateLimitTracker
    {
        private readonly float _windowSeconds;
        private readonly Queue<float> _timestamps = new();
        
        public float LastAccessTime { get; private set; }

        public RateLimitTracker(float windowSeconds)
        {
            _windowSeconds = windowSeconds;
        }

        public bool TryConsume()
        {
            float now = Time.unscaledTime;
            LastAccessTime = now;
            
            // Remove expired timestamps
            while (_timestamps.Count > 0 && now - _timestamps.Peek() > _windowSeconds)
            {
                _timestamps.Dequeue();
            }

            // Check limit (default 10 per window if not specified)
            if (_timestamps.Count >= 10)
            {
                return false;
            }

            _timestamps.Enqueue(now);
            return true;
        }
    }
}
