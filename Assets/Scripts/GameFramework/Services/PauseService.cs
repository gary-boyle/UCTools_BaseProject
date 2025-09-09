using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Simple centralized pause service that manages game pause state
    /// Handles time scale, audio, and event integration
    /// </summary>
    public class PauseService : IPauseService
    {
        #region Properties and Fields
        
        public bool IsInitialized { get; private set; }
        public bool IsPaused { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly IAudioService _audioService;
        
        // Pre-pause state restoration
        private float _prePauseTimeScale = 1f;
        private float _prePauseAudioVolume = 1f;
        
        // Events
        public event Action<bool> OnPauseStateChanged;
        public event Action<string> OnGamePaused;
        public event Action OnGameResumed;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public PauseService(IEventSystem eventSystem, IAudioService audioService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }
        
        #endregion
        
        #region Initialization and Shutdown
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[PauseService] Initializing pause service...");
            
            // Subscribe to application focus events
            Application.focusChanged += OnApplicationFocusChanged;
            //Application.pauseStateChanged += OnApplicationPauseStateChanged;
            
            // Subscribe to pause/resume events
            _eventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
            
            // Initialize with unpaused state
            IsPaused = false;
            
            IsInitialized = true;
            await Task.CompletedTask;
            
            Debug.Log("[PauseService] Pause service initialized successfully");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[PauseService] Shutting down pause service...");
            
            // Unsubscribe from application events
            Application.focusChanged -= OnApplicationFocusChanged;
            //Application.pauseStateChanged -= OnApplicationPauseStateChanged;
            
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
        
        public void PauseGame(string reason = null)
        {
            if (IsPaused) return; // Already paused
            
            Debug.Log($"[PauseService] Pausing game - Reason: {reason ?? "No reason specified"}");
            
            // Store current state before pausing
            _prePauseTimeScale = Time.timeScale;
            _prePauseAudioVolume = _audioService.GetMasterVolume();
            
            // Apply pause effects
            IsPaused = true;
            Time.timeScale = 0f;
            _audioService.SetMasterVolume(_prePauseAudioVolume * 0.3f); // Reduce volume
            
            // Fire events
            OnPauseStateChanged?.Invoke(true);
            OnGamePaused?.Invoke(reason ?? "Game paused");
            _eventSystem.Publish(new GamePausedEvent());
            
            Debug.Log("[PauseService] Game paused successfully");
        }
        
        public void ResumeGame()
        {
            Debug.Log($"!!! {IsPaused}");

            if (!IsPaused) return; // Already resumed
            
            Debug.Log("[PauseService] Resuming game...");
            
            // Restore pre-pause state
            IsPaused = false;
            Time.timeScale = _prePauseTimeScale;
            _audioService.SetMasterVolume(_prePauseAudioVolume);
            
            // Fire events
            OnPauseStateChanged?.Invoke(false);
            OnGameResumed?.Invoke();
            _eventSystem.Publish(new GameResumedEvent());
            
            Debug.Log("[PauseService] Game resumed successfully");
        }
        
        public void TogglePause()
        {
            if (IsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame("Player toggle");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnPauseRequested(PauseRequestedEvent evt)
        {
            PauseGame("Player requested pause");
        }
        
        private void OnResumeRequested(ResumeRequestedEvent evt)
        {
            ResumeGame();
        }
        
        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                // Don't automatically resume - let player decide
                Debug.Log("[PauseService] Application gained focus - game remains in current pause state");
            }
            else
            {
                PauseGame("Application lost focus");
            }
        }
        
        private void OnApplicationPauseStateChanged(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseGame("Application paused");
            }
            else
            {
                // Don't automatically resume - let player decide
                Debug.Log("[PauseService] Application unpaused - game remains in current pause state");
            }
        }
        
        #endregion
    }
}
