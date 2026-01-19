using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly Dictionary<Type, IGameService> _services = new Dictionary<Type, IGameService>();
        private static readonly List<IUpdatable> _updatables = new List<IUpdatable>();
        private static readonly List<IFixedUpdatable> _fixedUpdatables = new List<IFixedUpdatable>();

        private static bool _initialized;

        private struct ServiceUpdate { }
        private struct ServiceFixedUpdate { }

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

                    // Skip large system assemblies unless they contain Eraflo.Catalyst
                    if (name.StartsWith("System") || name.StartsWith("Unity") || name.StartsWith("mscorlib") ||
                        name.StartsWith("netstandard") || name.StartsWith("Microsoft") || name.StartsWith("Mono"))
                    {
                        if (!name.Contains("Eraflo.Catalyst"))
                            continue;
                    }

                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.IsAbstract || type.IsInterface) continue;

                        var attr = type.GetCustomAttribute<ServiceAttribute>();
                        if (attr != null)
                        {
                            serviceTypes.Add((type, attr));
                        }
                    }
                }
                catch (Exception)
                {
                    // Swallowing scan errors for third-party/system assemblies
                }
            }

            foreach (var (type, attr) in serviceTypes.OrderBy(s => s.attr.Priority))
            {
                Register(type);
            }
        }

        internal static void Register(Type type)
        {
            if (_services.ContainsKey(type)) return;

            try
            {
                if (Activator.CreateInstance(type) is IGameService service)
                {
                    _services[type] = service;

                    if (service is IUpdatable updatable) _updatables.Add(updatable);
                    if (service is IFixedUpdatable fixedUpdatable) _fixedUpdatables.Add(fixedUpdatable);

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
            var type = typeof(T);

            // Remove existing instance from all collections
            if (_services.TryGetValue(type, out var existing))
            {
                if (existing is IUpdatable oldUpdatable) _updatables.Remove(oldUpdatable);
                if (existing is IFixedUpdatable oldFixed) _fixedUpdatables.Remove(oldFixed);
                _services.Remove(type);
            }

            _services[type] = instance;
            if (instance is IUpdatable updatable) _updatables.Add(updatable);
            if (instance is IFixedUpdatable fixedUpdatable) _fixedUpdatables.Add(fixedUpdatable);

            instance.Initialize();
        }

        public static T Get<T>() where T : class
        {
            if (!_initialized) Initialize();

            if (_services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }

            return _services.Values.OfType<T>().FirstOrDefault();
        }

        public static IGameService Get(Type type)
        {
            if (!_initialized) Initialize();

            if (_services.TryGetValue(type, out var service))
            {
                return service;
            }

            return _services.Values.FirstOrDefault(s => type.IsAssignableFrom(s.GetType()));
        }

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
                if (rootLoop.subSystemList[i].type == typeof(TLocation))
                {
                    var system = rootLoop.subSystemList[i];
                    var subsystems = system.subSystemList ?? Array.Empty<PlayerLoopSystem>();

                    if (subsystems.Any(s => s.type == typeof(TMarker))) return;

                    var newSubsystems = new PlayerLoopSystem[subsystems.Length + 1];
                    Array.Copy(subsystems, newSubsystems, subsystems.Length);
                    newSubsystems[subsystems.Length] = new PlayerLoopSystem
                    {
                        type = typeof(TMarker),
                        updateDelegate = delegateFunction
                    };

                    system.subSystemList = newSubsystems;
                    rootLoop.subSystemList[i] = system;
                    return;
                }
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

        public static void Shutdown()
        {
            foreach (var service in _services.Values)
            {
                try { service.Shutdown(); } catch (Exception) { }
            }

            _services.Clear();
            _updatables.Clear();
            _fixedUpdatables.Clear();
            _initialized = false;
        }
    }
}
