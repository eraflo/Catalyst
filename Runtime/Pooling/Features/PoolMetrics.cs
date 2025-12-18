namespace Eraflo.UnityImportPackage.Pooling
{
    /// <summary>
    /// Tracks pool system metrics for debugging and profiling.
    /// Access via Pool.Metrics.
    /// </summary>
    public class PoolMetrics
    {
        /// <summary>Total objects spawned since startup or reset.</summary>
        public int TotalSpawned { get; private set; }

        /// <summary>Total objects despawned since startup or reset.</summary>
        public int TotalDespawned { get; private set; }

        /// <summary>Peak number of simultaneously active objects.</summary>
        public int PeakActiveCount { get; private set; }

        private int _currentActive;

        /// <summary>Current number of active objects.</summary>
        public int ActiveCount => _currentActive;

        /// <summary>Records a spawn event.</summary>
        internal void RecordSpawn()
        {
            TotalSpawned++;
            _currentActive++;
            
            if (_currentActive > PeakActiveCount)
                PeakActiveCount = _currentActive;
        }

        /// <summary>Records a despawn event.</summary>
        internal void RecordDespawn()
        {
            TotalDespawned++;
            _currentActive--;
            
            if (_currentActive < 0) 
                _currentActive = 0; // Safety check
        }

        /// <summary>Resets all metrics.</summary>
        public void Reset()
        {
            TotalSpawned = 0;
            TotalDespawned = 0;
            PeakActiveCount = 0;
            _currentActive = 0;
        }

        public override string ToString()
        {
            return $"Spawned: {TotalSpawned}, Despawned: {TotalDespawned}, Active: {ActiveCount}, Peak: {PeakActiveCount}";
        }
    }
}
