using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Pause screen implementation
    /// </summary>
    public class PauseScreen : UIScreen
    {
        private Button _resumeButton;
        private Button _optionsButton;
        private Button _mainMenuButton;
    
        public PauseScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeButtons();
        }
    
        private void InitializeButtons()
        {
            _resumeButton = RootElement?.Q<Button>("ResumeButton");
            _optionsButton = RootElement?.Q<Button>("OptionsButton");
            _mainMenuButton = RootElement?.Q<Button>("MainMenuButton");
        
            _resumeButton?.RegisterCallback<ClickEvent>(OnResumeClicked);
            _optionsButton?.RegisterCallback<ClickEvent>(OnOptionsClicked);
            _mainMenuButton?.RegisterCallback<ClickEvent>(OnMainMenuClicked);
        }
    
        private void OnResumeClicked(ClickEvent evt)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new ResumeRequestedEvent());
        }
    
        private void OnOptionsClicked(ClickEvent evt)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new OptionsRequestedEvent());
        }
    
        private void OnMainMenuClicked(ClickEvent evt)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new MainMenuRequestedEvent());
        }
    }
}