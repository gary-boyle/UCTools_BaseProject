using System.Collections.Generic;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// New Game Screen - pure UI component that only reports user interactions
    /// Does not handle its own lifecycle - that's managed by the NewGameState
    /// </summary>
    public class NewGameScreen : UIScreen
    {
        #region UI Elements
        
        private Button _confirmButton;
        private Button _backButton;
        private Button _saveButton;

        private TextField _playerNameTextField;
        private DropdownField _difficultyDropdown;
        
        #endregion

        #region Services
        
        private readonly IEventSystem _eventSystem;
        
        #endregion

        public NewGameScreen(VisualElement rootElement) : base(rootElement)
        {
            _eventSystem = GameManager.GetService<IEventSystem>();
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        #region Screen Lifecycle
        
        protected override void OnShow()
        {
            base.OnShow();
            RegisterUIEventHandlers();
            SetInitialUIState();
        }
        
        protected override void OnHide()
        {
            UnregisterUIEventHandlers();
            base.OnHide();
        }
        
        #endregion
        
        #region UI Initialization
        
        private void InitializeUI()
        {
            // Cache UI elements
            _confirmButton = RootElement?.Q<Button>("btn_Confirm");
            _backButton = RootElement?.Q<Button>("btn_Back");
            _saveButton = RootElement?.Q<Button>("btn_Save");

            _playerNameTextField = RootElement?.Q<TextField>("txt_PlayerName");
            _difficultyDropdown = RootElement?.Q<DropdownField>("dd_Difficulty");

            SetupUIElements();
        }
        
        private void SetupUIElements()
        {
            if (_difficultyDropdown != null)
            {
                _difficultyDropdown.choices.Clear();
                _difficultyDropdown.choices.AddRange(new[] { "Easy", "Normal", "Hard", "Expert" });
                _difficultyDropdown.SetValueWithoutNotify("Normal");
            }
        }
        
        private void SetInitialUIState()
        {
            if (_playerNameTextField != null)
            {
                _playerNameTextField.Focus();
                
                if (string.IsNullOrEmpty(_playerNameTextField.value))
                {
                    _playerNameTextField.SetValueWithoutNotify("Player");
                }
            }
        }
        
        #endregion
        
        #region Event Handler Registration
        
        private void RegisterUIEventHandlers()
        {
            if (_confirmButton != null)
            {
                _confirmButton.RegisterCallback<ClickEvent>(OnNewGameClicked);
            }
            
            if (_backButton != null)
            {
                _backButton.RegisterCallback<ClickEvent>(OnBackButtonClicked);
            }
            
            if (_saveButton != null)
            {
                _saveButton.RegisterCallback<ClickEvent>(OnSaveButtonClicked);
            }
            
            if (_playerNameTextField != null)
            {
                _playerNameTextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
            }
        }
        
        private void UnregisterUIEventHandlers()
        {
            if (_confirmButton != null)
            {
                _confirmButton.UnregisterCallback<ClickEvent>(OnNewGameClicked);
            }
            
            if (_backButton != null)
            {
                _backButton.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
            }
            if (_saveButton != null)
            {
                _saveButton.UnregisterCallback<ClickEvent>(OnSaveButtonClicked);
            }
            
            if (_playerNameTextField != null)
            {
                _playerNameTextField.UnregisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
            }
        }
        
        #endregion
        
        #region UI Event Handlers - Only Report User Interactions
        
        /// <summary>
        /// Report new game request - state will handle UI transitions
        /// </summary>
        private void OnNewGameClicked(ClickEvent evt)
        {
            var playerName = _playerNameTextField?.value?.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player";
            }
            
            var difficulty = _difficultyDropdown?.value ?? "Normal";
            
            // Only report the user interaction - don't handle UI lifecycle
            var newGameEvent = new NewGameRequestedEvent
            {
                PlayerName = playerName,
                Difficulty = difficulty,
                StartingScene = "GameLevel1",
                CustomData = new Dictionary<string, object>
                {
                    ["creationTime"] = System.DateTime.Now.ToString(),
                    ["screenSource"] = nameof(NewGameScreen)
                }
            };
    
            _eventSystem?.Publish(newGameEvent);
        }
        
        private void OnSaveButtonClicked(ClickEvent evt)
        {
            var playerName = _playerNameTextField?.value?.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player";
            }
            
            var difficulty = _difficultyDropdown?.value ?? "Normal";
            
            // Only report the user interaction - don't handle UI lifecycle
            var newGameEvent = new NewGameRequestedEvent
            {
                PlayerName = playerName,
                Difficulty = difficulty,
                StartingScene = "GameLevel1",
                CustomData = new Dictionary<string, object>
                {
                    ["creationTime"] = System.DateTime.Now.ToString(),
                    ["screenSource"] = nameof(NewGameScreen)
                }
            };
    
            _eventSystem.Publish(SaveRequestedEvent.CreateRegularSave());

            //_eventSystem?.Publish(newGameEvent);
        }

        
        /// <summary>
        /// Report back button request - state will handle UI transitions
        /// </summary>
        private void OnBackButtonClicked(ClickEvent evt)
        {
            // Only report the user interaction - don't handle UI lifecycle
            var mainMenuEvent = new MainMenuRequestedEvent();
            _eventSystem?.Publish(mainMenuEvent);
            evt?.StopPropagation();
        }
        
        /// <summary>
        /// Handle keyboard shortcuts for user convenience
        /// </summary>
        private void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.KeypadEnter)
            {
                OnNewGameClicked(null);
                evt.StopPropagation();
            }
            else if (evt.keyCode == UnityEngine.KeyCode.Escape)
            {
                OnBackButtonClicked(null);
                evt.StopPropagation();
            }
        }
        
        #endregion
    }
}
