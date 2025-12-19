using UnityEngine;
using Eraflo.UnityImportPackage.Networking;
using Eraflo.UnityImportPackage.Networking.Backends;
using Eraflo.UnityImportPackage.Timers;
using Eraflo.UnityImportPackage.Pooling;
using Eraflo.UnityImportPackage.Events;

namespace Eraflo.UnityImportPackage.Samples.Networking
{
    /// <summary>
    /// Demonstrates the unified networking system.
    /// Handlers are auto-registered via PackageSettings.
    /// Objects auto-register with handlers.
    /// </summary>
    public class NetworkingSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _useMockBackend = true;
        [SerializeField] private GameObject _spawnPrefab;
        [SerializeField] private NetworkEventChannel _eventChannel;

        private void Start()
        {
            // Setup mock backend if needed (normally from PackageSettings)
            if (_useMockBackend && !NetworkManager.HasBackend)
            {
                var mock = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
                mock.EnableLoopback = true;
                NetworkManager.SetBackend(mock);
            }

            // Subscribe to custom messages
            NetworkManager.On<ChatMessage>(OnChat);

            // Event channel auto-registers on OnEnable
            // Timer/Pool use extension methods

            Debug.Log("[NetworkingSample] Ready");
        }

        private void OnDestroy()
        {
            NetworkManager.Off<ChatMessage>(OnChat);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 250, 350));
            
            GUILayout.Label($"Connected: {NetworkManager.IsConnected}");
            GUILayout.Label($"Server: {NetworkManager.IsServer} | Client: {NetworkManager.IsClient}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create Networked Timer (5s)"))
            {
                var handle = Timer.Create<CountdownTimer>(5f);
                var networkId = handle.MakeNetworked(); // Extension method
                Timer.Start(handle);
                Debug.Log($"Created networked timer: {networkId}");
            }
            
            if (_spawnPrefab && GUILayout.Button("Spawn Networked Object"))
            {
                var pos = Random.insideUnitSphere * 3f;
                var (handle, networkId) = PoolNetworkExtensions.SpawnNetworked(_spawnPrefab, pos);
                Debug.Log($"Spawned networked object: {networkId}");
            }

            if (_eventChannel && GUILayout.Button("Raise Event"))
            {
                _eventChannel.Raise(); // Auto-uses handler
            }
            
            if (GUILayout.Button("Send Chat"))
            {
                NetworkManager.Send(new ChatMessage { Sender = "Player", Text = $"Hi at {Time.time:F1}s" });
            }
            
            if (GUILayout.Button("Sync Timers"))
            {
                TimerNetworkExtensions.BroadcastTimerSync();
            }

            GUILayout.Space(10);
            
            // Debug info
            var timerHandler = NetworkManager.Handlers.Get<TimerNetworkHandler>();
            var eventHandler = NetworkManager.Handlers.Get<EventNetworkHandler>();
            GUILayout.Label($"TimerHandler: {(timerHandler != null ? "OK" : "NULL")}");
            GUILayout.Label($"EventHandler: {(eventHandler != null ? "OK" : "NULL")}");
            GUILayout.Label($"Timers: {Timer.Count}");
            
            GUILayout.EndArea();
        }

        private void OnChat(ChatMessage msg) => Debug.Log($"[Chat] {msg.Sender}: {msg.Text}");
    }

    public struct ChatMessage : INetworkMessage
    {
        public string Sender;
        public string Text;

        public void Serialize(System.IO.BinaryWriter w)
        {
            w.Write(Sender ?? "");
            w.Write(Text ?? "");
        }

        public void Deserialize(System.IO.BinaryReader r)
        {
            Sender = r.ReadString();
            Text = r.ReadString();
        }
    }
}
