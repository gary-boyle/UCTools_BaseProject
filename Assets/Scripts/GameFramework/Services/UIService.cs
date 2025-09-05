using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public UIService(IEventSystem eventSystem, UIDocument uiDocument)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
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

            await ShowScreenAsync<DebugScreen>();
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// IUpdatable implementation - called every frame by GameManager
        /// Updates all screens that need frame-based updates and are currently visible
        /// </summary>
        public void Update()
        {
            if (!IsInitialized) return;
            
            float deltaTime = Time.deltaTime;
            
            // Update only visible screens that need frame updates
            for (int i = _updatableScreens.Count - 1; i >= 0; i--)
            {
                var screen = _updatableScreens[i];
                if (screen != null && screen.IsVisible && screen.NeedsFrameUpdates)
                {
                    screen.InternalUpdate(deltaTime);
                }
                else if (screen == null)
                {
                    // Remove null references
                    _updatableScreens.RemoveAt(i);
                }
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
            
            _screens.Clear();
            _popups.Clear();
            _updatableScreens.Clear();
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
            RegisterScreen(new DebugScreen(root.Q<VisualElement>("UI_DebugScreen")));
            RegisterScreen(new SplashScreen(root.Q<VisualElement>("UI_SplashScreen")));
            RegisterScreen(new MainMenuScreen(root.Q<VisualElement>("UI_MainMenuScreen")));
            RegisterScreen(new GamePlayScreen(root.Q<VisualElement>("UI_GamePlayScreen")));
            RegisterScreen(new PauseScreen(root.Q<VisualElement>("UI_PauseScreen")));
            RegisterScreen(new LoadingScreen(root.Q<VisualElement>("UI_LoadingScreen")));
            RegisterScreen(new NewGameScreen(root.Q<VisualElement>("UI_NewGameScreen")));
            RegisterScreen(new CreditsScreen(root.Q<VisualElement>("UI_CreditScreen")));
            RegisterScreen(new GameOverScreen(root.Q<VisualElement>("UI_GameOverScreen")));
            RegisterScreen(new VictoryScreen(root.Q<VisualElement>("UI_VictoryScreen")));
            
            // Register popups
            RegisterPopup(new OptionsPopup(root.Q<VisualElement>("UI_OptionsPopup")));
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
                popup.Show();
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
                popup.Hide();
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
        
        public void SetDebugScreenText(string text)
        {
            GetScreen<DebugScreen>().SetText(text);
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
    }
}
