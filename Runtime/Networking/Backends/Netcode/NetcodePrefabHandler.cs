#if UNITY_NETCODE
using System.Collections.Generic;
using UnityEngine;
using NetcodeMgr = Unity.Netcode.NetworkManager;
using Unity.Netcode;
using Eraflo.Catalyst.Pooling;

namespace Eraflo.Catalyst.Networking.Backends.Netcode
{
    /// <summary>
    /// Handles networked prefab instantiation and synchronization for Netcode for GameObjects.
    /// Integrates with Catalyst's Pooling system.
    /// </summary>
    public class NetcodePrefabHandler : IPoolNetworkBackend
    {
        private readonly NetcodeMgr _netcodeMgr;
        private readonly HashSet<uint> _registeredPrefabs = new HashSet<uint>();

        public NetcodePrefabHandler(NetcodeMgr netcodeMgr)
        {
            _netcodeMgr = netcodeMgr;
        }

        public void SynchronizeInstance(GameObject instance, uint networkId)
        {
            if (instance == null) return;
            
            var no = instance.GetComponent<NetworkObject>();
            if (no != null && !no.IsSpawned)
            {
                RegisterPrefabHandler(no);
                no.Spawn(true); 
            }
        }

        private void RegisterPrefabHandler(NetworkObject no)
        {
            uint prefabId = no.PrefabIdHash;
            if (_registeredPrefabs.Contains(prefabId)) return;

            if (_netcodeMgr.PrefabHandler != null)
            {
                _netcodeMgr.PrefabHandler.AddHandler(no, new CatalystPrefabHandler(no.gameObject));
                _registeredPrefabs.Add(prefabId);
            }
        }

        private class CatalystPrefabHandler : INetworkPrefabInstanceHandler
        {
            private readonly GameObject _prefab;
            public CatalystPrefabHandler(GameObject prefab) => _prefab = prefab;

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                var handle = App.Get<Pool>().SpawnObject(_prefab, position, rotation);
                return handle.Instance.GetComponent<NetworkObject>();
            }

            public void Destroy(NetworkObject networkObject)
            {
                App.Get<Pool>().DespawnDynamic(networkObject.gameObject);
            }
        }
    }
}
#endif
