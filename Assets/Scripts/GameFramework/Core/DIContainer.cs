using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GameFramework.Core
{
    /// <summary>
    /// Enhanced dependency injection container with automatic constructor injection support.
    /// Supports singleton registration, factory registration, type bindings, and automatic dependency resolution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This DI container provides three main registration patterns:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Singleton:</strong> One instance created and reused for all requests</description></item>
    /// <item><description><strong>Transient:</strong> New instance created for each request</description></item>
    /// <item><description><strong>Factory:</strong> Custom factory function called for each request</description></item>
    /// </list>
    /// <para>
    /// The container automatically resolves constructor dependencies using reflection,
    /// selecting the constructor with the most parameters and recursively resolving all dependencies.
    /// Circular dependency detection prevents infinite resolution loops.
    /// </para>
    /// <para>
    /// <strong>Design Benefits:</strong> Clean separation of concerns, automatic dependency resolution, easy testing, loose coupling<br/>
    /// <strong>Trade-offs:</strong> Runtime dependency resolution, potential circular dependencies, reflection overhead
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register services
    /// container.RegisterSingleton&lt;ILogger, ConsoleLogger&gt;();
    /// container.RegisterTransient&lt;IRepository, DatabaseRepository&gt;();
    /// container.RegisterFactory&lt;IConnection&gt;(() => new SqlConnection(connectionString));
    /// 
    /// // Resolve services (automatic constructor injection)
    /// var service = container.Resolve&lt;IMyService&gt;();
    /// </code>
    /// </example>
    public class DIContainer
    {
        #region Singleton Implementation

        /// <summary>
        /// Private static instance for singleton pattern.
        /// </summary>
        private static DIContainer _instance;
        
        /// <summary>
        /// Gets the singleton instance of the DI container.
        /// </summary>
        /// <value>The singleton DIContainer instance.</value>
        /// <remarks>
        /// Creates a new instance on first access. The container is designed to be used
        /// as a singleton to maintain consistent service registrations across the application.
        /// </remarks>
        public static DIContainer Instance => _instance ??= new DIContainer();

        #endregion

        #region Private Fields

        /// <summary>
        /// Storage for singleton instances that are reused across requests.
        /// </summary>
        /// <remarks>
        /// Maps service types to their singleton instances. Once created, these instances
        /// are cached and returned for all subsequent resolution requests.
        /// </remarks>
        private readonly Dictionary<Type, object> _singletons = new();
        
        /// <summary>
        /// Storage for factory functions that create new instances on each request.
        /// </summary>
        /// <remarks>
        /// Maps service types to factory functions. These functions are called every time
        /// the service is requested, allowing for custom creation logic and fresh instances.
        /// </remarks>
        private readonly Dictionary<Type, Func<object>> _factories = new();
        
        /// <summary>
        /// Storage for type bindings that map interfaces to implementation types.
        /// </summary>
        /// <remarks>
        /// Maps service interfaces to their concrete implementation types. Used for
        /// singleton registrations where the instance is created on first request.
        /// </remarks>
        private readonly Dictionary<Type, Type> _bindings = new();
        
        /// <summary>
        /// Stack tracking current resolution chain to prevent circular dependencies.
        /// </summary>
        /// <remarks>
        /// Maintains a set of types currently being resolved. If a type appears twice
        /// in the resolution chain, a circular dependency is detected and an exception is thrown.
        /// </remarks>
        private readonly HashSet<Type> _resolutionStack = new();

        #endregion

        #region Registration Methods - Singleton

        /// <summary>
        /// Registers a pre-created singleton instance that will be returned for all requests of type T.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="instance">The singleton instance to register.</param>
        /// <remarks>
        /// Use this method when you have a pre-configured instance that should be shared
        /// across the entire application. The instance will not be recreated.
        /// </remarks>
        /// <example>
        /// <code>
        /// var logger = new FileLogger("/logs/app.log");
        /// container.RegisterSingleton&lt;ILogger&gt;(logger);
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Thrown when instance is null.</exception>
        public void RegisterSingleton<T>(T instance) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
                
            _singletons[typeof(T)] = instance;
        }
        
        /// <summary>
        /// Registers a singleton mapping from interface to implementation type.
        /// The instance will be created on first request using constructor injection.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
        /// <remarks>
        /// The implementation instance is created lazily on first request and then cached
        /// for all subsequent requests. Constructor dependencies are automatically resolved.
        /// </remarks>
        /// <example>
        /// <code>
        /// container.RegisterSingleton&lt;IUserService, DatabaseUserService&gt;();
        /// // First call creates instance, subsequent calls return cached instance
        /// var service1 = container.Resolve&lt;IUserService&gt;();
        /// var service2 = container.Resolve&lt;IUserService&gt;(); // Same instance as service1
        /// </code>
        /// </example>
        public void RegisterSingleton<TInterface, TImplementation>() 
            where TImplementation : class, TInterface
        {
            _bindings[typeof(TInterface)] = typeof(TImplementation);
        }
        
        /// <summary>
        /// Registers a singleton for a concrete type (no interface mapping).
        /// </summary>
        /// <typeparam name="T">The concrete type to register as singleton.</typeparam>
        /// <remarks>
        /// Convenience method for registering concrete types as singletons when no
        /// interface abstraction is needed.
        /// </remarks>
        /// <example>
        /// <code>
        /// container.RegisterSingleton&lt;GameManager&gt;();
        /// </code>
        /// </example>
        public void RegisterSingleton<T>() where T : class
        {
            RegisterSingleton<T, T>();
        }

        #endregion

        #region Registration Methods - Factory and Transient

        /// <summary>
        /// Registers a factory function that creates new instances on each resolution request.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="factory">The factory function that creates instances of T.</param>
        /// <remarks>
        /// Use factory registration when you need custom creation logic, configuration,
        /// or when each request should receive a fresh instance with specific setup.
        /// The factory function is called every time the service is resolved.
        /// </remarks>
        /// <example>
        /// <code>
        /// container.RegisterFactory&lt;IDatabase&gt;(() => new SqlDatabase(GetConnectionString()));
        /// container.RegisterFactory&lt;IHttpClient&gt;(() => new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
        public void RegisterFactory<T>(Func<T> factory) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
                
            _factories[typeof(T)] = () => factory();
        }
        
        /// <summary>
        /// Registers a transient type binding where a new instance is created for each request.
        /// </summary>
        /// <typeparam name="TInterface">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
        /// <remarks>
        /// Transient services create a new instance every time they are requested.
        /// Use this for stateless services or when you need isolated instances.
        /// Constructor dependencies are automatically resolved for each new instance.
        /// </remarks>
        /// <example>
        /// <code>
        /// container.RegisterTransient&lt;IEmailService, SmtpEmailService&gt;();
        /// // Each resolution creates a new instance
        /// var email1 = container.Resolve&lt;IEmailService&gt;();
        /// var email2 = container.Resolve&lt;IEmailService&gt;(); // Different instance than email1
        /// </code>
        /// </example>
        public void RegisterTransient<TInterface, TImplementation>() 
            where TImplementation : class, TInterface
        {
            _factories[typeof(TInterface)] = () => CreateInstance(typeof(TImplementation));
        }

        #endregion

        #region Resolution Methods

        /// <summary>
        /// Resolves a service instance of type T using automatic constructor injection.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>An instance of type T with all dependencies injected.</returns>
        /// <remarks>
        /// This is the primary method for retrieving services from the container.
        /// The container automatically resolves constructor dependencies recursively.
        /// Resolution order: singleton instances → factories → type bindings → direct creation.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Assuming UserService has constructor: UserService(ILogger logger, IDatabase db)
        /// var userService = container.Resolve&lt;IUserService&gt;();
        /// // Logger and Database are automatically injected
        /// </code>
        /// </example>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the service is not registered and cannot be created directly,
        /// or when a circular dependency is detected.
        /// </exception>
        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }
        
        /// <summary>
        /// Resolves a service instance by type using automatic constructor injection.
        /// </summary>
        /// <param name="type">The service type to resolve.</param>
        /// <returns>An instance of the specified type with all dependencies injected.</returns>
        /// <remarks>
        /// <para>
        /// This method handles the core resolution logic:
        /// </para>
        /// <list type="number">
        /// <item><description>Checks for circular dependencies</description></item>
        /// <item><description>Returns existing singleton if available</description></item>
        /// <item><description>Calls factory function if registered</description></item>
        /// <item><description>Creates instance from type binding</description></item>
        /// <item><description>Attempts direct creation for concrete types</description></item>
        /// </list>
        /// <para>
        /// Uses reflection to find the constructor with the most parameters and recursively
        /// resolves all constructor dependencies.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when circular dependency is detected or no registration is found for the type.
        /// </exception>
        public object Resolve(Type type)
        {
            // Prevent circular dependencies
            if (_resolutionStack.Contains(type))
            {
                var dependencyChain = string.Join(" -> ", _resolutionStack.Select(t => t.Name));
                throw new InvalidOperationException($"Circular dependency detected for type {type.Name}. Resolution chain: {dependencyChain} -> {type.Name}");
            }
            
            _resolutionStack.Add(type);
            
            try
            {
                // Check for existing singleton
                if (_singletons.TryGetValue(type, out var singleton))
                    return singleton;
                    
                // Check for factory
                if (_factories.TryGetValue(type, out var factory))
                    return factory();
                    
                // Check for type binding
                if (_bindings.TryGetValue(type, out var implementationType))
                {
                    var instance = CreateInstance(implementationType);
                    
                    // Store as singleton if it was registered as one
                    if (_bindings.ContainsKey(type))
                    {
                        _singletons[type] = instance;
                    }
                    
                    return instance;
                }
                
                // Try to create directly if it's a concrete type
                if (!type.IsInterface && !type.IsAbstract)
                {
                    return CreateInstance(type);
                }
                
                throw new InvalidOperationException($"No registration found for type {type.Name}. Please register the service before attempting to resolve it.");
            }
            finally
            {
                _resolutionStack.Remove(type);
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Checks if a service type T is registered in the container.
        /// </summary>
        /// <typeparam name="T">The service type to check.</typeparam>
        /// <returns>True if the type is registered; otherwise, false.</returns>
        /// <remarks>
        /// Use this method to conditionally resolve services or verify registrations
        /// during testing and debugging.
        /// </remarks>
        /// <example>
        /// <code>
        /// if (container.IsRegistered&lt;IOptionalService&gt;())
        /// {
        ///     var service = container.Resolve&lt;IOptionalService&gt;();
        ///     service.DoSomething();
        /// }
        /// </code>
        /// </example>
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }
        
        /// <summary>
        /// Checks if a service type is registered in the container.
        /// </summary>
        /// <param name="type">The service type to check.</param>
        /// <returns>True if the type is registered; otherwise, false.</returns>
        /// <remarks>
        /// Checks all registration types: singleton instances, factories, and type bindings.
        /// </remarks>
        public bool IsRegistered(Type type)
        {
            return _singletons.ContainsKey(type) || 
                   _factories.ContainsKey(type) || 
                   _bindings.ContainsKey(type);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Creates an instance of the specified type using automatic constructor injection.
        /// </summary>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>A new instance with all constructor dependencies resolved.</returns>
        /// <remarks>
        /// <para>
        /// This method uses reflection to analyze the type's constructors and selects
        /// the one with the most parameters (assumes it's the most specific constructor).
        /// All constructor parameters are recursively resolved through the DI container.
        /// </para>
        /// <para>
        /// This approach supports constructor injection patterns where services declare
        /// their dependencies as constructor parameters.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when constructor parameters cannot be resolved or the type cannot be instantiated.
        /// </exception>
        /// <exception cref="TargetInvocationException">
        /// Thrown when the constructor throws an exception during instantiation.
        /// </exception>
        private object CreateInstance(Type type)
        {
            try
            {
                var constructors = type.GetConstructors();
                
                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException($"No public constructors found for type {type.Name}");
                }
                
                // Find the constructor with the most parameters (assumes most specific constructor)
                var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
                
                var parameters = constructor.GetParameters();
                var args = new object[parameters.Length];
                
                // Resolve all constructor parameters recursively
                for (int i = 0; i < parameters.Length; i++)
                {
                    try
                    {
                        args[i] = Resolve(parameters[i].ParameterType);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to resolve constructor parameter '{parameters[i].Name}' of type '{parameters[i].ParameterType.Name}' for type '{type.Name}'", ex);
                    }
                }
                
                return Activator.CreateInstance(type, args);
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Failed to create instance of type {type.Name}", ex);
            }
        }
        
        /// <summary>
        /// Clears all registrations from the container.
        /// </summary>
        /// <remarks>
        /// This method removes all singleton instances, factories, type bindings, and 
        /// clears the resolution stack. Primarily useful for testing scenarios where
        /// you need to reset the container state between tests.
        /// </remarks>
        public void Clear()
        {
            _singletons.Clear();
            _factories.Clear();
            _bindings.Clear();
            _resolutionStack.Clear();
        }

        #endregion
    }
}
