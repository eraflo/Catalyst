using System;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Events
{
    /// <summary>
    /// ScriptableObject-based event channel for void events (no parameters).
    /// Create via Assets > Create > Events > Event Channel.
    /// Can be used from code or via EventChannelListener in the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEventChannel", menuName = "Events/Event Channel", order = 0)]
    public class EventChannel : ScriptableObject
    {
        [SerializeField, TextArea]
        private string _description = "";

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }

        /// <summary>
        /// Raises this event, notifying all subscribers.
        /// </summary>
        public void Raise()
        {
            EventBus.Raise(this);
        }

        /// <summary>
        /// Subscribes a callback to this channel.
        /// </summary>
        /// <param name="callback">The callback to invoke when the event is raised.</param>
        public void Subscribe(Action callback)
        {
            EventBus.Subscribe(this, callback);
        }

        /// <summary>
        /// Unsubscribes a callback from this channel.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        public void Unsubscribe(Action callback)
        {
            EventBus.Unsubscribe(this, callback);
        }

        /// <summary>
        /// Gets the number of current subscribers.
        /// </summary>
        public int SubscriberCount => EventBus.GetSubscriberCount(this);
    }

    /// <summary>
    /// Generic ScriptableObject-based event channel that carries data of type T.
    /// Inherit from this class to create typed event channels.
    /// </summary>
    /// <typeparam name="T">The type of data the event carries.</typeparam>
    public abstract class EventChannel<T> : ScriptableObject
    {
        [SerializeField, TextArea]
        private string _description = "";

        [SerializeField, Tooltip("Debug value for testing in editor.")]
        protected T _debugValue;

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }

        /// <summary>
        /// Raises this event with the specified value, notifying all subscribers.
        /// </summary>
        /// <param name="value">The value to pass to subscribers.</param>
        public void Raise(T value)
        {
            EventBus.Raise(this, value);
        }

        /// <summary>
        /// Raises this event with the debug value (for editor testing).
        /// </summary>
        public void RaiseDebug()
        {
            Raise(_debugValue);
        }

        /// <summary>
        /// Subscribes a callback to this channel.
        /// </summary>
        /// <param name="callback">The callback to invoke when the event is raised.</param>
        public void Subscribe(Action<T> callback)
        {
            EventBus.Subscribe(this, callback);
        }

        /// <summary>
        /// Subscribes a callback (no args) to this channel.
        /// </summary>
        /// <param name="callback">The callback to invoke when the event is raised.</param>
        public void Subscribe(Action callback)
        {
            EventBus.Subscribe(this, callback);
        }

        /// <summary>
        /// Unsubscribes a callback from this channel.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        public void Unsubscribe(Action<T> callback)
        {
            EventBus.Unsubscribe(this, callback);
        }

        /// <summary>
        /// Unsubscribes a callback (no args) from this channel.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        public void Unsubscribe(Action callback)
        {
            EventBus.Unsubscribe(this, callback);
        }

        /// <summary>
        /// Gets the number of current subscribers.
        /// </summary>
        public int SubscriberCount => EventBus.GetSubscriberCount(this);
    }
}
