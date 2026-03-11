using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Eraflo.Catalyst
{
    /// <summary>
    /// Central registry for all game services.
    /// Handles discovery, lifecycle, and PlayerLoop injection.
    /// </summary>
    public static class ServiceLocator
    {
        // Primary index: concrete type → service instance (O(1) exact-type lookup).
        private static readonly Dictionary<Type, IGameService> _services = new Dictionary<Type, IGameService>();

        // Secondary index: interface / base type → service instance (O(1) interface lookup,
        // replaces the previous O(n) LINQ scan in Get<T> / Get(Type)).
        private static readonly Dictionary<Type, IGameService> _interfaceIndex = new Dictionary<Type, IGameService>();

        private static readonly List<IUpdatable> _updatables = new List<IUpdatable>();
        private static readonly List<IFixedUpdatable> _fixedUpdatables = new List<IFixedUpdatable>();

        // volatile: documents that _initialized may be read from non-main-thread contexts
        // (async continuations, Jobs) and prevents stale-cache reads.
        private static volatile bool _initialized;

        private struct ServiceUpdate { }
        private struct ServiceFixedUpdate { }

        // ──────────────────────────────────────────────────────────────
        //  Initialization
        // ──────────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                DiscoverServices();

                try { InjectIntoPlayerLoop(); }
                catch (Exception e) { Debug.LogWarning($"[ServiceLocator] PlayerLoop injection skipped: {e.Message}"); }

                Debug.Log($"[ServiceLocator] Initialized with {_services.Count} services.");
                Application.quitting += Shutdown;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServiceLocator] Critical error: {e.Message}");
                _initialized = false;
            }
        }

        private static void DiscoverServices()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var serviceTypes = new List<(Type type, ServiceAttribute attr)>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    string name = assembly.GetName().Name;

                    // Skip large system assemblies unless they are part of Eraflo.Catalyst.
                    // StringComparison.Ordinal: byte-level comparison — faster and culturally neutral.
                    if (name.StartsWith("System",      StringComparison.Ordinal) ||
                        name.StartsWith("Unity",       StringComparison.Ordinal) ||
                        name.StartsWith("mscorlib",    StringComparison.Ordinal) ||
                        name.StartsWith("netstandard", StringComparison.Ordinal) ||
                        name.StartsWith("Microsoft",   StringComparison.Ordinal) ||
                        name.StartsWith("Mono",        StringComparison.Ordinal))
                    {
                        if (!name.Contains("Eraflo.Catalyst", StringComparison.Ordinal))
                            continue;
                    }

                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;

                        var attr = type.GetCustomAttribute<ServiceAttribute>();
                        if (attr != null)
                            serviceTypes.Add((type, attr));
                    }
                }
                catch (Exception e)
                {
                    // Surface scan failures in the Editor so assembly/type-load errors are visible.
#if UNITY_EDITOR
                    Debug.LogWarning($"[ServiceLocator] Skipped assembly during scan: {e.GetType().Name} — {e.Message}");
#endif
                }
            }

            // In-place sort avoids the OrderBy LINQ allocation.
            serviceTypes.Sort((a, b) => a.attr.Priority.CompareTo(b.attr.Priority));

            foreach (var (type, _) in serviceTypes)
                Register(type);
        }

        // ──────────────────────────────────────────────────────────────
        //  Registration
        // ──────────────────────────────────────────────────────────────

        internal static void Register(Type type)
        {
            // TryGetValue replaces ContainsKey + indexer (single hash computation).
            if (_services.TryGetValue(type, out _)) return;

            try
            {
                if (Activator.CreateInstance(type) is IGameService service)
                {
                    _services[type] = service;
                    AddToInterfaceIndex(type, service);

                    if (service is IUpdatable updatable)       _updatables.Add(updatable);
                    if (service is IFixedUpdatable fixedUpd)   _fixedUpdatables.Add(fixedUpd);

                    service.Initialize();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServiceLocator] Could not create {type.Name}: {e.Message}");
            }
        }

        public static void Register<T>(T instance) where T : class, IGameService
        {
            if (instance == null)
            {
                Debug.LogError($"[ServiceLocator] Register<{typeof(T).Name}>: instance is null.");
                return;
            }

            var type = typeof(T);

            if (_services.TryGetValue(type, out var existing))
            {
                // Transfer interface index entries from old to new instance
                // (preserves ownership when two services share an interface).
                UpdateInterfaceIndex(type, existing, instance);

                if (existing is IUpdatable oldUpd)   _updatables.Remove(oldUpd);
                if (existing is IFixedUpdatable oldFx) _fixedUpdatables.Remove(oldFx);
            }
            else
            {
                AddToInterfaceIndex(type, instance);
            }

            _services[type] = instance;

            if (instance is IUpdatable updatable)       _updatables.Add(updatable);
            if (instance is IFixedUpdatable fixedUpd)   _fixedUpdatables.Add(fixedUpd);

            instance.Initialize();
        }

        // ──────────────────────────────────────────────────────────────
        //  Retrieval — all lookups are O(1) dictionary hits
        // ──────────────────────────────────────────────────────────────

        public static T Get<T>() where T : class
        {
            if (!_initialized) Initialize();

            if (_services.TryGetValue(typeof(T), out var service))
                return service as T;

            if (_interfaceIndex.TryGetValue(typeof(T), out service))
                return service as T;

            return null;
        }

        public static IGameService Get(Type type)
        {
            if (!_initialized) Initialize();

            if (_services.TryGetValue(type, out var service))
                return service;

            if (_interfaceIndex.TryGetValue(type, out service))
                return service;

            return null;
        }

        // ──────────────────────────────────────────────────────────────
        //  Interface index helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Walks all interfaces of <paramref name="concreteType"/> and inserts entries into
        /// <see cref="_interfaceIndex"/> for any interface not already claimed by another service.
        /// </summary>
        private static void AddToInterfaceIndex(Type concreteType, IGameService service)
        {
            foreach (var iface in concreteType.GetInterfaces())
            {
                if (iface == typeof(IGameService)) continue;
                // First registered service wins for a given interface.
                if (!_interfaceIndex.ContainsKey(iface))
                    _interfaceIndex[iface] = service;
            }
        }

        /// <summary>
        /// When a service is replaced via <see cref="Register{T}(T)"/>, updates every interface
        /// index entry that pointed to <paramref name="oldService"/> to point to <paramref name="newService"/>.
        /// Entries owned by a different service are left unchanged.
        /// </summary>
        private static void UpdateInterfaceIndex(Type concreteType, IGameService oldService, IGameService newService)
        {
            foreach (var iface in concreteType.GetInterfaces())
            {
                if (iface == typeof(IGameService)) continue;
                if (_interfaceIndex.TryGetValue(iface, out var current) && ReferenceEquals(current, oldService))
                    _interfaceIndex[iface] = newService;
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  PlayerLoop
        // ──────────────────────────────────────────────────────────────

        private static void InjectIntoPlayerLoop()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            if (loop.subSystemList == null) return;

            InsertSystem<Update, ServiceUpdate>(ref loop, OnUpdate);
            InsertSystem<FixedUpdate, ServiceFixedUpdate>(ref loop, OnFixedUpdate);

            PlayerLoop.SetPlayerLoop(loop);
        }

        private static void InsertSystem<TLocation, TMarker>(ref PlayerLoopSystem rootLoop, PlayerLoopSystem.UpdateFunction delegateFunction)
        {
            if (rootLoop.subSystemList == null) return;

            for (int i = 0; i < rootLoop.subSystemList.Length; i++)
            {
                if (rootLoop.subSystemList[i].type != typeof(TLocation)) continue;

                var system = rootLoop.subSystemList[i];
                var subsystems = system.subSystemList ?? Array.Empty<PlayerLoopSystem>();

                // Plain for-loop — avoids allocating a LINQ enumerator over the array.
                for (int j = 0; j < subsystems.Length; j++)
                {
                    if (subsystems[j].type == typeof(TMarker)) return; // already inserted
                }

                var newSubsystems = new PlayerLoopSystem[subsystems.Length + 1];
                Array.Copy(subsystems, newSubsystems, subsystems.Length);
                newSubsystems[subsystems.Length] = new PlayerLoopSystem
                {
                    type           = typeof(TMarker),
                    updateDelegate = delegateFunction
                };

                system.subSystemList = newSubsystems;
                rootLoop.subSystemList[i] = system;
                return;
            }
        }

        private static void OnUpdate()
        {
            for (int i = 0; i < _updatables.Count; i++) _updatables[i].OnUpdate();
        }

        private static void OnFixedUpdate()
        {
            for (int i = 0; i < _fixedUpdatables.Count; i++) _fixedUpdatables[i].OnFixedUpdate();
        }

        // ──────────────────────────────────────────────────────────────
        //  Shutdown
        // ──────────────────────────────────────────────────────────────

        public static void Shutdown()
        {
            // Unsubscribe to prevent a second call if Shutdown() is invoked manually
            // before the application actually quits.
            Application.quitting -= Shutdown;

            foreach (var service in _services.Values)
            {
                try { service.Shutdown(); } catch (Exception) { }
            }

            _services.Clear();
            _interfaceIndex.Clear();
            _updatables.Clear();
            _fixedUpdatables.Clear();
            _initialized = false;
        }
    }
}
