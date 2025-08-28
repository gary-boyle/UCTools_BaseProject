using System.Collections.Generic;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    public class NewGameScreen : UIScreen
    {
        private Button _confirmButton;
        private TextField _playerNameTextField;
        private DropdownField _difficultyDropdown;

        private IEventSystem _eventSystem;

        public NewGameScreen(VisualElement rootElement) : base(rootElement)
        {
            // Get event system from DI container
            _eventSystem = GameManager.GetService<IEventSystem>();
            
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        protected override void OnShow()
        {
            // Focus on player name field and set default value if empty
            _playerNameTextField?.Focus();
            if (string.IsNullOrEmpty(_playerNameTextField?.value))
            {
                _playerNameTextField?.SetValueWithoutNotify("Player");
            }
        }
        
        private void InitializeUI()
        {
            _confirmButton = RootElement?.Q<Button>("btn_Confirm");
            _playerNameTextField = RootElement?.Q<TextField>("txt_PlayerName");
            _difficultyDropdown = RootElement?.Q<DropdownField>("dd_Difficulty");

            // Set up difficulty options
            _difficultyDropdown?.choices.AddRange(new[] { "Easy", "Normal", "Hard", "Expert" });
            _difficultyDropdown?.SetValueWithoutNotify("Normal");

            
            // Subscribe to button events
            _confirmButton?.RegisterCallback<ClickEvent>(OnNewGameClicked);
            
            // Optional: Handle Enter key in text field
            _playerNameTextField?.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
        }
        
        // Then in OnNewGameClicked, use these values:
        private async void OnNewGameClicked(ClickEvent evt)
        {
            var newGameEvent = new NewGameRequestedEvent
            {
                PlayerName = _playerNameTextField?.value?.Trim() ?? "Player",
                Difficulty = _difficultyDropdown?.value ?? "Normal",
                StartingScene = "GameLevel1",
                CustomData = new Dictionary<string, object>
                {
                    ["creationTime"] = System.DateTime.Now.ToString()
                }
            };
    
            _eventSystem?.Publish(newGameEvent);
        }
        
        private void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            // Allow Enter key to confirm new game
            if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.KeypadEnter)
            {
                OnNewGameClicked(null);
            }
        }
        
        protected override void OnHide()
        {
            // Clean up event subscriptions if needed
            _confirmButton?.UnregisterCallback<ClickEvent>(OnNewGameClicked);
            _playerNameTextField?.UnregisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
        }
    }
}
