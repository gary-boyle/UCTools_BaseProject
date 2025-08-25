# UIService Test Suite Reference Table

UIServiceTests serves as the comprehensive unit test suite for the UIService class, systematically validating each individual method and behavior in isolation using mocked dependencies to ensure predictable and controlled testing conditions. 

This test suite covers the complete API surface of UIService including constructor validation with various parameter combinations, async initialization patterns, proper shutdown and cleanup procedures, screen registration and lifecycle management, popup system functionality, and error handling for edge cases like missing screens or invalid operations. 

It uses mock objects for dependencies like EventSystem and UIDocument wrappers to eliminate external variables and focus purely on the UIService's logic and state management. 

## Constructor Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `Constructor_WithValidUIDocumentWrapper_ShouldCreateService` | Validates service creation with wrapper interface | Service created, not initialized initially | Ensures proper dependency injection with abstraction layer |
| `Constructor_WithValidUIDocument_ShouldCreateService` | Validates service creation with concrete UIDocument | Service created with UIDocument reference set | Supports direct Unity component integration |
| `Constructor_WithNullEventSystem_UIDocumentWrapper_ShouldThrowArgumentNullException` | Tests null validation for event system with wrapper | Throws `ArgumentNullException` | Prevents runtime errors from missing critical dependencies |
| `Constructor_WithNullEventSystem_UIDocument_ShouldThrowArgumentNullException` | Tests null validation for event system with UIDocument | Throws `ArgumentNullException` | Ensures event system dependency is always present |
| `Constructor_WithNullUIDocument_ShouldThrowArgumentNullException` | Tests null validation for UIDocument parameter | Throws `ArgumentNullException` | Prevents service creation without required UI component |
| `Constructor_WithNullUIDocumentWrapper_ShouldThrowArgumentNullException` | Tests null validation for wrapper parameter | Throws `ArgumentNullException` | Ensures UI wrapper dependency is provided |

## Initialization Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `InitializeAsync_WithUIDocumentWrapper_ShouldSetIsInitializedToTrue` | Validates async initialization state management | `IsInitialized` becomes `true` after completion | Essential for tracking service lifecycle state |
| `InitializeAsync_CalledMultipleTimes_ShouldOnlyInitializeOnce` | Tests idempotent initialization behavior | Remains initialized, no duplicate initialization | Prevents resource waste and side effects from multiple calls |
| `InitializeAsync_ShouldRegisterAllScreens` | Validates complete screen registration during init | All expected screens are registered and accessible | Core functionality - ensures UI system is fully configured |

## Shutdown Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `Shutdown_ShouldSetIsInitializedToFalse` | Tests proper state reset during shutdown | `IsInitialized` becomes `false` | Essential for proper lifecycle management and cleanup |
| `Shutdown_ShouldClearAllScreens` | Validates complete screen cleanup | All registered screens become inaccessible | Critical for memory management and preventing dangling references |

## Screen Management Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `RegisterScreen_ShouldAddScreenToCollection` | Tests manual screen registration functionality | Screen added and retrievable via `GetScreen<T>()` | Enables dynamic screen registration beyond default set |
| `ShowScreenAsync_WithRegisteredScreen_ShouldShowScreen` | Validates screen visibility control | Screen's `IsVisible` becomes `true` | Core UI functionality - enables screen display |
| `HideScreenAsync_WithRegisteredScreen_ShouldHideScreen` | Tests screen hiding functionality | Screen's `IsVisible` becomes `false` | Essential for UI state management and navigation |
| `ShowScreenAsync_WithUnregisteredScreen_ShouldLogError` | Tests error handling for missing screens | Logs error message with screen type | Provides clear debugging information for configuration issues |
| `HideScreenAsync_WithUnregisteredScreen_ShouldLogError` | Tests error handling when hiding unregistered screens | Logs error message with screen type | Prevents silent failures and aids in debugging |

## Popup Management Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `RegisterPopup_ShouldAddPopupToCollection` | Tests popup registration functionality | Popup added and retrievable via `GetPopup<T>()` | Enables popup management separate from screens |
| `ShowPopupAsync_WithRegisteredPopup_ShouldShowPopup` | Validates popup display functionality | Popup's `IsVisible` becomes `true` | Core popup functionality for modal dialogs and overlays |
| `HidePopupAsync_WithRegisteredPopup_ShouldHidePopup` | Tests popup hiding functionality | Popup's `IsVisible` becomes `false` | Essential for popup lifecycle management |
| `ShowPopupAsync_WithUnregisteredPopup_ShouldLogError` | Tests error handling for missing popups | Logs error message with popup type | Provides debugging information for popup configuration issues |

## Debug Screen Tests

| Test Name | Purpose | Expected Outcome | Importance |
|-----------|---------|------------------|------------|
| `SetDebugScreenText_ShouldCallSetTextOnDebugScreen` | Tests debug screen text update functionality | No exception thrown when setting debug text | Validates debug functionality for development and troubleshooting |

## Quick Reference Summary

- **Total Tests**: 22
- **Constructor Tests**: 6
- **Initialization Tests**: 3
- **Shutdown Tests**: 2
- **Screen Management Tests**: 5
- **Popup Management Tests**: 4
- **Debug Screen Tests**: 1
- **Error Handling Tests**: 1

**Test Categories by Priority**:
1. **Critical**: Initialization, screen/popup management, constructor validation
2. **Important**: Shutdown cleanup, error handling for missing UI elements
3. **Supporting**: Debug functionality and edge case validation

**Key UIService Features Tested**:
- ✅ Dependency injection with multiple constructor overloads
- ✅ Async initialization with idempotent behavior
- ✅ Complete screen registration and lifecycle management
- ✅ Popup system separate from screens
- ✅ Proper cleanup and shutdown procedures
- ✅ Error handling and logging for missing UI elements
- ✅ Debug screen functionality for development support
- ✅ State management (`IsInitialized` flag)

**Unity-Specific Considerations**:
- Uses Unity's UIDocument and VisualElement systems
- Integrates with Unity's logging system (`LogAssert`)
- Proper GameObject cleanup in test teardown
- Mock objects for Unity UI components in testing