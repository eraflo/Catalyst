using UnityEngine;
using Eraflo.Catalyst.Networking;
using Eraflo.Catalyst.Networking.Backends;
using Eraflo.Catalyst.Timers;
using Eraflo.Catalyst.Pooling;
using Eraflo.Catalyst.Events;

namespace Eraflo.Catalyst.Samples.Networking
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
            var nm = App.Get<NetworkManager>();

            // Setup mock backend if needed (normally from PackageSettings)
            if (_useMockBackend && !nm.HasBackend)
            {
                var mock = new MockNetworkBackend(isServer: true, isClient: true, isConnected: true);
                mock.EnableLoopback = true;
                nm.SetBackend(mock);
            }

            // Subscribe to custom messages
            nm.On<ChatMessage>(OnChat);

            Debug.Log("[NetworkingSample] Ready");
        }

        private void OnDestroy()
        {
            App.Get<NetworkManager>().Off<ChatMessage>(OnChat);
        }

        private void OnGUI()
        {
            var nm = App.Get<NetworkManager>();
            var timer = App.Get<Timer>();

            GUILayout.BeginArea(new Rect(10, 10, 250, 350));
            
            GUILayout.Label($"Connected: {nm.IsConnected}");
            GUILayout.Label($"Server: {nm.IsServer} | Client: {nm.IsClient}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create Networked Timer (5s)"))
            {
                var handle = timer.CreateTimer<CountdownTimer>(5f);
                var networkId = handle.MakeNetworked(); // Extension method
                timer.Start(handle);
                Debug.Log($"Created networked timer: {networkId}");
            }
            
            if (_spawnPrefab && GUILayout.Button("Spawn Networked Object"))
            {
                Vector3 pos = Random.insideUnitSphere * 3f;
                var (handle, networkId) = App.Get<Pool>().SpawnNetworked(_spawnPrefab, pos);
                Debug.Log($"Spawned networked object: {networkId}");
            }

            if (_eventChannel && GUILayout.Button("Raise Event"))
            {
                _eventChannel.Raise(); // Auto-uses handler
            }
            
            if (GUILayout.Button("Send Chat"))
            {
                nm.Send(new ChatMessage { Sender = "Player", Text = $"Hi at {Time.time:F1}s" });
            }
            
            if (GUILayout.Button("Sync Timers"))
            {
                TimerNetworkExtensions.BroadcastTimerSync();
            }

            GUILayout.Space(10);
            
            // Debug info
            var timerHandler = nm.Handlers.Get<TimerNetworkHandler>();
            var eventHandler = nm.Handlers.Get<EventNetworkHandler>();
            GUILayout.Label($"TimerHandler: {(timerHandler != null ? "OK" : "NULL")}");
            GUILayout.Label($"EventHandler: {(eventHandler != null ? "OK" : "NULL")}");
            GUILayout.Label($"Timers: {timer.Count}");
            
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
