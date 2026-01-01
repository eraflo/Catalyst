using UnityEngine;
using UnityEngine.Events;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Generic MonoBehaviour listener for typed EventChannel ScriptableObjects.
    /// Inherit from this class to create listeners for specific channel types.
    /// </summary>
    /// <typeparam name="TChannel">The type of EventChannel to listen to.</typeparam>
    /// <typeparam name="TValue">The type of value the channel carries.</typeparam>
    public abstract class EventChannelListener<TChannel, TValue> : MonoBehaviour
        where TChannel : EventChannel<TValue>
    {
        [Tooltip("The EventChannel to listen to.")]
        [SerializeField] protected TChannel _channel;

        [Tooltip("Response to invoke when the event is raised.")]
        [SerializeField] protected UnityEvent<TValue> _response;

        /// <summary>
        /// The EventChannel this listener is subscribed to.
        /// </summary>
        public TChannel Channel
        {
            get => _channel;
            set => _channel = value;
        }

        /// <summary>
        /// The UnityEvent response triggered when the event is raised.
        /// </summary>
        public UnityEvent<TValue> Response
        {
            get => _response;
            set => _response = value;
        }

        protected virtual void OnEnable()
        {
            if (_channel != null)
            {
                _channel.Subscribe(OnEventRaised);
            }
        }

        protected virtual void OnDisable()
        {
            if (_channel != null)
            {
                _channel.Unsubscribe(OnEventRaised);
            }
        }

        private void OnEventRaised(TValue value)
        {
            _response?.Invoke(value);
        }
    }
}
