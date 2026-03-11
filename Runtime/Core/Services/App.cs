namespace Eraflo.Catalyst
{
    /// <summary>
    /// Static entry point to access and create services and injectable objects.
    /// </summary>
    public static class App
    {
        /// <summary>
        /// Retrieves a service of type T.
        /// </summary>
        /// <typeparam name="T">The type or interface of the service.</typeparam>
        /// <returns>The service instance, or null if not found.</returns>
        public static T Get<T>() where T : class
        {
            return ServiceLocator.Get<T>();
        }

        /// <summary>
        /// Retrieves a service by its runtime type.
        /// </summary>
        public static object Get(System.Type type)
        {
            return ServiceLocator.Get(type);
        }

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> and automatically injects all
        /// fields marked with <see cref="InjectAttribute"/> before returning it.
        /// Use for classes marked with <see cref="InjectableAttribute"/> instead of <c>new T()</c>.
        /// </summary>
        /// <typeparam name="T">A class with a public parameterless constructor.</typeparam>
        public static T Create<T>() where T : class, new()
        {
            return ServiceInjector.Create<T>();
        }

        public static void Register<T>(T instance) where T : class, IGameService
        {
            ServiceLocator.Register<T>(instance);
        }

        public static void Shutdown()
        {
            ServiceLocator.Shutdown();
        }
    }
}
