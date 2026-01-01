using System;
using System.Collections.Generic;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Central event bus managing all event subscriptions.
    /// Thread-safe and async-safe implementation.
    /// </summary>
    public static class EventBus
    {
        private static readonly object Lock = new object();
        
        // Stores callbacks per channel instance
        private static readonly Dictionary<object, List<Delegate>> ChannelCallbacks = 
            new Dictionary<object, List<Delegate>>();

        /// <summary>
        /// Subscribes a callback to a specific event channel.
        /// </summary>
        /// <typeparam name="T">The type of value the channel carries.</typeparam>
        /// <param name="channel">The event channel to subscribe to.</param>
        /// <param name="callback">The callback to invoke when the event is raised.</param>
        public static void Subscribe<T>(EventChannel<T> channel, Action<T> callback)
        {
            if (channel == null || callback == null) return;
            
            lock (Lock)
            {
                if (!ChannelCallbacks.TryGetValue(channel, out var callbacks))
                {
                    callbacks = new List<Delegate>();
                    ChannelCallbacks[channel] = callbacks;
                }
                
                if (!callbacks.Contains(callback))
                {
                    callbacks.Add(callback);
                }
            }
        }

        /// <summary>
        /// Subscribes a callback (no args) to a specific event channel.
        /// </summary>
        public static void Subscribe<T>(EventChannel<T> channel, Action callback)
        {
            if (channel == null || callback == null) return;
            
            lock (Lock)
            {
                if (!ChannelCallbacks.TryGetValue(channel, out var callbacks))
                {
                    callbacks = new List<Delegate>();
                    ChannelCallbacks[channel] = callbacks;
                }
                
                if (!callbacks.Contains(callback))
                {
                    callbacks.Add(callback);
                }
            }
        }

        /// <summary>
        /// Subscribes a callback to a void event channel.
        /// </summary>
        public static void Subscribe(EventChannel channel, Action callback)
        {
            if (channel == null || callback == null) return;
            
            lock (Lock)
            {
                if (!ChannelCallbacks.TryGetValue(channel, out var callbacks))
                {
                    callbacks = new List<Delegate>();
                    ChannelCallbacks[channel] = callbacks;
                }
                
                if (!callbacks.Contains(callback))
                {
                    callbacks.Add(callback);
                }
            }
        }

        /// <summary>
        /// Unsubscribes a callback from a specific event channel.
        /// </summary>
        public static void Unsubscribe<T>(EventChannel<T> channel, Action<T> callback)
        {
            if (channel == null || callback == null) return;
            
            lock (Lock)
            {
                if (ChannelCallbacks.TryGetValue(channel, out var callbacks))
                {
                    callbacks.Remove(callback);
                    
                    if (callbacks.Count == 0)
                    {
                        ChannelCallbacks.Remove(channel);
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes a callback (no args) from a specific event channel.
        /// </summary>
        public static void Unsubscribe<T>(EventChannel<T> channel, Action callback)
        {
            if (channel == null || callback == null) return;
            
            lock (Lock)
            {
                if (ChannelCallbacks.TryGetValue(channel, out var callbacks))
                {
                    callbacks.Remove(callback);
                    
                    if (callbacks.Count == 0)
                    {
                        ChannelCallbacks.Remove(channel);
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes a callback from a void event channel.
        /// </summary>
        public static void Unsubscribe(EventChannel channel, Action callback)
        {
            if (channel == null || callback == null) return;
            
            lock (Lock)
            {
                if (ChannelCallbacks.TryGetValue(channel, out var callbacks))
                {
                    callbacks.Remove(callback);
                    
                    if (callbacks.Count == 0)
                    {
                        ChannelCallbacks.Remove(channel);
                    }
                }
            }
        }

        /// <summary>
        /// Raises an event on a typed channel.
        /// </summary>
        internal static void Raise<T>(EventChannel<T> channel, T value)
        {
            if (channel == null) return;
            
            List<Delegate> callbacksCopy;
            
            lock (Lock)
            {
                if (!ChannelCallbacks.TryGetValue(channel, out var callbacks))
                {
                    return;
                }
                
                // Copy to avoid modification during iteration (async-safe)
                callbacksCopy = new List<Delegate>(callbacks);
            }
            
            foreach (var callback in callbacksCopy)
            {
                try
                {
                    if (callback is Action<T> typedCallback)
                    {
                        typedCallback.Invoke(value);
                    }
                    else if (callback is Action noArgsCallback)
                    {
                        noArgsCallback.Invoke();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Raises an event on a void channel.
        /// </summary>
        internal static void Raise(EventChannel channel)
        {
            if (channel == null) return;
            
            List<Delegate> callbacksCopy;
            
            lock (Lock)
            {
                if (!ChannelCallbacks.TryGetValue(channel, out var callbacks))
                {
                    return;
                }
                
                callbacksCopy = new List<Delegate>(callbacks);
            }
            
            foreach (var callback in callbacksCopy)
            {
                try
                {
                    if (callback is Action noArgsCallback)
                    {
                        noArgsCallback.Invoke();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Gets the number of subscribers for a channel.
        /// </summary>
        public static int GetSubscriberCount(object channel)
        {
            if (channel == null) return 0;
            
            lock (Lock)
            {
                return ChannelCallbacks.TryGetValue(channel, out var callbacks) ? callbacks.Count : 0;
            }
        }

        /// <summary>
        /// Clears all subscriptions. Useful for scene transitions.
        /// </summary>
        public static void ClearAll()
        {
            lock (Lock)
            {
                ChannelCallbacks.Clear();
            }
        }

        /// <summary>
        /// Clears subscriptions for a specific channel.
        /// </summary>
        public static void Clear(object channel)
        {
            if (channel == null) return;
            
            lock (Lock)
            {
                ChannelCallbacks.Remove(channel);
            }
        }
    }
}
