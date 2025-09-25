using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Config.ScriptableObjects;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.UI;
using GameFramework.UI.Interfaces;
using GameFramework.UI.Popups;
using GameFramework.UI.Screens;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.Components.Controllers.Enum;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace GameFramework.Services
{
    /// <summary>
    /// UI service implementation with constructor injection and centralized screen updates
    /// Implements IUpdatable to manage frame-based updates for screens that need them
    /// Now handles debug settings integration for automatic debug popup management
    /// </summary>
    public class UIService : IUIService, IUpdatable
    {
        public bool IsInitialized { get; private set; }
        public UIDocument UIDocument => _uiDocument;

        private readonly IEventSystem _eventSystem;
        private readonly Dictionary<Type, UIScreen> _screens = new();
        private readonly Dictionary<Type, UIPopup> _popups = new();
        private readonly List<UIScreen> _updatableScreens = new();
        private readonly List<UIPopup> _updatablePopups = new(); // Add this new list
        private readonly UIDocument _uiDocument;
        private readonly IUIDocumentWrapper _uiDocumentWrapper;

        // Popup management fields
        private UIPopup _currentPopup;
        private readonly Stack<UIPopup> _popupStack = new Stack<UIPopup>();

        private IPauseService _pauseService;

        // Centralized cursor management fields
        private CursorLockRequirement _currentControllerCursorRequirement = CursorLockRequirement.Never;
        private GameStateType _currentGameState = GameStateType.Bootstrap;

        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public UIService(IEventSystem eventSystem, UIDocument uiDocument, IPauseService pauseService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
        }
        
        public UIService(IEventSystem eventSystem, IUIDocumentWrapper uiDocumentWrapper)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _uiDocumentWrapper = uiDocumentWrapper ?? throw new ArgumentNullException(nameof(uiDocumentWrapper));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            // Initialize all screens and popups
            InitializeScreensAndPopups();

            // Subscribe to events
            SubscribeToLoadingEvents();
            SubscribeToDebugEvents();
            SubscribeToCursorEvents();

            // Apply initial config state (including debug popup)
            await ApplyInitialConfigState();

            IsInitialized = true;
            await Task.CompletedTask;
        }
                
        /// <summary>
        /// IUpdatable implementation - called every frame by GameManager
        /// Updates all screens and popups that need frame-based updates
        /// Uses unscaled time for popups to work during pause
        /// </summary>
        public void Update()
        {
            if (!IsInitialized) return;
            
            bool isPaused = _pauseService?.IsPaused ?? false;
            
            // Use different deltaTime sources based on pause state and element type
            float scaledDeltaTime = Time.deltaTime;        // Affected by timeScale (becomes 0 when paused)
            float unscaledDeltaTime = Time.unscaledDeltaTime; // Not affected by timeScale
            
            // Update only visible screens that need frame updates
            for (int i = _updatableScreens.Count - 1; i >= 0; i--)
            {
                var screen = _updatableScreens[i];
                if (screen != null && screen.IsVisible && screen.NeedsFrameUpdates)
                {
                    if (!isPaused || screen.ShouldUpdateWhenPaused())
                    {
                        // Use unscaled time if screen should update when paused, otherwise use scaled
                        float deltaTimeToUse = screen.ShouldUpdateWhenPaused() ? unscaledDeltaTime : scaledDeltaTime;
                        screen.InternalUpdate(deltaTimeToUse);
                    }
                }
                else if (screen == null)
                {
                    _updatableScreens.RemoveAt(i);
                }
            }
            
            // Update all visible popups that need frame updates
            // Popups should generally work even when paused (especially debug/utility popups)
            for (int i = _updatablePopups.Count - 1; i >= 0; i--)
            {
                var popup = _updatablePopups[i];
                if (popup != null && popup.IsVisible && popup.NeedsFrameUpdates)
                {
                    // Use unscaled delta time for popups so they work when paused
                    popup.InternalUpdate(unscaledDeltaTime);
                    
                    // Debug logging for DebugPopup specifically
                    if (popup is DebugPopup && unscaledDeltaTime > 0)
                    {
                        // Uncomment for debugging
                        // Debug.Log($"[UIService] Updating DebugPopup with unscaledDeltaTime: {unscaledDeltaTime:F4}");
                    }
                }
                else if (popup == null)
                {
                    _updatablePopups.RemoveAt(i);
                }
            }
        }
        
        public void Shutdown()
        {
            // Unsubscribe from events
            UnsubscribeFromLoadingEvents();
            UnsubscribeFromDebugEvents();
            UnsubscribeFromCursorEvents();

            // Clean up all screens
            foreach (var screen in _screens.Values)
            {
                screen.Cleanup();
            }
            
            foreach (var popup in _popups.Values)
            {
                popup.Cleanup();
            }
            
            _screens.Clear();
            _popups.Clear();
            _updatableScreens.Clear();
            _updatablePopups.Clear(); 

            // Clear popup management
            _currentPopup = null;
            _popupStack.Clear();
            
            IsInitialized = false;
        }
        
        public void SetCursorState(bool visible, bool locked)
        {
            // Allow manual override but log for debugging
            Debug.Log($"[UIService] Manual cursor override - Visible: {visible}, Locked: {locked}");
            Cursor.visible = visible;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }

        /// <summary>
        /// Centralized cursor state management based on game state, controller type, and popup visibility
        /// </summary>
        private void UpdateCursorState()
        {
            bool shouldLockCursor = false;
            bool shouldShowCursor = true;

            // Determine cursor behavior based on controller requirements and game state
            switch (_currentControllerCursorRequirement)
            {
                case CursorLockRequirement.Never:
                    // Always visible and unlocked (RTS, Isometric)
                    shouldLockCursor = false;
                    shouldShowCursor = true;
                    break;

                case CursorLockRequirement.DuringGameplay:
                    // Lock only during Playing state and no popups (First Person)
                    shouldLockCursor = _currentGameState == GameStateType.Playing && !HasOpenPopups();
                    shouldShowCursor = !shouldLockCursor;
                    break;

                case CursorLockRequirement.DuringGameplayWithUIExceptions:
                    // Lock during Playing state but allow unlocking for popups (Third Person)
                    shouldLockCursor = _currentGameState == GameStateType.Playing && !HasOpenPopups();
                    shouldShowCursor = true; // Keep cursor visible but may be locked
                    break;
            }

            // Apply the cursor state
            Cursor.visible = shouldShowCursor;
            Cursor.lockState = shouldLockCursor ? CursorLockMode.Locked : CursorLockMode.None;

            Debug.Log($"[UIService] Cursor updated - Controller: {_currentControllerCursorRequirement}, " +
                      $"State: {_currentGameState}, Popups: {HasOpenPopups()}, " +
                      $"Visible: {shouldShowCursor}, Locked: {shouldLockCursor}");
        }
        
        private void InitializeScreensAndPopups()
        {
            // Handle both constructor types
            VisualElement root;
            if (_uiDocument != null)
            {
                root = _uiDocument.rootVisualElement;
            }
            else if (_uiDocumentWrapper != null)
            {
                root = _uiDocumentWrapper.RootVisualElement;
            }
            else
            {
                throw new InvalidOperationException("No UI document or wrapper available");
            }
    
            // Register screens
            RegisterScreen(new SplashScreen(root.Q<VisualElement>("UI_SplashScreen")));
            RegisterScreen(new MainMenuScreen(root.Q<VisualElement>("UI_MainMenuScreen")));
            RegisterScreen(new GamePlayScreen(root.Q<VisualElement>("UI_GamePlayScreen")));
            RegisterScreen(new LoadingScreen(root.Q<VisualElement>("UI_LoadingScreen")));
            RegisterScreen(new NewGameScreen(root.Q<VisualElement>("UI_NewGameScreen")));
            RegisterScreen(new CreditsScreen(root.Q<VisualElement>("UI_CreditScreen")));
            RegisterScreen(new GameOverScreen(root.Q<VisualElement>("UI_GameOverScreen")));
            RegisterScreen(new VictoryScreen(root.Q<VisualElement>("UI_VictoryScreen")));
            RegisterScreen(new QuitScreen(root.Q<VisualElement>("UI_QuitScreen")));

            // Register popups
            RegisterPopup(new OptionsPopup(root.Q<VisualElement>("UI_OptionsPopup")));
            RegisterPopup(new LoadGamePopup(root.Q<VisualElement>("UI_LoadGamePopup"))); 
            RegisterPopup(new SaveGamePopup(root.Q<VisualElement>("UI_SaveGamePopup")));
            RegisterPopup(new PausePopup(root.Q<VisualElement>("UI_PausePopup")));
            RegisterPopup(new DebugPopup(root.Q<VisualElement>("UI_DebugPopup"))); 
        }

        public async Task ShowScreenAsync<T>() where T : UIScreen
        {
            if (_screens.TryGetValue(typeof(T), out var screen))
            {
                screen.Show();
                await Task.CompletedTask;
            }
            else
            {
                Debug.LogError($"[UIService] Screen of type {typeof(T).Name} not registered");
            }
        }
        
        public async Task HideScreenAsync<T>() where T : UIScreen
        {
            if (_screens.TryGetValue(typeof(T), out var screen))
            {
                screen.Hide();
                await Task.CompletedTask;
            }
            else
            {
                Debug.LogError($"[UIService] Screen of type {typeof(T).Name} not registered");
            }
        }
        
        public async Task ShowPopupAsync<T>() where T : UIPopup
        {
            if (_popups.TryGetValue(typeof(T), out var popup))
            {
                // Push current popup to stack if exists
                if (_currentPopup != null)
                {
                    _popupStack.Push(_currentPopup);
                }

                // Show new popup
                _currentPopup = popup;
                popup.Show();
                
                // Update cursor state when popup is shown
                UpdateCursorState();
                
                await Task.CompletedTask;
            }
            else
            {
                Debug.LogError($"[UIService] Popup of type {typeof(T).Name} not registered");
            }
        }
        
        public async Task HidePopupAsync<T>() where T : UIPopup
        {
            if (_popups.TryGetValue(typeof(T), out var popup))
            {
                if (_currentPopup == popup)
                {
                    // Current popup - hide and restore from stack
                    popup.Hide();

                    if (_popupStack.Count > 0)
                    {
                        _currentPopup = _popupStack.Pop();
                        _currentPopup.Show();
                    }
                    else
                    {
                        _currentPopup = null;
                    }

                    // Update cursor state after hiding popup
                    UpdateCursorState();
                }
                else if (_popupStack.Contains(popup))
                {
                    // Handle popup in stack
                    var stackList = _popupStack.ToList();
                    stackList.Remove(popup);
                    _popupStack.Clear();
            
                    // Rebuild stack without the removed popup
                    foreach (var stackedPopup in stackList.AsEnumerable().Reverse())
                    {
                        _popupStack.Push(stackedPopup);
                    }
            
                    popup.Hide();
                }
                else
                {
                    // Popup exists but not open
                    Debug.LogWarning($"[UIService] Popup {typeof(T).Name} is not currently open");
                }

                await Task.CompletedTask;
            }
            else
            {
                Debug.LogError($"[UIService] Popup of type {typeof(T).Name} not registered");
            }
        }

        public void RegisterScreen<T>(T screen) where T : UIScreen
        {
            _screens[typeof(T)] = screen;
            
            // Add to updatable list if the screen needs frame updates
            if (screen.NeedsFrameUpdates && !_updatableScreens.Contains(screen))
            {
                _updatableScreens.Add(screen);
            }
        }
        
        public void RegisterPopup<T>(T popup) where T : UIPopup
        {
            _popups[typeof(T)] = popup;
        
            // Add to updatable list if the popup needs frame updates
            if (popup.NeedsFrameUpdates && !_updatablePopups.Contains(popup))
            {
                _updatablePopups.Add(popup);
            }
        }
        
        
        public T GetScreen<T>() where T : UIScreen
        {
            return _screens.TryGetValue(typeof(T), out var screen) ? (T)screen : null;
        }
        
        public T GetPopup<T>() where T : UIPopup
        {
            return _popups.TryGetValue(typeof(T), out var popup) ? (T)popup : null;
        }
        

        public UIDocument GetUIDocument()
        {
            return _uiDocument;
        }
        
        public void SetDebugPopupText(string text)
        {
            GetPopup<DebugPopup>()?.SetText(text);
        }
        
        /// <summary>
        /// Internal method to register a screen for updates after it's created
        /// </summary>
        internal void RegisterScreenForUpdates(UIScreen screen)
        {
            if (screen != null && !_updatableScreens.Contains(screen))
            {
                _updatableScreens.Add(screen);
            }
        }
        
        /// <summary>
        /// Internal method to unregister a screen from updates
        /// </summary>
        internal void UnregisterScreenFromUpdates(UIScreen screen)
        {
            if (screen != null)
            {
                _updatableScreens.Remove(screen);
            }
        }
        
        #region Popup Management Methods
        
        public UIPopup GetCurrentPopup() => _currentPopup;
        public Type GetCurrentPopupType() => _currentPopup?.GetType();
        public bool IsCurrentPopup<T>() where T : UIPopup => _currentPopup != null && _currentPopup.GetType() == typeof(T);

        public bool IsPopupOpen<T>() where T : UIPopup
        {
            if (!_popups.TryGetValue(typeof(T), out var popup))
                return false;

            return _currentPopup == popup || _popupStack.Contains(popup);
        }

        public int GetPopupStackPosition<T>() where T : UIPopup
        {
            if (!_popups.TryGetValue(typeof(T), out var popup))
                return -1;

            if (_currentPopup == popup)
                return 0;

            var stackArray = _popupStack.ToArray();
            for (int i = 0; i < stackArray.Length; i++)
            {
                if (stackArray[i] == popup)
                    return i + 1;
            }

            return -1;
        }
        
        public async Task CloseAllPopupsAsync()
        {
            // Hide current popup if any
            if (_currentPopup != null)
            {
                _currentPopup.Hide();
                _currentPopup = null;
            }
            
            // Hide all popups in the stack
            while (_popupStack.Count > 0)
            {
                var popup = _popupStack.Pop();
                popup.Hide();
            }

            // Update cursor state after closing all popups
            UpdateCursorState();
            
            await Task.CompletedTask;
        }

        public bool HasOpenPopups()
        {
            if (_currentPopup != null && _currentPopup.CountsAsGameBlockingPopup)
                return true;
    
            return _popupStack.Any(popup => popup.CountsAsGameBlockingPopup);
        }

        public int GetOpenPopupCount()
        {
            int count = 0;
    
            if (_currentPopup != null && _currentPopup.CountsAsGameBlockingPopup)
                count++;
    
            count += _popupStack.Count(popup => popup.CountsAsGameBlockingPopup);
            return count;
        }

        public UIPopup[] GetPopupStack() => _popupStack.ToArray();

        public async Task HidePopupAsync<T>(bool closeAll = false) where T : UIPopup
        {
            if (closeAll)
            {
                await CloseAllPopupsAsync();
                return;
            }
            
            await HidePopupAsync<T>();
        }
        
        public async Task ForceClosePopupAsync<T>() where T : UIPopup
        {
            if (_popups.TryGetValue(typeof(T), out var popup))
            {
                // Remove from current if it's current
                if (_currentPopup == popup)
                {
                    _currentPopup = null;
                }
        
                // Remove from stack if it exists there
                if (_popupStack.Contains(popup))
                {
                    var stackList = _popupStack.ToList();
                    stackList.Remove(popup);
                    _popupStack.Clear();
                    foreach (var stackedPopup in stackList.AsEnumerable().Reverse())
                    {
                        _popupStack.Push(stackedPopup);
                    }
                }
        
                // Force hide
                popup.Hide();
        
                // If no current popup and stack has items, restore top of stack
                if (_currentPopup == null && _popupStack.Count > 0)
                {
                    _currentPopup = _popupStack.Pop();
                    _currentPopup.Show();
                }
        
                await Task.CompletedTask;
            }
        }
        
        #endregion

        #region Debug Settings Integration

        /// <summary>
        /// Subscribe to debug-related events
        /// </summary>
        private void SubscribeToDebugEvents()
        {
            _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
        }

        /// <summary>
        /// Unsubscribe from debug events
        /// </summary>
        private void UnsubscribeFromDebugEvents()
        {
            _eventSystem.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
        }

        /// <summary>
        /// Subscribe to cursor management events
        /// </summary>
        private void SubscribeToCursorEvents()
        {
            _eventSystem.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
            _eventSystem.Subscribe<PlayerControllerActivatedEvent>(OnPlayerControllerActivated);
        }

        /// <summary>
        /// Unsubscribe from cursor management events
        /// </summary>
        private void UnsubscribeFromCursorEvents()
        {
            _eventSystem.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
            _eventSystem.Unsubscribe<PlayerControllerActivatedEvent>(OnPlayerControllerActivated);
        }

        /// <summary>
        /// Handle options changed events for debug UI
        /// </summary>
        private async void OnOptionsChanged(OptionsChangedEvent evt)
        {
            await ApplyDebugSettings();
        }
        
        /// <summary>
        /// Handle game state changes for cursor management
        /// </summary>
        private void OnGameStateChanged(GameStateChangeEvent evt)
        {
            _currentGameState = evt.NewState;
            UpdateCursorState();
        }
        
        /// <summary>
        /// Handle player controller activation for cursor management
        /// </summary>
        private void OnPlayerControllerActivated(PlayerControllerActivatedEvent evt)
        {
            _currentControllerCursorRequirement = evt.CursorRequirement;
            UpdateCursorState();
        }

        /// <summary>
        /// Apply debug display settings with proper lifecycle management
        /// </summary>
        private async Task ApplyDebugSettings()
        {
            try
            {
                if (SettingsRegistry.Get<DebugSettings_SO>().ShowDebugInfo.Value)
                {
                    var debugPopup = GetPopup<DebugPopup>();
            
                    // Show debug popup if not already visible, but don't interfere with popup stack
                    if (debugPopup != null && !debugPopup.IsVisible)
                    {
                        // Show directly without going through popup stack management
                        debugPopup.Show();
                
                        // Ensure it's not counted in game-blocking popup checks
                        Debug.Log("[UIService] DebugPopup shown (non-blocking)");
                    }
                }
                else
                {
                    // Hide debug popup safely
                    await HideDebugPopupSafely();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Error applying debug display settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely hide the debug popup without affecting popup stack
        /// </summary>
        private async Task HideDebugPopupSafely()
        {
            var debugPopup = GetPopup<DebugPopup>();
            if (debugPopup != null && debugPopup.IsVisible)
            {
                // Hide directly without popup stack management since it's non-blocking
                debugPopup.Hide();
        
                // Clean up any references in popup management if somehow they exist
                if (_currentPopup == debugPopup)
                {
                    _currentPopup = null;
            
                    // Restore next popup from stack if available
                    if (_popupStack.Count > 0)
                    {
                        _currentPopup = _popupStack.Pop();
                        _currentPopup.Show();
                    }
                }
        
                Debug.Log("[UIService] DebugPopup hidden safely");
            }
    
            await Task.CompletedTask;
        }

        /// <summary>
        /// Apply initial UI state based on current configuration values
        /// </summary>
        private async Task ApplyInitialConfigState()
        {
            try
            {
                // Apply debug settings on startup
                await ApplyDebugSettings();
                
                // You can add other initial UI state checks here in the future
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Error applying initial config state: {ex.Message}");
            }
        }

        #endregion

        #region Loading Event Management

        /// <summary>
        /// Subscribe to loading-related events for proper UI management
        /// </summary>
        private void SubscribeToLoadingEvents()
        {
            _eventSystem.Subscribe<LoadingStartedEvent>(OnLoadingStarted);
            _eventSystem.Subscribe<LoadSaveFileEvent>(OnLoadSaveFileRequested);
        }
        
        /// <summary>
        /// Unsubscribe from loading events
        /// </summary>
        private void UnsubscribeFromLoadingEvents()
        {
            _eventSystem.Unsubscribe<LoadingStartedEvent>(OnLoadingStarted);
            _eventSystem.Unsubscribe<LoadSaveFileEvent>(OnLoadSaveFileRequested);
        }
        
        /// <summary>
        /// Handle loading started event by closing relevant popups
        /// </summary>
        private async void OnLoadingStarted(LoadingStartedEvent evt)
        {
            await CloseAllPopupsForLoading();
        }
        
        /// <summary>
        /// Handle load save file event by immediately closing popups
        /// </summary>
        private async void OnLoadSaveFileRequested(LoadSaveFileEvent evt)
        {
            await CloseAllPopupsForLoading();
        }
        
        /// <summary>
        /// Close only game-blocking popups for loading operations
        /// Preserves utility popups like DebugPopup that should persist across loading
        /// </summary>
        public async Task CloseAllPopupsForLoading()
        {
            try
            {
                await CloseGameBlockingPopupsAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIService] Error closing popups for loading: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Close only popups that block game flow, preserving utility popups
        /// </summary>
        public async Task CloseGameBlockingPopupsAsync()
        {
            // Handle current popup if it's game-blocking
            if (_currentPopup != null && _currentPopup.CountsAsGameBlockingPopup)
            {
                _currentPopup.Hide();
                _currentPopup = null;
            }
    
            // Close only game-blocking popups from the stack
            var gameBlockingPopups = _popupStack.Where(popup => popup.CountsAsGameBlockingPopup).ToList();
    
            // Remove game-blocking popups from stack
            var remainingPopups = _popupStack.Where(popup => !popup.CountsAsGameBlockingPopup).ToList();
            _popupStack.Clear();
    
            // Hide all game-blocking popups
            foreach (var popup in gameBlockingPopups)
            {
                popup.Hide();
            }
    
            // Rebuild stack with only non-game-blocking popups
            foreach (var popup in remainingPopups.AsEnumerable().Reverse())
            {
                _popupStack.Push(popup);
            }
    
            // If no current popup and we have non-game-blocking popups, restore the top one
            if (_currentPopup == null && _popupStack.Count > 0)
            {
                _currentPopup = _popupStack.Pop();
                // Don't call Show() here as the popup should already be visible
            }
    
            await Task.CompletedTask;
        }

        
        #endregion
    }
}
