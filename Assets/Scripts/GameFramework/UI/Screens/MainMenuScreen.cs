using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Main menu screen implementation with event publishing
    /// </summary>
    public class MainMenuScreen : UIScreen
    {
        private Button _newGameButton;
        private Button _loadButton;
        private Button _optionsButton;
        private Button _creditsButton;
        private Button _quitButton;
        
        public MainMenuScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeButtons();
            
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        private void InitializeButtons()
        {
            _newGameButton = RootElement?.Q<Button>("btn_NewGame");
            _loadButton = RootElement?.Q<Button>("btn_LoadGame");
            _optionsButton = RootElement?.Q<Button>("btn_Options");
            _creditsButton = RootElement?.Q<Button>("btn_Credits");
            _quitButton = RootElement?.Q<Button>("btn_QuitGame");

            
            // Simple validation - just pass all the elements
            
            // Subscribe to button events
            _newGameButton?.RegisterCallback<ClickEvent>(OnNewGameClicked);
            _loadButton?.RegisterCallback<ClickEvent>(OnLoadClicked);
            _optionsButton?.RegisterCallback<ClickEvent>(OnOptionsClicked);
            _creditsButton?.RegisterCallback<ClickEvent>(OnCreditsClicked);
            _quitButton?.RegisterCallback<ClickEvent>(OnQuitClicked);
        }
        
        private void OnNewGameClicked(ClickEvent evt)
        {
            Debug.Log("New Game Pressed");
            // Get event system from game manager and publish event
            _eventSystem?.Publish(new NewGameRequestedEvent());
        }
        
        private void OnLoadClicked(ClickEvent evt)
        {
            Debug.Log("Load Pressed");
            _eventSystem?.Publish(new LoadRequestedEvent());
        }
        
        private void OnOptionsClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new OptionsRequestedEvent());
        }
        
        private void OnCreditsClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new CreditsRequestedEvent());
        }
        
        private void OnQuitClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new QuitRequestedEvent());
        }
        
        protected override void OnShow()
        {
            // Update continue button state
            var saveService = GameManager.GetService<ISaveService>();
            if (_loadButton != null && saveService != null)
            {
                _loadButton.SetEnabled(saveService.HasAnySaves());
            }
        }
    }
}