using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;
using System.Text;
using GameFramework.ConsoleTool.Interfaces;
using GameFramework.Input.Interfaces; // Updated import

namespace GameFramework.ConsoleTool
{
    /// <summary>
    /// Console GUI component responsible for rendering console UI and handling user input.
    /// 
    /// Architecture:
    /// - Pure UI component - only handles visual representation
    /// - Subscribes to console-specific input events (not toggle - that's handled by ConsoleService)
    /// - Uses events to communicate back to console system
    /// - Optimized string building for better performance
    /// 
    /// Input Handling Flow:
    /// 1. InputManager detects console input (submit, tab, history) 
    /// 2. InputManager publishes console input events
    /// 3. ConsoleGUI receives events and updates UI accordingly
    /// 4. ConsoleGUI calls Console static methods to execute commands
    /// </summary>
    public class ConsoleGUI : MonoBehaviour, IConsoleUI
    {
        #region Constants
        private const int MAX_LINES = 100;
        private const float BACKGROUND_ALPHA = 0.5f;
        private const string LOG_PREFIX = "[ConsoleGUI]";
        #endregion

        #region UI State
        private readonly List<string> _lines = new List<string>(MAX_LINES);
        private readonly StringBuilder _stringBuilder = new StringBuilder(1024); // Pre-allocate for efficiency
        private int _wantedCaretPosition = -1;
        private bool _isInitialized = false;
        #endregion

        #region UI References
        [Header("Console UI Components")]
        [SerializeField] private Transform panel;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text textArea;
        [SerializeField] private Image textAreaBackground;
        [SerializeField] private TMP_Text buildIdText;
        #endregion

        #region Dependencies (Injected via Service Locator)
        private IInputManager _inputManager; // Changed from IInputService
        private IEventSystem _eventSystem;
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateDependencies();
            SetupEventSubscriptions();
        }

        void OnDestroy()
        {
            CleanupEventSubscriptions();
        }

        #endregion

        #region IConsoleUI Implementation

        /// <summary>
        /// Initialize the console UI
        /// </summary>
        public void Init()
        {
            if (buildIdText != null)
            {
                buildIdText.text = $"Unity {Application.unityVersion}";
            }
            
            _isInitialized = true;
        }

        /// <summary>
        /// Shutdown the console UI
        /// </summary>
        public void Shutdown()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Add a line of text to the console output
        /// Uses StringBuilder for efficient string concatenation
        /// </summary>
        public void OutputString(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _lines.Add(message);

            // Limit the number of lines to prevent memory bloat
            if (_lines.Count > MAX_LINES)
            {
                _lines.RemoveAt(0);
            }

            UpdateTextArea();
        }

        /// <summary>
        /// Check if console is currently visible
        /// </summary>
        public bool IsOpen()
        {
            return panel != null && panel.gameObject.activeSelf;
        }

        /// <summary>
        /// Show or hide the console
        /// </summary>
        public void SetOpen(bool open)
        {
            if (panel == null) 
            {
                Debug.LogError($"{LOG_PREFIX} Panel reference is null!");
                return;
            }

            panel.gameObject.SetActive(open);

            if (open)
            {
                ActivateInputField();
            }
            else
            {
                DeactivateInputField();
            }
        }

        /// <summary>
        /// Update console UI (called every frame when console is open)
        /// </summary>
        public void ConsoleUpdate()
        {
            if (!IsOpen() || !_isInitialized) return;

            UpdateBackgroundAlpha();
        }

        /// <summary>
        /// Late update for console UI (handles caret positioning after UI events)
        /// </summary>
        public void ConsoleLateUpdate()
        {
            if (_wantedCaretPosition > -1 && inputField != null)
            {
                inputField.caretPosition = _wantedCaretPosition;
                _wantedCaretPosition = -1;
            }
        }

        /// <summary>
        /// Set console prompt text (not currently used but part of interface)
        /// </summary>
        public void SetPrompt(string prompt)
        {
            // Could be implemented to show different prompts
            // e.g., ">" for commands, "?" for help mode, etc.
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validate that all required dependencies are available
        /// </summary>
        private void ValidateDependencies()
        {
            // Check for Unity EventSystem
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                Debug.LogError($"{LOG_PREFIX} No Unity EventSystem found! UGUI input won't work.");
            }

            // ✅ UPDATED: Get services from DI container with correct interface
            _inputManager = GameManager.GetService<IInputManager>(); // Changed from IInputService
            _eventSystem = GameManager.GetService<IEventSystem>();

            if (_inputManager == null)
            {
                Debug.LogError($"{LOG_PREFIX} Could not get InputManager from GameManager!");
            }

            if (_eventSystem == null)
            {
                Debug.LogError($"{LOG_PREFIX} Could not get EventSystem from GameManager!");
            }
        }

        /// <summary>
        /// Subscribe to console-specific input events
        /// Note: Does NOT subscribe to toggle events - that's handled by ConsoleService
        /// </summary>
        private void SetupEventSubscriptions()
        {
            if (_eventSystem == null) return;

            _eventSystem.Subscribe<ConsoleSubmitInputEvent>(OnConsoleSubmitEvent);
            _eventSystem.Subscribe<ConsoleTabCompleteInputEvent>(OnConsoleTabCompleteEvent);
            _eventSystem.Subscribe<ConsoleHistoryUpInputEvent>(OnConsoleHistoryUpEvent);
            _eventSystem.Subscribe<ConsoleHistoryDownInputEvent>(OnConsoleHistoryDownEvent);

            Debug.Log($"{LOG_PREFIX} Subscribed to console input events");
        }

        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        private void CleanupEventSubscriptions()
        {
            if (_eventSystem == null) return;

            _eventSystem.Unsubscribe<ConsoleSubmitInputEvent>(OnConsoleSubmitEvent);
            _eventSystem.Unsubscribe<ConsoleTabCompleteInputEvent>(OnConsoleTabCompleteEvent);
            _eventSystem.Unsubscribe<ConsoleHistoryUpInputEvent>(OnConsoleHistoryUpEvent);
            _eventSystem.Unsubscribe<ConsoleHistoryDownInputEvent>(OnConsoleHistoryDownEvent);
        }

        /// <summary>
        /// Efficiently update the text area using StringBuilder
        /// </summary>
        private void UpdateTextArea()
        {
            if (textArea == null) return;

            _stringBuilder.Clear();

            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0) _stringBuilder.AppendLine();
                _stringBuilder.Append(_lines[i]);
            }

            textArea.text = _stringBuilder.ToString();
        }

        /// <summary>
        /// Update background transparency
        /// </summary>
        private void UpdateBackgroundAlpha()
        {
            if (textAreaBackground != null)
            {
                var color = textAreaBackground.color;
                color.a = BACKGROUND_ALPHA;
                textAreaBackground.color = color;
            }
        }

        /// <summary>
        /// Activate input field for typing
        /// </summary>
        private void ActivateInputField()
        {
            if (inputField == null) return;

            // Use a simple delayed activation to ensure UI is ready
            StartCoroutine(ActivateInputFieldDelayed());
        }

        /// <summary>
        /// Deactivate input field
        /// </summary>
        private void DeactivateInputField()
        {
            if (inputField != null && inputField.isFocused)
            {
                inputField.DeactivateInputField();
            }
        }

        /// <summary>
        /// Coroutine to activate input field after UI is ready
        /// </summary>
        private System.Collections.IEnumerator ActivateInputFieldDelayed()
        {
            yield return new WaitForEndOfFrame();
            
            if (inputField != null)
            {
                inputField.ActivateInputField();
                inputField.Select();
            }
        }

        /// <summary>
        /// Handle command submission
        /// </summary>
        private void SubmitCommand()
        {
            if (inputField == null) return;

            string command = inputField.text?.Trim() ?? string.Empty;
            
            if (!string.IsNullOrEmpty(command))
            {
                // Queue command for execution
                Console.EnqueueCommand(command);
            }

            // Clear input field and maintain focus
            inputField.text = string.Empty;
            inputField.ActivateInputField();
        }

        #endregion

        #region Input Event Handlers
        
        /// <summary>
        /// Handle console submit input (Enter key)
        /// </summary>
        private void OnConsoleSubmitEvent(ConsoleSubmitInputEvent inputEvent)
        {
            Debug.Log($"{LOG_PREFIX} Submit event received - Phase: {inputEvent.Phase}, Console open: {IsOpen()}, Input focused: {inputField?.isFocused}");
            
            if (!IsOpen()) return;
            
            // Remove the strict phase check for now - some input might use different phases
            HandleSubmit();
        }

        /// <summary>
        /// Handle tab completion input
        /// </summary>
        private void OnConsoleTabCompleteEvent(ConsoleTabCompleteInputEvent inputEvent)
        {
            if (!IsOpen() || inputField == null || !inputField.isFocused) return;
            
            // Only tab complete if cursor is at end of text
            if (inputField.caretPosition != inputField.text.Length || inputField.text.Length <= 0) return;
            
            string completed = Console.TabComplete(inputField.text);
            inputField.text = completed;
            inputField.caretPosition = completed.Length;
        }

        /// <summary>
        /// Handle history up input (previous command)
        /// </summary>
        private void OnConsoleHistoryUpEvent(ConsoleHistoryUpInputEvent inputEvent)
        {
            if (!IsOpen() || inputField == null || !inputField.isFocused) return;
            
            string historyCommand = Console.HistoryUp(inputField.text);
            if (!string.IsNullOrEmpty(historyCommand))
            {
                inputField.text = historyCommand;
                _wantedCaretPosition = historyCommand.Length;
            }
        }

        /// <summary>
        /// Handle history down input (next command)
        /// </summary>
        private void OnConsoleHistoryDownEvent(ConsoleHistoryDownInputEvent inputEvent)
        {
            if (!IsOpen() || inputField == null || !inputField.isFocused) return;
            
            string historyCommand = Console.HistoryDown();
            inputField.text = historyCommand ?? string.Empty;
            _wantedCaretPosition = inputField.text.Length;
        }

        /// <summary>
        /// Handle command submission (back to original working logic)
        /// </summary>
        private void HandleSubmit()
        {
            if (inputField == null) return;

            string value = inputField.text;
            
            inputField.text = "";
            inputField.ActivateInputField();

            // Only enqueue if there's actually a command
            if (!string.IsNullOrWhiteSpace(value))
            {
                Console.EnqueueCommand(value);
            }
        }
        #endregion
    }
}
