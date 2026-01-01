using UnityEngine;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.Events
{
    /// <summary>
    /// Network-aware EventChannel for void events.
    /// Auto-registers with EventNetworkHandler.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNetworkEventChannel", menuName = "Events/Network/Event Channel", order = 100)]
    public class NetworkEventChannel : EventChannel
    {
        [Header("Network Settings")]
        [SerializeField] private bool _enableNetwork = false;
        [SerializeField] private NetworkTarget _networkTarget = NetworkTarget.All;
        [SerializeField] private bool _raiseLocally = true;
        [SerializeField] private string _channelId = "";

        public bool EnableNetwork { get => _enableNetwork; set => _enableNetwork = value; }
        public NetworkTarget NetworkTarget { get => _networkTarget; set => _networkTarget = value; }
        public bool RaiseLocally { get => _raiseLocally; set => _raiseLocally = value; }
        public string ChannelId => string.IsNullOrEmpty(_channelId) ? name : _channelId;

        private EventNetworkHandler _handler;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_enableNetwork)
            {
                _handler = NetworkManager.Handlers.Get<EventNetworkHandler>();
                _handler?.Register(this);
            }
        }

        protected new virtual void OnDisable()
        {
            _handler?.Unregister(ChannelId);
            _handler = null;
        }

        public new void Raise()
        {
            Raise(_networkTarget);
        }

        /// <summary>
        /// Raises the event to a specific target.
        /// </summary>
        public void Raise(NetworkTarget target)
        {
            // Lazy get handler if not yet available (ScriptableObject timing)
            if (_enableNetwork && _handler == null)
            {
                _handler = NetworkManager.Handlers.Get<EventNetworkHandler>();
                _handler?.Register(this);
            }

            if (!_enableNetwork || _handler == null || !_handler.IsNetworkAvailable)
            {
                base.Raise();
                return;
            }

            if (_raiseLocally) base.Raise();
            _handler.Send(ChannelId, target);
        }

        public void RaiseLocal() => base.Raise();
    }

    /// <summary>
    /// Generic network-aware EventChannel.
    /// </summary>
    public abstract class NetworkEventChannel<T> : EventChannel<T>
    {
        [Header("Network Settings")]
        [SerializeField] private bool _enableNetwork = false;
        [SerializeField] private NetworkTarget _networkTarget = NetworkTarget.All;
        [SerializeField] private bool _raiseLocally = true;
        [SerializeField] private string _channelId = "";

        public bool EnableNetwork { get => _enableNetwork; set => _enableNetwork = value; }
        public NetworkTarget NetworkTarget { get => _networkTarget; set => _networkTarget = value; }
        public bool RaiseLocally { get => _raiseLocally; set => _raiseLocally = value; }
        public string ChannelId => string.IsNullOrEmpty(_channelId) ? name : _channelId;

        private EventNetworkHandler _handler;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_enableNetwork)
            {
                _handler = NetworkManager.Handlers.Get<EventNetworkHandler>();
                _handler?.Register(this);
            }
        }

        protected new virtual void OnDisable()
        {
            _handler?.Unregister(ChannelId);
            _handler = null;
        }

        public new void Raise(T value)
        {
            if (!_enableNetwork || _handler == null || !_handler.IsNetworkAvailable)
            {
                base.Raise(value);
                return;
            }

            if (_raiseLocally) base.Raise(value);
            _handler.Send(ChannelId, SerializeValue(value), _networkTarget);
        }

        public void RaiseLocal(T value) => base.Raise(value);

        protected virtual byte[] SerializeValue(T value)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public virtual T DeserializeValue(byte[] data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
    }
}
