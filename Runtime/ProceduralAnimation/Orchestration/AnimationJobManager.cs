using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Eraflo.Catalyst.ProceduralAnimation.Jobs;

namespace Eraflo.Catalyst.ProceduralAnimation
{
    /// <summary>
    /// Manages scheduling and execution of procedural animation jobs.
    /// Provides an extensible system for registering custom job types.
    /// </summary>
    public class AnimationJobManager : IDisposable
    {
        private readonly List<IProceduralAnimationJob> _jobs = new List<IProceduralAnimationJob>();
        private readonly Dictionary<Type, List<IProceduralAnimationJob>> _jobsByType = new Dictionary<Type, List<IProceduralAnimationJob>>();
        private readonly object _lock = new object();
        
        private JobHandle _lastJobHandle;
        private bool _isDisposed;
        
        /// <summary>
        /// Singleton instance of the job manager.
        /// </summary>
        public static AnimationJobManager Instance { get; private set; }
        
        /// <summary>
        /// Number of registered jobs.
        /// </summary>
        public int JobCount
        {
            get
            {
                lock (_lock) return _jobs.Count;
            }
        }
        
        /// <summary>
        /// Initializes the job manager singleton.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Instance?.Dispose();
            Instance = new AnimationJobManager();
            
            // Register with the animation loop
            ProceduralAnimationLoop.RegisterUpdate(Instance.Update);
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                {
                    Instance?.Dispose();
                    Instance = null;
                }
            };
        }
#endif
        
        /// <summary>
        /// Registers a job with the manager.
        /// </summary>
        /// <typeparam name="T">Job type for categorization.</typeparam>
        /// <param name="job">The job to register.</param>
        public void Register<T>(T job) where T : IProceduralAnimationJob
        {
            if (job == null) return;
            
            lock (_lock)
            {
                _jobs.Add(job);
                
                var type = typeof(T);
                if (!_jobsByType.TryGetValue(type, out var list))
                {
                    list = new List<IProceduralAnimationJob>();
                    _jobsByType[type] = list;
                }
                list.Add(job);
            }
        }
        
        /// <summary>
        /// Unregisters a job from the manager.
        /// </summary>
        public void Unregister(IProceduralAnimationJob job)
        {
            if (job == null) return;
            
            lock (_lock)
            {
                _jobs.Remove(job);
                
                foreach (var kvp in _jobsByType)
                {
                    kvp.Value.Remove(job);
                }
            }
        }
        
        /// <summary>
        /// Gets all jobs of a specific type.
        /// </summary>
        public IReadOnlyList<IProceduralAnimationJob> GetJobs<T>() where T : IProceduralAnimationJob
        {
            lock (_lock)
            {
                if (_jobsByType.TryGetValue(typeof(T), out var list))
                    return list;
                return Array.Empty<IProceduralAnimationJob>();
            }
        }
        
        /// <summary>
        /// Updates all registered jobs.
        /// Called automatically by the animation loop.
        /// </summary>
        private void Update(float deltaTime)
        {
            if (_isDisposed) return;
            
            // Complete any pending jobs from last frame
            _lastJobHandle.Complete();
            
            IProceduralAnimationJob[] snapshot;
            lock (_lock)
            {
                snapshot = _jobs.ToArray();
            }
            
            // Prepare all jobs
            foreach (var job in snapshot)
            {
                if (job.NeedsUpdate)
                {
                    job.Prepare(deltaTime);
                }
            }
            
            // Schedule all jobs with dependencies
            JobHandle combinedHandle = default;
            foreach (var job in snapshot)
            {
                if (job.NeedsUpdate)
                {
                    var handle = job.Schedule(combinedHandle);
                    combinedHandle = JobHandle.CombineDependencies(combinedHandle, handle);
                }
            }
            
            // Complete all jobs
            combinedHandle.Complete();
            
            // Apply results
            foreach (var job in snapshot)
            {
                if (job.NeedsUpdate)
                {
                    job.Apply();
                }
            }
            
            _lastJobHandle = combinedHandle;
        }
        
        /// <summary>
        /// Forces completion of all pending jobs.
        /// </summary>
        public void CompleteAll()
        {
            _lastJobHandle.Complete();
        }
        
        /// <summary>
        /// Clears all registered jobs.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (var job in _jobs)
                {
                    job.Dispose();
                }
                _jobs.Clear();
                _jobsByType.Clear();
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            ProceduralAnimationLoop.UnregisterUpdate(Update);
            
            _lastJobHandle.Complete();
            Clear();
        }
    }
}
