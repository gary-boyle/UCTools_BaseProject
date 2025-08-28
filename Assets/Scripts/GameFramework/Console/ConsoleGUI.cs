using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;

namespace UCTools_CommandConsole
{
    public class ConsoleGUI : MonoBehaviour, IConsoleUI
    {
        List<string> m_Lines = new List<string>();
        int m_WantedCaretPosition = -1;

        [Header("UI References")]
        [SerializeField] Transform panel;
        [SerializeField] TMP_InputField input_field;
        [SerializeField] TMP_Text text_area;
        [SerializeField] Image text_area_background;
        [SerializeField] TMP_Text buildIdText;
        
        // Reference to services (injected via DI)
        private IInputService _inputService;
        private IEventSystem _eventSystem;
        private bool _isInitialized = false;
        
        void Awake()
        {
            input_field.onEndEdit.AddListener(OnSubmit);
        }
        
        void Start()
        {
            // Check for EventSystem
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                Debug.LogError("[ConsoleGUI] No EventSystem found! UGUI input won't work without it.");
            }
            
            // Get services from DI container
            _inputService = GameManager.GetService<IInputService>();
            _eventSystem = GameManager.GetService<IEventSystem>();
            
            if (_eventSystem != null)
            {
                // Subscribe to console input events
                _eventSystem.Subscribe<ConsoleSubmitInputEvent>(OnConsoleSubmitEvent);
                _eventSystem.Subscribe<ConsoleTabCompleteInputEvent>(OnConsoleTabCompleteEvent);
                _eventSystem.Subscribe<ConsoleHistoryUpInputEvent>(OnConsoleHistoryUpEvent);
                _eventSystem.Subscribe<ConsoleHistoryDownInputEvent>(OnConsoleHistoryDownEvent);
                
                Debug.Log("[ConsoleGUI] Subscribed to console input events");
            }
            else
            {
                Debug.LogError("[ConsoleGUI] Could not get EventSystem from GameManager!");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<ConsoleSubmitInputEvent>(OnConsoleSubmitEvent);
                _eventSystem.Unsubscribe<ConsoleTabCompleteInputEvent>(OnConsoleTabCompleteEvent);
                _eventSystem.Unsubscribe<ConsoleHistoryUpInputEvent>(OnConsoleHistoryUpEvent);
                _eventSystem.Unsubscribe<ConsoleHistoryDownInputEvent>(OnConsoleHistoryDownEvent);
            }
        }

        public void Init()
        {
            buildIdText.text = Application.unityVersion;
            _isInitialized = true;
        }

        public void Shutdown()
        {
            if (_inputService != null)
            {
                _inputService.EnableConsoleInput(false);
            }
        }

        public void OutputString(string s)
        {
            m_Lines.Add(s);
            var count = Mathf.Min(100, m_Lines.Count);
            var start = m_Lines.Count - count;
            text_area.text = string.Join("\n", m_Lines.GetRange(start, count).ToArray());
        }

        public bool IsOpen()
        {
            return panel.gameObject.activeSelf;
        }

        public void SetOpen(bool open)
        {
            Debug.Log($"[ConsoleGUI] Setting console open: {open}");
            panel.gameObject.SetActive(open);
    
            // Enable/disable console input based on console state
            if (_inputService != null)
            {
                _inputService.EnableConsoleInput(open);
            }
    
            if (open)
            {
                // Only activate input field when opening, then leave it alone
                StartCoroutine(ActivateInputFieldDelayed());
            }
        }

        private System.Collections.IEnumerator ActivateInputFieldDelayed()
        {
            yield return new WaitForEndOfFrame();
            input_field.ActivateInputField();
            input_field.Select();
            Debug.Log("[ConsoleGUI] Input field activated once on console open");
        }

        public void ConsoleUpdate()
        {
            if (!IsOpen() || !_isInitialized)
                return;

            // Just handle the background alpha
            var c = text_area_background.color;
            c.a = 0.5f;
            text_area_background.color = c;

            // Don't mess with input field focus during update - let UGUI handle it
        }

        public void ConsoleLateUpdate()
        {
            // Only manipulate caret when we specifically need to (history navigation)
            if (m_WantedCaretPosition > -1)
            {
                input_field.caretPosition = m_WantedCaretPosition;
                m_WantedCaretPosition = -1;
            }
        }

        private void OnConsoleSubmitEvent(ConsoleSubmitInputEvent inputEvent)
        {
            if (!IsOpen()) return;
    
            // Only handle submit if input field is focused AND we're not in the middle of typing
            if (input_field.isFocused && inputEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                HandleSubmit();
            }
        }

        private void OnConsoleTabCompleteEvent(ConsoleTabCompleteInputEvent inputEvent)
        {
            if (!IsOpen() || !input_field.isFocused) return;

            if (inputEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                if (input_field.caretPosition == input_field.text.Length && input_field.text.Length > 0)
                {
                    var res = Console.TabComplete(input_field.text);
                    input_field.text = res;
                    input_field.caretPosition = res.Length;
                }
            }
        }

        private void OnConsoleHistoryUpEvent(ConsoleHistoryUpInputEvent inputEvent)
        {
            if (!IsOpen() || !input_field.isFocused) return;

            if (inputEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                input_field.text = Console.HistoryUp(input_field.text);
                m_WantedCaretPosition = input_field.text.Length;
            }
        }

        private void OnConsoleHistoryDownEvent(ConsoleHistoryDownInputEvent inputEvent)
        {
            if (!IsOpen() || !input_field.isFocused) return;

            if (inputEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                input_field.text = Console.HistoryDown();
                m_WantedCaretPosition = input_field.text.Length;
            }
        }

        private void HandleSubmit()
        {
            string value = input_field.text;
            input_field.text = "";
            input_field.ActivateInputField();

            Console.EnqueueCommand(value);
        }

        void OnSubmit(string value)
        {
            // Fallback for TMP_InputField's onEndEdit event
        }

        public void SetPrompt(string prompt)
        {
        }
    }
}
