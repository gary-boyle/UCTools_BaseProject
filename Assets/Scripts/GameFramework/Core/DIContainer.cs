using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// Enhanced dependency injection container with automatic constructor injection support.
    /// Supports singleton registration, factory registration, type bindings, and automatic dependency resolution.
    /// 
    /// Design: Service container with constructor injection and dependency graph resolution
    /// Pros: Clean separation of concerns, automatic dependency resolution, easy testing, loose coupling
    /// Cons: Runtime dependency resolution, potential circular dependencies, reflection overhead
    /// </summary>
    public class DIContainer
    {
        private static DIContainer _instance;
        public static DIContainer Instance => _instance ??= new DIContainer();
        
        private readonly Dictionary<Type, object> _singletons = new();
        private readonly Dictionary<Type, Func<object>> _factories = new();
        private readonly Dictionary<Type, Type> _bindings = new();
        private readonly HashSet<Type> _resolutionStack = new(); // Prevents circular dependencies
        
        /// <summary>
        /// Register a singleton instance that will be reused for all requests
        /// </summary>
        public void RegisterSingleton<T>(T instance) where T : class
        {
            _singletons[typeof(T)] = instance;
        }
        
        /// <summary>
        /// Register a singleton by type - will be created on first request
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>() 
            where TImplementation : class, TInterface
        {
            _bindings[typeof(TInterface)] = typeof(TImplementation);
        }
        
        /// <summary>
        /// Register a singleton by type
        /// </summary>
        public void RegisterSingleton<T>() where T : class
        {
            RegisterSingleton<T, T>();
        }
        
        /// <summary>
        /// Register a factory function that creates new instances on each request
        /// </summary>
        public void RegisterFactory<T>(Func<T> factory) where T : class
        {
            _factories[typeof(T)] = () => factory();
        }
        
        /// <summary>
        /// Register a transient type binding - new instance created each time
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>() 
            where TImplementation : class, TInterface
        {
            _factories[typeof(TInterface)] = () => CreateInstance(typeof(TImplementation));
        }
        
        /// <summary>
        /// Resolve a service instance with automatic constructor injection
        /// </summary>
        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }
        
        /// <summary>
        /// Resolve a service instance by type with automatic constructor injection
        /// </summary>
        public object Resolve(Type type)
        {
            // Prevent circular dependencies
            if (_resolutionStack.Contains(type))
            {
                throw new InvalidOperationException($"Circular dependency detected for type {type.Name}");
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
                
                throw new InvalidOperationException($"No registration found for type {type.Name}");
            }
            finally
            {
                _resolutionStack.Remove(type);
            }
        }
        
        /// <summary>
        /// Create an instance with automatic constructor injection
        /// </summary>
        private object CreateInstance(Type type)
        {
            var constructors = type.GetConstructors();
            
            // Find the constructor with the most parameters (assumes most specific constructor)
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            
            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];
            
            // Resolve all constructor parameters recursively
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = Resolve(parameters[i].ParameterType);
            }
            
            return Activator.CreateInstance(type, args);
        }
        
        /// <summary>
        /// Check if a type is registered
        /// </summary>
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }
        
        public bool IsRegistered(Type type)
        {
            return _singletons.ContainsKey(type) || 
                   _factories.ContainsKey(type) || 
                   _bindings.ContainsKey(type);
        }
        
        /// <summary>
        /// Clear all registrations - useful for testing
        /// </summary>
        public void Clear()
        {
            _singletons.Clear();
            _factories.Clear();
            _bindings.Clear();
            _resolutionStack.Clear();
        }
    }
}