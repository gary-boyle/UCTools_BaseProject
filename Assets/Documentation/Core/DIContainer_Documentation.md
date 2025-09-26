# DIContainer Documentation

## Overview

The DIContainer is an enhanced dependency injection container that provides automatic constructor injection support for Unity projects. It implements the Service Locator pattern with singleton instance management and supports multiple registration strategies for flexible dependency management.

## Core Architecture

The DIContainer operates as a singleton instance that manages three types of service registrations:

- **Singleton Services**: Single instance shared across the application
- **Transient Services**: New instance created for each request
- **Factory Services**: Custom factory functions for specialized creation logic

## Registration Patterns

### Singleton Pattern Flow

```mermaid
graph TD
    A[RegisterSingleton Call] --> B{Instance Already Provided?}
    B -->|Yes| C[Store Pre-created Instance]
    B -->|No| D[Store Type Binding]
    C --> E[Instance Available Immediately]
    D --> F[Lazy Creation on First Resolve]
    F --> G[Cache Instance for Future Use]
```

### Transient Pattern Flow

```mermaid
graph TD
    A[RegisterTransient Call] --> B[Store Factory Function]
    B --> C[Factory Creates New Instance Each Time]
    C --> D[No Caching]
    D --> E[Fresh Instance Per Request]
```

### Factory Pattern Flow

```mermaid
graph TD
    A[RegisterFactory Call] --> B[Store Custom Factory Function]
    B --> C[Factory Called on Each Resolve]
    C --> D[Custom Creation Logic Executed]
    D --> E[Return Factory Result]
```

## Service Resolution Process

The container follows a priority-based resolution strategy:

```mermaid
graph TD
    A[Resolve<T> Called] --> B[Check Circular Dependencies]
    B --> C{Circular Dependency?}
    C -->|Yes| D[Throw Exception with Chain]
    C -->|No| E[Add to Resolution Stack]
    
    E --> F{Existing Singleton?}
    F -->|Yes| G[Return Cached Instance]
    F -->|No| H{Factory Registered?}
    
    H -->|Yes| I[Call Factory Function]
    H -->|No| J{Type Binding Exists?}
    
    J -->|Yes| K[Create via Constructor Injection]
    J -->|No| L{Concrete Type?}
    
    L -->|Yes| M[Direct Creation]
    L -->|No| N[Throw Registration Error]
    
    K --> O[Cache if Singleton]
    I --> P[Remove from Resolution Stack]
    G --> P
    O --> P
    M --> P
    P --> Q[Return Instance]
```

## Constructor Injection Process

The DIContainer uses reflection-based constructor injection to automatically resolve dependencies:

```mermaid
graph TD
    A[CreateInstance Called] --> B[Get All Constructors]
    B --> C[Select Constructor with Most Parameters]
    C --> D[Get Parameter Types]
    D --> E[For Each Parameter]
    E --> F[Recursively Resolve Parameter]
    F --> G{All Parameters Resolved?}
    G -->|No| E
    G -->|Yes| H[Create Instance with Activator]
    H --> I[Return Constructed Object]
    
    F --> J{Resolution Failed?}
    J -->|Yes| K[Throw Detailed Exception]
```

## Circular Dependency Detection

The container maintains a resolution stack to prevent infinite dependency loops:

```mermaid
graph TD
    A[Resolution Started] --> B[Add Type to Stack]
    B --> C[Begin Dependency Resolution]
    C --> D{Type Already in Stack?}
    D -->|Yes| E[Build Dependency Chain]
    E --> F[Throw Circular Dependency Exception]
    D -->|No| G[Continue Resolution]
    G --> H[Resolve Dependencies Recursively]
    H --> I[Remove Type from Stack]
    I --> J[Return Instance]
```

## Internal Data Management

The container manages four key data structures:

```mermaid
graph LR
    A[DIContainer] --> B[_singletons Dictionary]
    A --> C[_factories Dictionary]
    A --> D[_bindings Dictionary]
    A --> E[_resolutionStack HashSet]
    
    B --> B1[Type → Instance Mapping]
    C --> C1[Type → Factory Function Mapping]
    D --> D1[Interface → Implementation Mapping]
    E --> E1[Circular Dependency Tracking]
```

## Service Lifecycle Management

Different registration types have distinct lifecycle behaviors:

```mermaid
graph TD
    A[Service Request] --> B{Registration Type}
    
    B -->|Singleton Instance| C[Return Cached Object]
    B -->|Singleton Binding| D{First Request?}
    B -->|Transient| E[Create New Instance]
    B -->|Factory| F[Call Factory Function]
    
    D -->|Yes| G[Create & Cache Instance]
    D -->|No| C
    
    C --> H[Same Object Reference]
    G --> I[Cache for Future Use]
    E --> J[Unique Object Instance]
    F --> K[Factory-Controlled Creation]
```

## Error Handling Strategy

The container implements comprehensive error handling:

```mermaid
graph TD
    A[Resolution Request] --> B{Validation Checks}
    
    B --> C[Circular Dependency Check]
    B --> D[Registration Existence Check]
    B --> E[Constructor Availability Check]
    B --> F[Parameter Resolution Check]
    
    C -->|Failed| G[CircularDependencyException]
    D -->|Failed| H[RegistrationNotFoundException]
    E -->|Failed| I[NoConstructorException]
    F -->|Failed| J[ParameterResolutionException]
    
    G --> K[Include Dependency Chain Details]
    H --> L[Suggest Registration Steps]
    I --> M[Constructor Analysis Info]
    J --> N[Parameter-Specific Error Context]
```

## Container Query Capabilities

The container provides introspection methods for runtime service discovery:

```mermaid
graph TD
    A[IsRegistered<T> Query] --> B{Check Singleton Storage}
    B -->|Found| C[Return True]
    B -->|Not Found| D{Check Factory Storage}
    D -->|Found| C
    D -->|Not Found| E{Check Binding Storage}
    E -->|Found| C
    E -->|Not Found| F[Return False]
```

## Performance Considerations

### Memory Management
- Singleton instances are cached indefinitely until container cleanup
- Transient instances are not cached, relying on garbage collection
- Factory functions maintain closure references

### Reflection Overhead
- Constructor analysis happens once per type
- Parameter type resolution uses cached reflection data
- Performance cost is front-loaded during registration/first resolution

### Thread Safety
- The current implementation is **not thread-safe**
- Singleton pattern implementation uses lazy initialization
- Concurrent access requires external synchronization

## Best Practices

### Registration Strategy
1. **Use Singletons** for stateful services that maintain application-wide state
2. **Use Transient** for stateless services that can be safely recreated
3. **Use Factory** for services requiring custom initialization or external dependencies

### Dependency Design
1. Depend on abstractions (interfaces) rather than concrete types
2. Avoid circular dependencies through careful interface design
3. Keep constructor parameter lists focused and minimal

### Error Handling
1. Register all required services before attempting resolution
2. Use `IsRegistered<T>()` for conditional service access
3. Handle resolution exceptions gracefully in application code

## Integration Patterns

### Service Registration Flow
Services should be registered during application bootstrap:

```mermaid
graph TD
    A[Application Start] --> B[Create DIContainer Instance]
    B --> C[Register Core Services]
    C --> D[Register Application Services]
    D --> E[Register UI Services]
    E --> F[Container Ready for Resolution]
    F --> G[Runtime Service Access]
```

### Unity Integration
The container integrates with Unity's component system through service locator patterns, allowing MonoBehaviour components to access registered services without tight coupling.

## Troubleshooting

### Common Issues
1. **Circular Dependencies**: Redesign interfaces to break dependency cycles
2. **Missing Registrations**: Ensure all services are registered before first use
3. **Constructor Ambiguity**: Use explicit registration with factory functions
4. **Performance Issues**: Consider singleton vs transient trade-offs

### Debugging Tools
- Use `IsRegistered<T>()` to verify service availability
- Examine exception messages for dependency chain details
- Enable logging in factory functions for creation tracking
