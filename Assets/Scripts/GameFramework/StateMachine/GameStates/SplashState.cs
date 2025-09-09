using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Splash state with event-driven input handling and constructor injection
    /// </summary>
    public class SplashState : BaseGameState
    {
        private float _timer;
        private const float SPLASH_DURATION = 3f;
        private bool _skipRequested = false;
    
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public SplashState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Splash, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            InputManager.SetInputContext(InputContext.UI);

            // Show splash UI using injected UI service
            await UIService.ShowScreenAsync<SplashScreen>();
        
            // Start background music using injected audio service
            AudioService.PlayMusic("splash_music");
        
            _timer = 0f;
            _skipRequested = false;
            
            // Subscribe to input events for skipping splash screen
            EventSystem.Subscribe<UISubmitInputEvent>(OnSkipSplash);
            EventSystem.Subscribe<UICancelInputEvent>(OnSkipSplash);
            EventSystem.Subscribe<UIClickInputEvent>(OnSkipSplash);
            EventSystem.Subscribe<PlayerAttackInputEvent>(OnSkipSplash);
            EventSystem.Subscribe<PlayerJumpInputEvent>(OnSkipSplash);
            EventSystem.Subscribe<PlayerInteractInputEvent>(OnSkipSplash);
            
            // You could also subscribe to any other input that should skip the splash
            // EventSystem.Subscribe<PlayerMoveInputEvent>(OnMovementSkipSplash);
        }
    
        public override void Update()
        {
            _timer += Time.deltaTime;
        
            // Auto-advance after duration or if skip was requested via input events
            if (_timer >= SPLASH_DURATION || _skipRequested)
            {
                TransitionToMainMenuAsync();
            }
        }
        
        #region Input Event Handlers
        
        /// <summary>
        /// Handle skip splash input (Submit, Cancel, Click, Attack, Jump, Interact)
        /// </summary>
        private void OnSkipSplash(UISubmitInputEvent evt)
        {
            RequestSkip();
        }
        
        private void OnSkipSplash(UICancelInputEvent evt)
        {
            RequestSkip();
        }
        
        private void OnSkipSplash(UIClickInputEvent evt)
        {
            if (evt.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                RequestSkip();
            }
        }
        
        private void OnSkipSplash(PlayerAttackInputEvent evt)
        {
            if (evt.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                RequestSkip();
            }
        }
        
        private void OnSkipSplash(PlayerJumpInputEvent evt)
        {
            RequestSkip();
        }
        
        private void OnSkipSplash(PlayerInteractInputEvent evt)
        {
            if (evt.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                RequestSkip();
            }
        }

        /// <summary>
        /// Request splash screen skip
        /// </summary>
        private void RequestSkip()
        {
            if (!_skipRequested)
            {
                _skipRequested = true;
                Debug.Log("[SplashState] Skip requested via input");
            }
        }
        
        #endregion
    
        private async void TransitionToMainMenuAsync()
        {
            // Use injected state machine for transition
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
    
        public override async Task ExitAsync()
        {
            // Unsubscribe from input events
            EventSystem.Unsubscribe<UISubmitInputEvent>(OnSkipSplash);
            EventSystem.Unsubscribe<UICancelInputEvent>(OnSkipSplash);
            EventSystem.Unsubscribe<UIClickInputEvent>(OnSkipSplash);
            EventSystem.Unsubscribe<PlayerAttackInputEvent>(OnSkipSplash);
            EventSystem.Unsubscribe<PlayerJumpInputEvent>(OnSkipSplash);
            EventSystem.Unsubscribe<PlayerInteractInputEvent>(OnSkipSplash);
            
            // EventSystem.Unsubscribe<PlayerMoveInputEvent>(OnMovementSkipSplash);
            
            await UIService.HideScreenAsync<SplashScreen>();
            await base.ExitAsync();
        }
    }
}
