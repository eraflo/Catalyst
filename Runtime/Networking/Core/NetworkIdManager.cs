using System.Collections.Generic;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Central registry for mapping network IDs to object instances.
    /// </summary>
    [Service(Priority = 1)]
    public class NetworkIdManager : IGameService
    {
        private readonly Dictionary<uint, object> _idToObject = new Dictionary<uint, object>();
        private readonly Dictionary<object, uint> _objectToId = new Dictionary<object, uint>();

        public void Initialize() { }

        public void Shutdown()
        {
            _idToObject.Clear();
            _objectToId.Clear();
        }

        /// <summary>
        /// Registers a network ID for an object instance.
        /// </summary>
        public void Register(uint networkId, object instance)
        {
            if (instance == null) return;
            _idToObject[networkId] = instance;
            _objectToId[instance] = networkId;
        }

        /// <summary>
        /// Unregisters an object instance from the registry.
        /// </summary>
        public void Unregister(object instance)
        {
            if (instance == null) return;
            if (_objectToId.TryGetValue(instance, out uint id))
            {
                _idToObject.Remove(id);
                _objectToId.Remove(instance);
            }
        }

        /// <summary>
        /// Unregisters a network ID from the registry.
        /// </summary>
        public void UnregisterId(uint networkId)
        {
            if (_idToObject.TryGetValue(networkId, out object instance))
            {
                _idToObject.Remove(networkId);
                _objectToId.Remove(instance);
            }
        }

        /// <summary>
        /// Gets the network ID for a given object instance.
        /// </summary>
        public uint GetId(object instance)
        {
            if (instance == null) return 0;
            return _objectToId.TryGetValue(instance, out uint id) ? id : 0;
        }

        /// <summary>
        /// Gets the object instance for a given network ID.
        /// </summary>
        public T GetObject<T>(uint networkId)
        {
            return _idToObject.TryGetValue(networkId, out object obj) && obj is T typedObj ? typedObj : default;
        }

        /// <summary>
        /// Clears all registrations.
        /// </summary>
        public void Clear()
        {
            _idToObject.Clear();
            _objectToId.Clear();
        }
    }
}
