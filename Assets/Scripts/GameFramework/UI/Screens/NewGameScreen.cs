using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// New Game Screen - pure UI component that only reports user interactions
    /// Does not handle its own lifecycle - that's managed by the NewGameState
    /// Provides form validation and user feedback
    /// </summary>
    public class NewGameScreen : UIScreen
    {
        #region UI Elements
        
        private Button _confirmButton;
        private Button _backButton;
        
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
        
        /// <summary>
        /// Reset UI state when screen is shown again
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();
            RegisterUIEventHandlers();
            SetInitialUIState();
            EnableConfirmButton(); // Re-enable button when screen is shown
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

            // Set up confirm button text for clarity
            if (_confirmButton != null)
            {
                _confirmButton.text = "Start New Game";
            }
        }
        
        private void SetInitialUIState()
        {
            if (_playerNameTextField != null)
            {
                // Focus the text field for immediate typing
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
            _confirmButton?.RegisterCallback<ClickEvent>(OnNewGameClicked);
            _backButton?.RegisterCallback<ClickEvent>(OnBackButtonClicked);
            _playerNameTextField?.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
        }
        
        private void UnregisterUIEventHandlers()
        {
            _confirmButton?.UnregisterCallback<ClickEvent>(OnNewGameClicked);
            _backButton?.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
            _playerNameTextField?.UnregisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
        }
        
        #endregion
        
        #region UI Event Handlers - Only Report User Interactions
        
        /// <summary>
        /// Report new game request with validation - state will handle game creation and transitions
        /// </summary>
        private void OnNewGameClicked(ClickEvent evt)
        {
            // Basic client-side validation
            string playerName = _playerNameTextField?.value?.Trim() ?? "";
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player"; // Default fallback
            }
            
            string difficulty = _difficultyDropdown?.value ?? "Normal";
            
            // Validate difficulty selection
            if (!IsValidDifficulty(difficulty))
            {
                Debug.LogWarning($"[NewGameScreen] Invalid difficulty selected: {difficulty}, defaulting to Normal");
                difficulty = "Normal";
            }
            
            Debug.Log($"[NewGameScreen] New game requested - Player: '{playerName}', Difficulty: '{difficulty}'");
            
            // Report the user interaction - state will handle the actual game creation
            var newGameEvent = new NewGameRequestedEvent
            {
                PlayerName = playerName,
                Difficulty = difficulty,
                StartingScene = "GameLevel1" // Default starting scene
            };
    
            _eventSystem?.Publish(newGameEvent);
            
            // Provide immediate visual feedback
            DisableConfirmButton();
        }
        
        /// <summary>
        /// Report back button request - state will handle UI transitions
        /// </summary>
        private void OnBackButtonClicked(ClickEvent evt)
        {
            Debug.Log("[NewGameScreen] Back button clicked, requesting return to main menu");
            
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
        
        #region Helper Methods
        
        /// <summary>
        /// Validates if the selected difficulty is valid
        /// </summary>
        private bool IsValidDifficulty(string difficulty)
        {
            var validDifficulties = new[] { "Easy", "Normal", "Hard", "Expert" };
            return Array.Exists(validDifficulties, d => d.Equals(difficulty, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Disables confirm button to prevent double-clicks during transition
        /// </summary>
        private void DisableConfirmButton()
        {
            if (_confirmButton != null)
            {
                _confirmButton.SetEnabled(false);
                _confirmButton.text = "Starting...";
            }
        }
        
        /// <summary>
        /// Re-enables confirm button (called when returning to this screen)
        /// </summary>
        private void EnableConfirmButton()
        {
            if (_confirmButton != null)
            {
                _confirmButton.SetEnabled(true);
                _confirmButton.text = "Start New Game";
            }
        }
        
        #endregion

    }
}
