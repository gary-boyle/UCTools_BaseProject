using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.UI;
using GameFramework.UI.Interfaces;
using GameFramework.UI.Popups;
using GameFramework.UI.Screens;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Services
{
    /// <summary>
    /// UI service implementation with constructor injection and centralized screen updates
    /// Implements IUpdatable to manage frame-based updates for screens that need them
    /// </summary>
    public class UIService : IUIService, IUpdatable
    {
        public bool IsInitialized { get; private set; }
        public UIDocument UIDocument => _uiDocument;

        private readonly IEventSystem _eventSystem;
        private readonly Dictionary<Type, UIScreen> _screens = new();
        private readonly Dictionary<Type, UIPopup> _popups = new();
        private readonly List<UIScreen> _updatableScreens = new();
        private readonly UIDocument _uiDocument;
        private readonly IUIDocumentWrapper _uiDocumentWrapper;

        // Popup management fields
        private UIPopup _currentPopup;
        private readonly Stack<UIPopup> _popupStack = new Stack<UIPopup>();

        private IPauseService _pauseService;
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
            
            Debug.Log("[UIService] Initializing UI system...");

            // Initialize all screens and popups
            InitializeScreensAndPopups();

            SubscribeToLoadingEvents();

            await ApplyInitialConfigState();

            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// IUpdatable implementation - called every frame by GameManager
        /// Updates all screens that need frame-based updates and are currently visible
        /// Now respects global pause state from PauseService
        /// </summary>
        public void Update()
        {
            if (!IsInitialized) return;

            bool isPaused = _pauseService?.IsPaused ?? false;

            float deltaTime = Time.deltaTime;

            // Update only visible screens that need frame updates
            for (int i = _updatableScreens.Count - 1; i >= 0; i--)
            {
                var screen = _updatableScreens[i];
                if (screen != null && screen.IsVisible && screen.NeedsFrameUpdates)
                {
                    // **UPDATED:** Only update screen if not paused (unless it's a special pause-immune screen)
                    if (!isPaused || screen.ShouldUpdateWhenPaused())
                    {
                        screen.InternalUpdate(deltaTime);
                    }
                }
                else if (screen == null)
                {
                    // Remove null references
                    _updatableScreens.RemoveAt(i);
                }
            }

            // Update current popup if it needs updates
            // Popups generally should update even when paused (for animations, etc.)
            if (_currentPopup != null && _currentPopup.IsVisible && _currentPopup.NeedsFrameUpdates)
            {
                _currentPopup.InternalUpdate(deltaTime);
            }
        }
        
        public void Shutdown()
        {
            // Clean up all screens
            foreach (var screen in _screens.Values)
            {
                screen.Cleanup();
            }
            
            foreach (var popup in _popups.Values)
            {
                popup.Cleanup();
            }
            
            UnsubscribeFromLoadingEvents();

            _screens.Clear();
            _popups.Clear();
            _updatableScreens.Clear();
            
            // Clear popup management
            _currentPopup = null;
            _popupStack.Clear();
            
            IsInitialized = false;
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
            Debug.Log("Trying to show a popup");
            if (_popups.TryGetValue(typeof(T), out var popup))
            {
                // Push current popup to stack if exists
                if (_currentPopup != null)
                {
                    _popupStack.Push(_currentPopup);
                    Debug.Log($"[UIService] Pushed {_currentPopup.GetType().Name} to popup stack");
                }

                // Show new popup
                _currentPopup = popup;
                popup.Show();
                
                Debug.Log($"[UIService] Showing popup: {typeof(T).Name} (Stack depth: {_popupStack.Count})");
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
                        Debug.Log($"[UIService] Restored popup: {_currentPopup.GetType().Name} (Stack depth: {_popupStack.Count})");
                    }
                    else
                    {
                        _currentPopup = null;
                        Debug.Log($"[UIService] No more popups in stack");
                    }

                    Debug.Log($"[UIService] Hidden current popup: {typeof(T).Name}");
                }
                else if (_popupStack.Contains(popup))
                {
                    // ✅ NEW: Handle popup in stack
                    Debug.Log($"[UIService] Hiding stacked popup: {typeof(T).Name}");
            
                    // Remove from stack and hide
                    var stackList = _popupStack.ToList();
                    stackList.Remove(popup);
                    _popupStack.Clear();
            
                    // Rebuild stack without the removed popup
                    foreach (var stackedPopup in stackList.AsEnumerable().Reverse())
                    {
                        _popupStack.Push(stackedPopup);
                    }
            
                    popup.Hide();
                    Debug.Log($"[UIService] Removed {typeof(T).Name} from popup stack (New stack depth: {_popupStack.Count})");
                }
                else
                {
                    // ✅ NEW: Popup exists but not open
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
                Debug.Log($"[UIService] Registered {typeof(T).Name} for frame updates");
            }
        }
        
        public void RegisterPopup<T>(T popup) where T : UIPopup
        {
            _popups[typeof(T)] = popup;
        }
        
        public T GetScreen<T>() where T : UIScreen
        {
            return _screens.TryGetValue(typeof(T), out var screen) ? (T)screen : null;
        }
        
        public T GetPopup<T>() where T : UIPopup
        {
            return _popups.TryGetValue(typeof(T), out var popup) ? (T)popup : null;
        }
        
        public void SetDebugPopupText(string text)
        {
            GetPopup<DebugPopup>().SetText(text);
        }
        
        /// <summary>
        /// Internal method to register a screen for updates after it's created
        /// Called by screens when they enable frame updates
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
        /// Called by screens when they disable frame updates or are destroyed
        /// </summary>
        internal void UnregisterScreenFromUpdates(UIScreen screen)
        {
            if (screen != null)
            {
                _updatableScreens.Remove(screen);
            }
        }
        
        #region NEW POPUP MANAGEMENT METHODS
        
        /// <summary>
        /// Gets the currently visible popup (null if none)
        /// </summary>
        public UIPopup GetCurrentPopup()
        {
            return _currentPopup;
        }

        /// <summary>
        /// Gets the type of the currently visible popup (null if none)
        /// </summary>
        public Type GetCurrentPopupType()
        {
            return _currentPopup?.GetType();
        }

        /// <summary>
        /// Checks if a specific popup type is currently the active popup
        /// </summary>
        public bool IsCurrentPopup<T>() where T : UIPopup
        {
            return _currentPopup != null && _currentPopup.GetType() == typeof(T);
        }

        /// <summary>
        /// Check if a specific popup is currently open (either current or in stack)
        /// </summary>
        public bool IsPopupOpen<T>() where T : UIPopup
        {
            if (!_popups.TryGetValue(typeof(T), out var popup))
                return false;

            return _currentPopup == popup || _popupStack.Contains(popup);
        }

        /// <summary>
        /// Get the position of a popup in the stack (0 = current, 1 = top of stack, etc.)
        /// Returns -1 if popup is not open
        /// </summary>
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
        
        /// <summary>
        /// Closes all currently open popups and clears the popup stack
        /// </summary>
        public async Task CloseAllPopupsAsync()
        {
            Debug.Log($"[UIService] Closing all popups... (Current: {_currentPopup?.GetType().Name}, Stack: {_popupStack.Count})");
            
            // Hide current popup if any
            if (_currentPopup != null)
            {
                _currentPopup.Hide();
                Debug.Log($"[UIService] Closed current popup: {_currentPopup.GetType().Name}");
                _currentPopup = null;
            }
            
            // Hide all popups in the stack
            while (_popupStack.Count > 0)
            {
                var popup = _popupStack.Pop();
                popup.Hide();
                Debug.Log($"[UIService] Closed stacked popup: {popup.GetType().Name}");
            }
            
            Debug.Log("[UIService] All popups closed, stack cleared");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Checks if any popups are currently open
        /// </summary>
        public bool HasOpenPopups()
        {
            // Check current popup
            if (_currentPopup != null && _currentPopup.CountsAsGameBlockingPopup)
                return true;
    
            // Check stacked popups
            return _popupStack.Any(popup => popup.CountsAsGameBlockingPopup);
        }


        /// <summary>
        /// Gets the number of open popups (current + stacked)
        /// </summary>
        public int GetOpenPopupCount()
        {
            int count = 0;
    
            if (_currentPopup != null && _currentPopup.CountsAsGameBlockingPopup)
                count++;
    
            count += _popupStack.Count(popup => popup.CountsAsGameBlockingPopup);
            return count;
        }

        /// <summary>
        /// Gets all popups in the current stack (excluding current popup)
        /// </summary>
        public UIPopup[] GetPopupStack()
        {
            return _popupStack.ToArray();
        }

        /// <summary>
        /// Enhanced hide popup method that can optionally close all popups
        /// </summary>
        public async Task HidePopupAsync<T>(bool closeAll = false) where T : UIPopup
        {
            if (closeAll)
            {
                await CloseAllPopupsAsync();
                return;
            }
            
            // Use the existing HidePopupAsync method
            await HidePopupAsync<T>();
        }
        
        /// <summary>
        /// Apply initial UI state based on current configuration values
        /// </summary>
        private async Task ApplyInitialConfigState()
        {
            var configService = GameManager.GetService<IConfigService>();
            if (configService == null) return;
    
            try
            {
                // Check debug popup state
                bool showDebugInfo = configService.GetConfigValue<bool>("debug.show_debug_info");
                if (showDebugInfo)
                {
                    Debug.Log("[UIService] Showing debug popup on startup due to config setting");
                    await ShowPopupAsync<DebugPopup>();
                }
        
                // You can add other initial UI state checks here in the future
                // For example, showing certain screens based on game state, etc.
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIService] Error applying initial config state: {ex.Message}");
            }
        }
        
        #endregion
        
        /// <summary>
        /// Force close a specific popup type regardless of stack position
        /// Use this for cleanup when transitions get stuck
        /// </summary>
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
        
                Debug.Log($"[UIService] Force closed popup: {typeof(T).Name}");
                await Task.CompletedTask;
            }
        }

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
        /// This provides immediate UI feedback before loading starts
        /// </summary>
        private async void OnLoadSaveFileRequested(LoadSaveFileEvent evt)
        {
            Debug.Log($"[UIService] Preparing UI for loading: {evt.SaveFileInfo.FileName}");
            await CloseAllPopupsForLoading();
        }
        
        /// <summary>
        /// Much cleaner than maintaining a specific list of popups to close
        /// </summary>
        public async Task CloseAllPopupsForLoading()
        {
            try
            {
                await CloseAllPopupsAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIService] Error closing popups for loading: {ex.Message}");
            }
        }
        
        #endregion
        
    }
}
