using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.UnityImportPackage.Networking
{
    /// <summary>
    /// Manages network backend factories.
    /// </summary>
    public class NetworkBackendRegistry
    {
        private readonly Dictionary<string, INetworkBackendFactory> _factories = new Dictionary<string, INetworkBackendFactory>();

        public void Register(INetworkBackendFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factories[factory.Id] = factory;
            
            if (PackageSettings.Instance.NetworkDebugMode)
            {
                Debug.Log($"[NetworkBackendRegistry] Registered: {factory.Id}");
            }
        }

        public void Unregister(string id) => _factories.Remove(id);

        public INetworkBackendFactory Get(string id) 
            => _factories.TryGetValue(id, out var f) ? f : null;

        public INetworkBackend Create(string id)
        {
            var factory = Get(id);
            return factory?.IsAvailable == true ? factory.Create() : null;
        }

        public IEnumerable<string> GetAvailableIds()
        {
            foreach (var kvp in _factories)
                if (kvp.Value.IsAvailable)
                    yield return kvp.Key;
        }

        public void Clear() => _factories.Clear();
    }
}
