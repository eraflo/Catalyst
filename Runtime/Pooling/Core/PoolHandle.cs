using System;

namespace Eraflo.UnityImportPackage.Pooling
{
    /// <summary>
    /// Handle to a pooled object. Use this to track and release pooled instances.
    /// </summary>
    /// <typeparam name="T">Type of the pooled object.</typeparam>
    public readonly struct PoolHandle<T> where T : class
    {
        /// <summary>Unique identifier for this pooled instance.</summary>
        public readonly uint Id;

        /// <summary>The pooled object instance.</summary>
        public readonly T Instance;

        /// <summary>Pool identifier (for multi-pool scenarios).</summary>
        public readonly int PoolId;

        /// <summary>Timestamp when spawned.</summary>
        public readonly float SpawnTime;

        /// <summary>Whether this handle is valid.</summary>
        public bool IsValid => Id != 0 && Instance != null;

        /// <summary>Invalid/empty handle.</summary>
        public static PoolHandle<T> None => default;

        public PoolHandle(uint id, T instance, int poolId = 0, float spawnTime = 0f)
        {
            Id = id;
            Instance = instance;
            PoolId = poolId;
            SpawnTime = spawnTime;
        }

        public override bool Equals(object obj)
        {
            return obj is PoolHandle<T> other && Id == other.Id && PoolId == other.PoolId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, PoolId);
        }

        public static bool operator ==(PoolHandle<T> left, PoolHandle<T> right)
        {
            return left.Id == right.Id && left.PoolId == right.PoolId;
        }

        public static bool operator !=(PoolHandle<T> left, PoolHandle<T> right)
        {
            return !(left == right);
        }
    }
}
