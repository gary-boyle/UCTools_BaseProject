using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;
using GameFramework.UI;
using GameFramework.UI.Screens;
using UnityEngine;
using UnityEngine.UIElements;
using IUIService = GameFramework.Services.Interfaces.IUIService;

namespace GameFramework.Services
{
    /// <summary>
    /// UI service implementation with constructor injection
    /// </summary>
    public class UIService : IUIService
    {
        public bool IsInitialized { get; private set; }

        public UIDocument UIDocument => _uiDocument;

        private readonly IEventSystem _eventSystem;
        private readonly Dictionary<Type, UIScreen> _screens = new();
        private readonly Dictionary<Type, UIPopup> _popups = new();
        private readonly UIDocument _uiDocument;
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public UIService(IEventSystem eventSystem, UIDocument uiDocument)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
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
        
        public void Shutdown()
        {
            _screens.Clear();
            _popups.Clear();
            IsInitialized = false;
        }
        
        private void InitializeScreensAndPopups()
        {
            var root = _uiDocument.rootVisualElement;
            
            // Register screens
            RegisterScreen(new DebugScreen(root.Q<VisualElement>("UI_DebugScreen")));
            RegisterScreen(new SplashScreen(root.Q<VisualElement>("UI_SplashScreen")));
            RegisterScreen(new MainMenuScreen(root.Q<VisualElement>("UI_MainMenuScreen")));
            RegisterScreen(new GameplayHUD(root.Q<VisualElement>("UI_GamePlayHUD")));
            RegisterScreen(new PauseScreen(root.Q<VisualElement>("UI_PauseScreen")));
            RegisterScreen(new OptionsScreen(root.Q<VisualElement>("UI_OptionsScreen")));
            RegisterScreen(new LoadingScreen(root.Q<VisualElement>("UI_LoadingScreen")));
            RegisterScreen(new NewGameScreen(root.Q<VisualElement>("UI_NewGameScreen")));
            RegisterScreen(new CreditsScreen(root.Q<VisualElement>("UI_CreditScreen")));
            RegisterScreen(new GameOverScreen(root.Q<VisualElement>("UI_GameOverScreen")));
            RegisterScreen(new VictoryScreen(root.Q<VisualElement>("UI_VictoryScreen")));
            
            // Register popups TODO
            //RegisterPopup(new ConfirmationPopup(root.Q<VisualElement>("ConfirmationPopup")));
            //RegisterPopup(new ErrorPopup(root.Q<VisualElement>("ErrorPopup")));
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
    }
}