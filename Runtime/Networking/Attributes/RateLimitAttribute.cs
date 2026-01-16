/*
 * ============================================================================
 * RATE LIMIT ATTRIBUTE
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * Decorates message handlers to limit how many messages a client can send
 * within a time window. Prevents DoS attacks and spam.
 * 
 * USAGE:
 * ------
 * [RateLimit(maxMessages: 10, windowSeconds: 1.0f)]
 * public class MyHandler : INetworkMessageHandler<MyMessage> { }
 * 
 * ============================================================================
 */

using System;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Limits how many times a client can invoke this handler per time window.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RateLimitAttribute : Attribute
    {
        /// <summary>Maximum messages allowed in the window.</summary>
        public int MaxMessages { get; }
        
        /// <summary>Time window in seconds.</summary>
        public float WindowSeconds { get; }
        
        /// <summary>Action to take when limit exceeded.</summary>
        public RateLimitAction Action { get; set; } = RateLimitAction.Reject;

        /// <summary>
        /// Creates a rate limit.
        /// </summary>
        /// <param name="maxMessages">Max messages per window.</param>
        /// <param name="windowSeconds">Window duration in seconds.</param>
        public RateLimitAttribute(int maxMessages, float windowSeconds)
        {
            MaxMessages = maxMessages;
            WindowSeconds = windowSeconds;
        }
    }

    /// <summary>
    /// Action to take when rate limit is exceeded.
    /// </summary>
    public enum RateLimitAction
    {
        /// <summary>Silently drop the message.</summary>
        Reject,
        
        /// <summary>Log a warning and drop.</summary>
        Warn,
        
        /// <summary>Disconnect the client.</summary>
        Disconnect
    }
}
