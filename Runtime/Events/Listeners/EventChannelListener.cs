using UnityEngine;
using UnityEngine.Events;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// MonoBehaviour that listens to an EventChannel and triggers a UnityEvent response.
    /// Add this component to any GameObject to react to events via the inspector.
    /// </summary>
    [AddComponentMenu("Events/Event Channel Listener")]
    public class EventChannelListener : MonoBehaviour
    {
        [Tooltip("The EventChannel to listen to.")]
        [SerializeField] private EventChannel _channel;

        [Tooltip("Response to invoke when the event is raised.")]
        [SerializeField] private UnityEvent _response;

        /// <summary>
        /// The EventChannel this listener is subscribed to.
        /// </summary>
        public EventChannel Channel
        {
            get => _channel;
            set => _channel = value;
        }

        /// <summary>
        /// The UnityEvent response triggered when the event is raised.
        /// </summary>
        public UnityEvent Response
        {
            get => _response;
            set => _response = value;
        }

        private void OnEnable()
        {
            if (_channel != null)
            {
                _channel.Subscribe(OnEventRaised);
            }
        }

        private void OnDisable()
        {
            if (_channel != null)
            {
                _channel.Unsubscribe(OnEventRaised);
            }
        }

        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
