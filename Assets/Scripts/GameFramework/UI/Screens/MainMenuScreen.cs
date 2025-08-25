using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Main menu screen implementation with event publishing
    /// </summary>
    public class MainMenuScreen : UIScreen
    {
        private Button _newGameButton;
        private Button _continueButton;
        private Button _optionsButton;
        private Button _creditsButton;
        private Button _quitButton;
        
        public MainMenuScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeButtons();
        }
        
        private void InitializeButtons()
        {
            _newGameButton = RootElement?.Q<Button>("NewGameButton");
            _continueButton = RootElement?.Q<Button>("ContinueButton");
            _optionsButton = RootElement?.Q<Button>("OptionsButton");
            _creditsButton = RootElement?.Q<Button>("CreditsButton");
            _quitButton = RootElement?.Q<Button>("QuitButton");
            
            // Subscribe to button events
            _newGameButton?.RegisterCallback<ClickEvent>(OnNewGameClicked);
            _continueButton?.RegisterCallback<ClickEvent>(OnContinueClicked);
            _optionsButton?.RegisterCallback<ClickEvent>(OnOptionsClicked);
            _creditsButton?.RegisterCallback<ClickEvent>(OnCreditsClicked);
            _quitButton?.RegisterCallback<ClickEvent>(OnQuitClicked);
        }
        
        private void OnNewGameClicked(ClickEvent evt)
        {
            // Get event system from game manager and publish event
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new NewGameRequestedEvent());
        }
        
        private void OnContinueClicked(ClickEvent evt)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new ContinueGameRequestedEvent());
        }
        
        private void OnOptionsClicked(ClickEvent evt)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new OptionsRequestedEvent());
        }
        
        private void OnCreditsClicked(ClickEvent evt)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new CreditsRequestedEvent());
        }
        
        private void OnQuitClicked(ClickEvent evt)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new QuitRequestedEvent());
        }
        
        protected override void OnShow()
        {
            // Update continue button state
            var saveService = GameManager.GetService<ISaveService>();
            if (_continueButton != null && saveService != null)
            {
                _continueButton.SetEnabled(saveService.HasAnySaves());
            }
        }
    }
}