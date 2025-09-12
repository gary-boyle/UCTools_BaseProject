using GameFramework.Audio.Data;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Main menu screen - pure UI component that only reports user interactions
    /// Does not handle its own lifecycle - that's managed by the MainMenuState
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
            
            // Subscribe to button events
            _newGameButton?.RegisterCallback<ClickEvent>(OnNewGameClicked);
            _loadButton?.RegisterCallback<ClickEvent>(OnLoadClicked);
            _optionsButton?.RegisterCallback<ClickEvent>(OnOptionsClicked);
            _creditsButton?.RegisterCallback<ClickEvent>(OnCreditsClicked);
            _quitButton?.RegisterCallback<ClickEvent>(OnQuitClicked);
        }
        
        #region UI Event Handlers - Only Report User Interactions
        
        /// <summary>
        /// Report new game request - state will handle transitions
        /// </summary>
        private void OnNewGameClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new NewGameRequestedEvent());
            _eventSystem?.Publish(new AudioEvents.UIAudioEvent(UIAudioType.ScreenOpen));
        }
        
        /// <summary>
        /// Report load game request - state will handle popup management
        /// </summary>
        private void OnLoadClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new LoadWindowRequestedEvent());
        }
        
        /// <summary>
        /// Report options request - state will handle popup management
        /// </summary>
        private void OnOptionsClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new OptionsRequestedEvent());
        }
        
        /// <summary>
        /// Report credits request - state will handle transitions
        /// </summary>
        private void OnCreditsClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new CreditsRequestedEvent());
        }
        
        /// <summary>
        /// Report quit request - state will handle transitions
        /// </summary>
        private void OnQuitClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new QuitRequestedEvent());
        }
        
        #endregion
        
        protected override void OnShow()
        {
            // Update button states based on current game state
            var saveService = GameManager.GetService<ISaveService>();
            if (_loadButton != null && saveService != null)
            {
                _loadButton.SetEnabled(saveService.HasAnySaves());
            }
        }
        
    }
}
