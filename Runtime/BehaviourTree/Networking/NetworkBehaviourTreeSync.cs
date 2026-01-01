using System.IO;
using UnityEngine;
using Eraflo.Catalyst.Networking;

namespace Eraflo.Catalyst.BehaviourTree
{
    /// <summary>
    /// Synchronizes Behaviour Tree state across the network using the package's networking system.
    /// Attach to agents that need synchronized AI behavior.
    /// </summary>
    [RequireComponent(typeof(BehaviourTreeRunner))]
    public class NetworkBehaviourTreeSync : MonoBehaviour
    {
        [SerializeField] private BehaviourTreeRunner _runner;
        
        /// <summary>If true, only the server/host runs the tree and syncs state to clients.</summary>
        [Tooltip("If true, only the server/host evaluates the tree. Clients receive state updates.")]
        public bool ServerAuthoritative = true;
        
        /// <summary>How often to sync blackboard values (in seconds).</summary>
        [Tooltip("Sync interval for blackboard data. Lower = more traffic.")]
        public float BlackboardSyncInterval = 0.5f;
        
        private float _lastSyncTime;
        private bool _isRegistered;
        
        private void Awake()
        {
            if (_runner == null)
            {
                _runner = GetComponent<BehaviourTreeRunner>();
            }
        }
        
        private void OnEnable()
        {
            // Register network message handlers
            if (!_isRegistered)
            {
                NetworkManager.On<BlackboardSyncMessage>(OnBlackboardSync);
                NetworkManager.On<TreeStateMessage>(OnTreeStateSync);
                _isRegistered = true;
            }
        }
        
        private void OnDisable()
        {
            if (_isRegistered)
            {
                NetworkManager.Off<BlackboardSyncMessage>(OnBlackboardSync);
                NetworkManager.Off<TreeStateMessage>(OnTreeStateSync);
                _isRegistered = false;
            }
        }
        
        private void Update()
        {
            if (!NetworkManager.IsConnected) return;
            
            // Only server/host evaluates in authoritative mode
            if (ServerAuthoritative && !NetworkManager.IsServer)
            {
                return;
            }
            
            // Sync blackboard periodically
            if (NetworkManager.IsServer && Time.time - _lastSyncTime >= BlackboardSyncInterval)
            {
                _lastSyncTime = Time.time;
                SyncBlackboardToClients();
            }
        }
        
        /// <summary>
        /// Syncs a specific blackboard value to all clients.
        /// </summary>
        public void SyncBlackboardInt(string key, int value)
        {
            if (!NetworkManager.IsServer) return;
            
            var msg = new BlackboardSyncMessage
            {
                Key = key,
                ValueType = ValueTypeId.Int,
                IntValue = value
            };
            
            NetworkManager.SendToClients(msg);
        }
        
        /// <summary>
        /// Syncs tree state to all clients.
        /// </summary>
        public void SyncTreeState(NodeState state)
        {
            if (!NetworkManager.IsServer) return;
            
            var msg = new TreeStateMessage
            {
                State = (int)state
            };
            
            NetworkManager.SendToClients(msg);
        }
        
        private void SyncBlackboardToClients()
        {
            if (_runner?.Blackboard == null) return;
            
            var keys = _runner.Blackboard.GetAllKeys();
            foreach (var key in keys)
            {
                if (_runner.Blackboard.TryGet<int>(key, out int intVal))
                {
                    SyncBlackboardInt(key, intVal);
                }
            }
        }
        
        private void OnBlackboardSync(BlackboardSyncMessage msg)
        {
            // Clients apply received values
            if (NetworkManager.IsServer) return;
            
            switch (msg.ValueType)
            {
                case ValueTypeId.Int:
                    _runner?.Blackboard?.Set(msg.Key, msg.IntValue);
                    break;
                case ValueTypeId.Float:
                    _runner?.Blackboard?.Set(msg.Key, msg.FloatValue);
                    break;
                case ValueTypeId.Bool:
                    _runner?.Blackboard?.Set(msg.Key, msg.BoolValue);
                    break;
            }
        }
        
        private void OnTreeStateSync(TreeStateMessage msg)
        {
            if (NetworkManager.IsServer) return;
            
            Debug.Log($"[NetworkBT] Tree state: {(NodeState)msg.State}");
        }
        
        #region Network Messages
        
        public enum ValueTypeId : byte
        {
            Int,
            Float,
            Bool,
            String,
            Vector3
        }
        
        public struct BlackboardSyncMessage : INetworkMessage
        {
            public string Key;
            public ValueTypeId ValueType;
            public int IntValue;
            public float FloatValue;
            public bool BoolValue;
            
            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Key ?? "");
                writer.Write((byte)ValueType);
                writer.Write(IntValue);
                writer.Write(FloatValue);
                writer.Write(BoolValue);
            }
            
            public void Deserialize(BinaryReader reader)
            {
                Key = reader.ReadString();
                ValueType = (ValueTypeId)reader.ReadByte();
                IntValue = reader.ReadInt32();
                FloatValue = reader.ReadSingle();
                BoolValue = reader.ReadBoolean();
            }
        }
        
        public struct TreeStateMessage : INetworkMessage
        {
            public int State;
            
            public void Serialize(BinaryWriter writer)
            {
                writer.Write(State);
            }
            
            public void Deserialize(BinaryReader reader)
            {
                State = reader.ReadInt32();
            }
        }
        
        #endregion
    }
}
