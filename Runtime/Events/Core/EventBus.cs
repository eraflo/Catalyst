using System;
using System.Collections.Generic;
using Eraflo.Catalyst;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Central event bus managing all event subscriptions.
    /// Supports both ScriptableObject Channels and Generic Type-based events.
    /// </summary>
    [Service(Priority = -10)]
    public class EventBus : IGameService
    {
        private readonly object Lock = new object();
        
        private readonly Dictionary<object, List<Delegate>> ChannelCallbacks = 
            new Dictionary<object, List<Delegate>>();

        private readonly Dictionary<Type, List<Delegate>> TypeCallbacks = 
            new Dictionary<Type, List<Delegate>>();

        #region IGameService

        void IGameService.Initialize() { }
        void IGameService.Shutdown() => ClearAll();

        #endregion

        #region Channel-Based Events

        public void Subscribe<T>(EventChannel<T> channel, Action<T> callback)
        {
            if (channel == null || callback == null) return;
            RegisterCallback(channel, callback);
        }

        public void Subscribe<T>(EventChannel<T> channel, Action callback)
        {
            if (channel == null || callback == null) return;
            RegisterCallback(channel, callback);
        }

        public void Subscribe(EventChannel channel, Action callback)
        {
            if (channel == null || callback == null) return;
            RegisterCallback(channel, callback);
        }

        public void Unsubscribe<T>(EventChannel<T> channel, Action<T> callback)
        {
            if (channel == null || callback == null) return;
            UnregisterCallback(channel, callback);
        }

        public void Unsubscribe<T>(EventChannel<T> channel, Action callback)
        {
            if (channel == null || callback == null) return;
            UnregisterCallback(channel, callback);
        }

        public void Unsubscribe(EventChannel channel, Action callback)
        {
            if (channel == null || callback == null) return;
            UnregisterCallback(channel, callback);
        }

        internal void Raise<T>(EventChannel<T> channel, T value)
        {
            if (channel == null) return;
            List<Delegate> callbacks;
            lock (Lock)
            {
                if (!ChannelCallbacks.TryGetValue(channel, out var originalCallbacks)) return;
                callbacks = new List<Delegate>(originalCallbacks);
            }

            foreach (var callback in callbacks)
            {
                try
                {
                    if (callback is Action<T> typed) typed.Invoke(value);
                    else if (callback is Action noArgs) noArgs.Invoke();
                }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
            }
        }

        internal void Raise(EventChannel channel)
        {
            if (channel == null) return;
            List<Delegate> callbacks;
            lock (Lock)
            {
                if (!ChannelCallbacks.TryGetValue(channel, out var originalCallbacks)) return;
                callbacks = new List<Delegate>(originalCallbacks);
            }

            foreach (var callback in callbacks)
            {
                try { if (callback is Action noArgs) noArgs.Invoke(); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
            }
        }

        #endregion

        #region Type-Based Events

        public void Subscribe<T>(Action<T> callback)
        {
            if (callback == null) return;
            var type = typeof(T);
            lock (Lock)
            {
                if (!TypeCallbacks.TryGetValue(type, out var callbacks))
                {
                    callbacks = new List<Delegate>();
                    TypeCallbacks[type] = callbacks;
                }
                if (!callbacks.Contains(callback)) callbacks.Add(callback);
            }
        }

        public void Unsubscribe<T>(Action<T> callback)
        {
            if (callback == null) return;
            var type = typeof(T);
            lock (Lock)
            {
                if (TypeCallbacks.TryGetValue(type, out var callbacks))
                {
                    callbacks.Remove(callback);
                    if (callbacks.Count == 0) TypeCallbacks.Remove(type);
                }
            }
        }

        public void Publish<T>(T evt)
        {
            if (evt == null) return;
            var type = typeof(T);
            List<Delegate> callbacks;
            lock (Lock)
            {
                if (!TypeCallbacks.TryGetValue(type, out var originalCallbacks)) return;
                callbacks = new List<Delegate>(originalCallbacks);
            }

            foreach (var callback in callbacks)
            {
                try { if (callback is Action<T> action) action.Invoke(evt); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
            }
        }

        #endregion

        #region Helper Methods

        private void RegisterCallback(object key, Delegate callback)
        {
            lock (Lock)
            {
                if (!ChannelCallbacks.TryGetValue(key, out var callbacks))
                {
                    callbacks = new List<Delegate>();
                    ChannelCallbacks[key] = callbacks;
                }
                if (!callbacks.Contains(callback)) callbacks.Add(callback);
            }
        }

        private void UnregisterCallback(object key, Delegate callback)
        {
            lock (Lock)
            {
                if (ChannelCallbacks.TryGetValue(key, out var callbacks))
                {
                    callbacks.Remove(callback);
                    if (callbacks.Count == 0) ChannelCallbacks.Remove(key);
                }
            }
        }

        public int GetSubscriberCount(object key)
        {
            lock (Lock)
            {
                if (ChannelCallbacks.TryGetValue(key, out var c1)) return c1.Count;
                if (key is Type t && TypeCallbacks.TryGetValue(t, out var c2)) return c2.Count;
                return 0;
            }
        }

        public void ClearAll()
        {
            lock (Lock)
            {
                ChannelCallbacks.Clear();
                TypeCallbacks.Clear();
            }
        }

        public void Clear() => ClearAll();

        public void Clear(object key)
        {
            if (key == null) return;
            lock (Lock)
            {
                ChannelCallbacks.Remove(key);
                if (key is Type t) TypeCallbacks.Remove(t);
            }
        }

        #endregion
    }
}
