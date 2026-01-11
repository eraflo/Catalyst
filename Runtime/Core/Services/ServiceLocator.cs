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

        /// <summary>
        /// Marker struct for the Service Locator update in the Player Loop.
        /// </summary>
        private struct ServiceUpdate { }
        private struct ServiceFixedUpdate { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            if (_initialized) return;

            // 1. Discover and Register Services
            DiscoverServices();

            // 2. Inject into Player Loop
            InjectIntoPlayerLoop();

            _initialized = true;
            Debug.Log($"[ServiceLocator] Initialized with {_services.Count} services.");

            // 3. Application Lifecycle
            Application.quitting += Shutdown;
        }

        private static void DiscoverServices()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var serviceTypes = new List<(Type type, ServiceAttribute attr)>();

            foreach (var assembly in assemblies)
            {
                // Optimization: Only scan our own assemblies or those that might have services
                if (assembly.FullName.StartsWith("Unity") || assembly.FullName.StartsWith("System") || assembly.FullName.StartsWith("mscorlib"))
                    continue;

                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var attr = type.GetCustomAttribute<ServiceAttribute>();
                        if (attr != null)
                        {
                            serviceTypes.Add((type, attr));
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                }
            }

            // Sort by priority
            var sortedServices = serviceTypes.OrderBy(s => s.attr.Priority);

            foreach (var (type, attr) in sortedServices)
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
                Debug.LogError($"[ServiceLocator] Failed to instantiate service {type.Name}: {e.Message}");
            }
        }

        internal static void Register<T>(T instance) where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.ContainsKey(type)) _services.Remove(type);

            _services[type] = instance;
            if (instance is IUpdatable updatable) _updatables.Add(updatable);
            if (instance is IFixedUpdatable fixedUpdatable) _fixedUpdatables.Add(fixedUpdatable);
            
            instance.Initialize();
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }

            // Fallback for interfaces
            return _services.Values.OfType<T>().FirstOrDefault();
        }

        private static void InjectIntoPlayerLoop()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            
            InsertSystem<Update, ServiceUpdate>(ref loop, OnUpdate);
            InsertSystem<FixedUpdate, ServiceFixedUpdate>(ref loop, OnFixedUpdate);

            PlayerLoop.SetPlayerLoop(loop);
        }

        private static void InsertSystem<TLocation, TMarker>(ref PlayerLoopSystem rootLoop, PlayerLoopSystem.UpdateFunction delegateFunction)
        {
            for (int i = 0; i < rootLoop.subSystemList.Length; i++)
            {
                if (rootLoop.subSystemList[i].type == typeof(TLocation))
                {
                    var system = rootLoop.subSystemList[i];
                    var subsystems = system.subSystemList ?? Array.Empty<PlayerLoopSystem>();

                    // Already inserted?
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
            for (int i = 0; i < _updatables.Count; i++)
            {
                _updatables[i].OnUpdate();
            }
        }

        private static void OnFixedUpdate()
        {
            for (int i = 0; i < _fixedUpdatables.Count; i++)
            {
                _fixedUpdatables[i].OnFixedUpdate();
            }
        }

        internal static void Shutdown()
        {
            foreach (var service in _services.Values)
            {
                try { service.Shutdown(); } catch (Exception e) { Debug.LogException(e); }
            }
            
            _services.Clear();
            _updatables.Clear();
            _fixedUpdatables.Clear();
            _initialized = false;
        }
    }
}
