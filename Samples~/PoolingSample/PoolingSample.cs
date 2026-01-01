using UnityEngine;
using Eraflo.Catalyst.Pooling;

namespace Eraflo.Catalyst.Samples.Pooling
{
    /// <summary>
    /// Sample demonstrating the Object Pooling system.
    /// Attach to any GameObject in the scene.
    /// </summary>
    public class PoolingSample : MonoBehaviour
    {
        [Header("Prefab (assign a simple cube or sphere)")]
        [SerializeField] private GameObject prefab;

        [Header("Settings")]
        [SerializeField] private int warmupCount = 10;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private float timedSpawnDuration = 2f;

        private void Start()
        {
            // Create a default prefab if none assigned
            if (prefab == null)
            {
                prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                prefab.transform.localScale = Vector3.one * 0.5f;
                prefab.SetActive(false);
                prefab.name = "PooledSphere";
            }

            Debug.Log("[Pooling Sample] Started. Use UI buttons to test pooling.");
        }

        public void Warmup()
        {
            Pool.Warmup(prefab, warmupCount);
            Debug.Log($"<color=cyan>[POOL]</color> Warmed up {warmupCount} objects");
        }

        public void SpawnOne()
        {
            var pos = transform.position + Random.insideUnitSphere * spawnRadius;
            pos.y = Mathf.Max(pos.y, 0.5f);
            
            var handle = Pool.Spawn(prefab, pos);
            Debug.Log($"<color=green>[POOL]</color> Spawned at {pos}");
        }

        public void SpawnTimed()
        {
            var pos = transform.position + Random.insideUnitSphere * spawnRadius;
            pos.y = Mathf.Max(pos.y, 0.5f);
            
            Pool.SpawnTimed(prefab, pos, timedSpawnDuration);
            Debug.Log($"<color=yellow>[POOL]</color> Spawned timed ({timedSpawnDuration}s) at {pos}");
        }

        public void SpawnBurst()
        {
            for (int i = 0; i < 10; i++)
            {
                var pos = transform.position + Random.insideUnitSphere * spawnRadius;
                pos.y = Mathf.Max(pos.y, 0.5f);
                Pool.SpawnTimed(prefab, pos, Random.Range(1f, 3f));
            }
            Debug.Log("<color=magenta>[POOL]</color> Spawned burst of 10 objects");
        }

        public void DespawnAll()
        {
            Pool.Clear(prefab);
            Debug.Log("<color=red>[POOL]</color> Cleared pool");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 250, 300));
            GUILayout.Box("Pooling Sample");

            if (GUILayout.Button("Warmup (10 objects)")) Warmup();
            GUILayout.Space(5);

            if (GUILayout.Button("Spawn One")) SpawnOne();
            if (GUILayout.Button($"Spawn Timed ({timedSpawnDuration}s)")) SpawnTimed();
            if (GUILayout.Button("Spawn Burst (10)")) SpawnBurst();
            GUILayout.Space(5);

            if (GUILayout.Button("Despawn All")) DespawnAll();
            GUILayout.Space(10);

            // Metrics
            var m = Pool.Metrics;
            GUILayout.Label($"Spawned: {m.TotalSpawned}");
            GUILayout.Label($"Despawned: {m.TotalDespawned}");
            GUILayout.Label($"Active: {m.ActiveCount}");
            GUILayout.Label($"Peak: {m.PeakActiveCount}");

            GUILayout.EndArea();
        }
    }
}
