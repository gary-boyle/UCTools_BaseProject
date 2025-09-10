using System.Collections.Generic;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// New Game Screen that allows players to configure and start a new game
    /// Handles UI event registration/unregistration properly in OnShow/OnHide
    /// Includes proper back button navigation functionality using existing event system
    /// </summary>
    public class NewGameScreen : UIScreen
    {
        #region UI Elements
        
        private Button _confirmButton;
        private Button _backButton; // Added: Cache for back button
        private TextField _playerNameTextField;
        private DropdownField _difficultyDropdown;
        
        #endregion

        #region Services
        
        private readonly IEventSystem _eventSystem;
        
        #endregion

        public NewGameScreen(VisualElement rootElement) : base(rootElement)
        {
            // Get event system from DI container
            _eventSystem = GameManager.GetService<IEventSystem>();
            
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        #region Screen Lifecycle
        
        protected override void OnShow()
        {
            base.OnShow();
            
            // Register UI event handlers when screen becomes visible
            RegisterUIEventHandlers();
            
            // Set initial focus and default values
            SetInitialUIState();
        }
        
        protected override void OnHide()
        {
            // Unregister UI event handlers when screen becomes hidden
            UnregisterUIEventHandlers();
            
            base.OnHide();
        }
        
        #endregion
        
        #region UI Initialization
        
        private void InitializeUI()
        {
            // Cache UI elements
            _confirmButton = RootElement?.Q<Button>("btn_Confirm");
            _backButton = RootElement?.Q<Button>("btn_Back"); // Added: Cache back button
            _playerNameTextField = RootElement?.Q<TextField>("txt_PlayerName");
            _difficultyDropdown = RootElement?.Q<DropdownField>("dd_Difficulty");

            // Configure UI elements (but don't register events yet)
            SetupUIElements();
        }
        
        private void SetupUIElements()
        {
            // Set up difficulty dropdown options
            if (_difficultyDropdown != null)
            {
                _difficultyDropdown.choices.Clear();
                _difficultyDropdown.choices.AddRange(new[] { "Easy", "Normal", "Hard", "Expert" });
                _difficultyDropdown.SetValueWithoutNotify("Normal");
            }
        }
        
        private void SetInitialUIState()
        {
            // Focus on player name field and set default value if empty
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
        
        /// <summary>
        /// Registers all UI event handlers when the screen is shown
        /// </summary>
        private void RegisterUIEventHandlers()
        {
            // Register button click events
            if (_confirmButton != null)
            {
                _confirmButton.RegisterCallback<ClickEvent>(OnNewGameClicked);
            }
            
            // Added: Register back button click event
            if (_backButton != null)
            {
                _backButton.RegisterCallback<ClickEvent>(OnBackButtonClicked);
            }
            
            // Register text field key events
            if (_playerNameTextField != null)
            {
                _playerNameTextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
            }
        }
        
        /// <summary>
        /// Unregisters all UI event handlers when the screen is hidden
        /// </summary>
        private void UnregisterUIEventHandlers()
        {
            // Unregister button click events
            if (_confirmButton != null)
            {
                _confirmButton.UnregisterCallback<ClickEvent>(OnNewGameClicked);
            }
            
            // Added: Unregister back button click event
            if (_backButton != null)
            {
                _backButton.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
            }
            
            // Unregister text field key events
            if (_playerNameTextField != null)
            {
                _playerNameTextField.UnregisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
            }
        }
        
        #endregion
        
        #region UI Event Handlers
        
        /// <summary>
        /// Handles the confirm button click to start a new game
        /// </summary>
        private async void OnNewGameClicked(ClickEvent evt)
        {
            // Validate input
            var playerName = _playerNameTextField?.value?.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player"; // Fallback to default
            }
            
            var difficulty = _difficultyDropdown?.value ?? "Normal";
            
            // Create new game event with player configuration
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
    
            // Publish the event to start the new game
            _eventSystem?.Publish(newGameEvent);
        }
        
        /// <summary>
        /// Added: Handles the back button click to return to main menu
        /// Uses the existing MainMenuRequestedEvent from the event system
        /// </summary>
        private void OnBackButtonClicked(ClickEvent evt)
        {
            // Use existing event system to request navigation back to main menu
            var mainMenuEvent = new MainMenuRequestedEvent();
            
            // Publish the main menu request event
            _eventSystem?.Publish(mainMenuEvent);
            
            // Stop event propagation
            evt?.StopPropagation();
        }
        
        /// <summary>
        /// Handles key down events in the player name text field
        /// Allows Enter key to confirm new game creation and Escape key to go back
        /// </summary>
        private void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            // Allow Enter key to confirm new game
            if (evt.keyCode == UnityEngine.KeyCode.Return || evt.keyCode == UnityEngine.KeyCode.KeypadEnter)
            {
                // Trigger the same logic as clicking the confirm button
                OnNewGameClicked(null);
                evt.StopPropagation(); // Prevent further processing of the key event
            }
            // Added: Allow Escape key to go back to main menu
            else if (evt.keyCode == UnityEngine.KeyCode.Escape)
            {
                // Trigger the same logic as clicking the back button
                OnBackButtonClicked(null);
                evt.StopPropagation();
            }
        }
        
        #endregion
    }
}
