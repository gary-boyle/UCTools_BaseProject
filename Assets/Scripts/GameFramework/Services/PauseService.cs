using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.Input.Interfaces;
using GameFramework.Input;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Simple centralized pause service that manages game pause state
    /// Handles time scale, audio, and event integration
    /// Uses EventSystem for all event publishing instead of local events
    /// </summary>
    public class PauseService : IPauseService
    {
        #region Properties and Fields
        
        public bool IsInitialized { get; private set; }
        public bool IsPaused { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private IInputManager _inputManager;
        
        // Pre-pause state restoration
        private float _prePauseTimeScale = 1f;
        private InputContext _prePauseInputContext = InputContext.None;
        
        // Removed local events - now using EventSystem
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public PauseService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        }
        
        #endregion
        
        #region Initialization and Shutdown
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            // Subscribe to application focus events
            //Application.focusChanged += OnApplicationFocusChanged;
            _inputManager = GameManager.GetService<IInputManager>();

            // Subscribe to pause/resume events
            _eventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
            
            // Initialize with unpaused state
            IsPaused = false;
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            // Unsubscribe from application events
            //Application.focusChanged -= OnApplicationFocusChanged;
            
            // Unsubscribe from game events
            _eventSystem?.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem?.Unsubscribe<ResumeRequestedEvent>(OnResumeRequested);
            
            // Ensure game is resumed
            if (IsPaused)
            {
                ResumeGame();
            }
            
            IsInitialized = false;
        }
        
        #endregion
        
        #region Update Loop
        
        public void Update()
        {
            // This service doesn't need frame updates, but implements IUpdatable 
            // in case we need to add frame-based pause logic later
        }
        
        #endregion
        
        #region Pause Control

        private void PauseGame()
        {
            if (IsPaused) return; // Already paused
            
            // Store current state before pausing
            _prePauseTimeScale = Time.timeScale;
            _prePauseInputContext = _inputManager.GetCurrentContext();
            
            // Apply pause effects
            IsPaused = true;
            Time.timeScale = 0f;
            
            // Switch to UI-only input to block player interactions (mouse raycasts, movement, etc.)
            // but allow UI interactions like pause menu navigation
            _inputManager.SetInputContext(InputContext.UI);
            
            // Publish events through EventSystem only
            _eventSystem.Publish(new GamePausedEvent());
        }
        
        private void ResumeGame()
        {
            if (!IsPaused) return; // Already resumed
            
            // Restore pre-pause state
            IsPaused = false;
            Time.timeScale = _prePauseTimeScale;
            
            // Restore previous input context to re-enable player interactions
            _inputManager.SetInputContext(_prePauseInputContext);
            
            //Debug.Log($"[PauseService] Game resumed (timeScale: {Time.timeScale})");
            
            // Publish events through EventSystem only
            _eventSystem.Publish(new GameResumedEvent());
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnPauseRequested(PauseRequestedEvent evt)
        {
            PauseGame();
        }
        
        private void OnResumeRequested(ResumeRequestedEvent evt)
        {
            ResumeGame();
        }
        
        // private void OnApplicationFocusChanged(bool hasFocus)
        // {
        //     // Only pause when losing focus, resume when gaining focus
        //     if (!hasFocus)
        //     {
        //         PauseGame();
        //         Debug.Log("[PauseService] Application lost focus - pausing game");
        //     }
        //     else
        //     {
        //         ResumeGame();
        //         Debug.Log("[PauseService] Application gained focus - resuming game");
        //     }
        // }
        //
        #endregion
    }
}
