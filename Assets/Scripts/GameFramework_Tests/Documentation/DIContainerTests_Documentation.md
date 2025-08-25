# DIContainer Test Suite Reference Table

DIContainerTests thoroughly validates the dependency injection container that manages object creation, lifetime, and dependency resolution throughout the entire application, ensuring that complex object graphs can be constructed automatically while maintaining proper lifecycle management. 

This  test suite covers all registration patterns including singleton instances that maintain state across the application, factory patterns that create fresh instances on each request, and transient registrations that provide new objects with automatically injected dependencies. 

It validates sophisticated dependency resolution including multi-level dependency chains where objects depend on other objects that have their own dependencies, while also testing critical error scenarios like circular dependencies that could cause infinite loops and missing dependencies that would prevent object construction. 

The tests ensure that the container can handle both interface-based abstractions and concrete type registrations, supporting flexible architecture patterns. These tests are absolutely crucial because the DI container serves as the foundation for the entire application's object composition - virtually every service, screen, and component depends on it for proper instantiation and dependency management, so any failures in dependency resolution would cascade into system-wide instability and prevent the application from functioning correctly.



## Singleton Registration Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `RegisterSingleton_WithInstance_ShouldReturnSameInstance` | Validates pre-created instance singleton behavior | Multiple resolves return identical instance (reference equality) | Ensures singleton pattern integrity and object identity consistency |
| `RegisterSingleton_WithType_ShouldCreateSingleInstance` | Tests type mapping singleton registration (interface → concrete) | Single instance created on first resolve, same returned subsequently | Critical for memory efficiency and consistent state management |
| `RegisterSingleton_ConcreteType_ShouldCreateSingleInstance` | Tests direct concrete type singleton registration | Creates and maintains single instance of concrete type | Supports singleton behavior without interface abstraction |

## Factory Registration Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `RegisterFactory_ShouldCreateNewInstanceEachTime` | Validates factory pattern creates new instances per resolve | Each resolve creates distinct object instance | Critical for stateful or per-request services that need fresh state |
| `RegisterTransient_ShouldCreateNewInstanceEachTime` | Tests transient lifetime with constructor injection | Multiple instances created with properly injected dependencies | Ensures short-lived services are instantiated correctly |

## Constructor Injection Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `Resolve_WithDependencies_ShouldInjectConstructorParameters` | Validates automatic dependency injection through constructors | Service created with all dependencies properly injected | Core DI functionality - enables loose coupling and testability |
| `Resolve_ConcreteTypeWithoutRegistration_ShouldCreateInstance` | Tests auto-wiring of unregistered concrete types | Concrete type instantiated with dependencies injected automatically | Reduces configuration overhead while maintaining DI |
| `Resolve_SimpleServiceWithoutDependencies_ShouldCreateInstance` | Tests resolution of simple types with no dependencies | Simple service created successfully without dependencies | Ensures basic object creation scenarios work |
| `Resolve_ComplexDependencyChain_ShouldResolveAll` | Tests multi-level dependency resolution | Complex service with nested dependencies fully resolved | Validates deep dependency graph resolution capability |

## Error Handling Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `Resolve_UnregisteredInterface_ShouldThrowException` | Tests behavior when resolving unregistered interfaces | Throws `InvalidOperationException` | Prevents silent failures and provides clear error feedback |
| `Resolve_CircularDependency_ShouldThrowException` | Tests circular dependency detection | Throws exception with "Circular dependency" message | Prevents infinite loops and stack overflow errors |
| `Resolve_MissingDependency_ShouldThrowException` | Tests behavior when dependencies are missing | Throws exception indicating missing dependency type | Ensures all dependencies are properly registered |

## Registration Check Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `IsRegistered_WithRegisteredType_ShouldReturnTrue` | Tests registration status checking for registered types | Returns `true` for both generic and `Type` parameter versions | Enables conditional logic based on registration status |
| `IsRegistered_WithUnregisteredType_ShouldReturnFalse` | Tests registration status checking for unregistered types | Returns `false` for both generic and `Type` parameter versions | Allows safe checking before attempting resolution |

## Container Management Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `Clear_ShouldRemoveAllRegistrations` | Tests complete container cleanup | All registrations removed, `IsRegistered` returns `false` for all types | Essential for test isolation and container reset scenarios |
