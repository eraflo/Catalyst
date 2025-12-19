using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Eraflo.UnityImportPackage.Networking.Backends;

namespace Eraflo.UnityImportPackage.Networking
{
    /// <summary>
    /// Bootstraps the network system at startup.
    /// </summary>
    public static class NetworkBootstrapper
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBuiltInFactories()
        {
            NetworkManager.Backends.Register(new MockBackendFactory());
            
            #if UNITY_NETCODE
            NetworkManager.Backends.Register(new NetcodeBackendFactory());
            #endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var settings = PackageSettings.Instance;
            
            // Always initialize handlers (for manual backend setup later)
            InitializeHandlers(settings);
            
            // Only auto-initialize backend if configured
            if (settings.EnableNetworking)
            {
                InitializeBackend(settings.NetworkBackendId);
            }
        }

        public static bool InitializeBackend(string backendId)
        {
            if (string.IsNullOrEmpty(backendId))
            {
                NetworkManager.ClearBackend();
                return true;
            }

            var factory = NetworkManager.Backends.Get(backendId);
            if (factory == null)
            {
                Debug.LogWarning($"[NetworkBootstrapper] Unknown backend: {backendId}");
                return false;
            }

            return factory.OnInitialize();
        }

        public static void InitializeHandlers(PackageSettings settings)
        {
            var handlerTypes = FindAllHandlerTypes();

            if (settings.HandlerMode == NetworkHandlerMode.Auto)
            {
                foreach (var type in handlerTypes)
                    RegisterHandler(type);
            }
            else
            {
                foreach (var typeName in settings.EnabledHandlers)
                {
                    var type = handlerTypes.FirstOrDefault(t => t.FullName == typeName);
                    if (type != null) RegisterHandler(type);
                }
            }
        }

        public static List<Type> FindAllHandlerTypes()
        {
            var types = new List<Type>();
            var iface = typeof(INetworkMessageHandler);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (iface.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface 
                            && t.GetConstructor(Type.EmptyTypes) != null)
                        {
                            types.Add(t);
                        }
                    }
                }
                catch { }
            }
            return types;
        }

        private static void RegisterHandler(Type type)
        {
            try
            {
                var handler = (INetworkMessageHandler)Activator.CreateInstance(type);
                NetworkManager.Handlers.Register(handler);
                
                if (PackageSettings.Instance.NetworkDebugMode)
                    Debug.Log($"[NetworkBootstrapper] Registered: {type.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkBootstrapper] Failed: {type.Name} - {e.Message}");
            }
        }

        public static void Reset() => _initialized = false;
    }
}
