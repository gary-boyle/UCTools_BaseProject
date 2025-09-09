using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UnityEngine;

namespace GameFramework.Input.Handlers
{
    /// <summary>
    /// Handles player input - only active during gameplay
    /// Lower priority than UI to ensure menus can override player actions
    /// </summary>
    public class PlayerInputHandler : InputHandlerBase
    {
        private readonly IPauseService _pauseService;
        
        public PlayerInputHandler(IEventSystem eventSystem, IPauseService pauseService)
            : base("Player", 400, eventSystem, false) // Lower priority, don't consume by default
        {
            _pauseService = pauseService;
        }
        
        protected override void SubscribeToEvents()
        {
            _eventSystem.Subscribe<PlayerMoveInputEvent>(OnPlayerMove);
            _eventSystem.Subscribe<PlayerLookInputEvent>(OnPlayerLook);
            _eventSystem.Subscribe<PlayerAttackInputEvent>(OnPlayerAttack);
            _eventSystem.Subscribe<PlayerJumpInputEvent>(OnPlayerJump);
            _eventSystem.Subscribe<PlayerPauseInputEvent>(OnPlayerPause);
        }
        
        protected override void UnsubscribeFromEvents()
        {
            _eventSystem.Unsubscribe<PlayerMoveInputEvent>(OnPlayerMove);
            _eventSystem.Unsubscribe<PlayerLookInputEvent>(OnPlayerLook);
            _eventSystem.Unsubscribe<PlayerAttackInputEvent>(OnPlayerAttack);
            _eventSystem.Unsubscribe<PlayerJumpInputEvent>(OnPlayerJump);
            _eventSystem.Unsubscribe<PlayerPauseInputEvent>(OnPlayerPause);
        }
        
        public override bool HandleInput<T>(T inputEvent)
        {
            // Don't handle input if game is paused
            return _pauseService.IsPaused;
        }
        
        private void OnPlayerMove(PlayerMoveInputEvent evt)
        {
            if (_pauseService.IsPaused) return;
            
            // Forward to player movement system
            Debug.Log($"[PlayerInputHandler] Move: {evt.MovementVector}");
        }
        
        private void OnPlayerLook(PlayerLookInputEvent evt)
        {
            if (_pauseService.IsPaused) return;
            
            // Forward to camera system
            // Debug.Log($"[PlayerInputHandler] Look: {evt.LookDelta}"); // Too frequent for logging
        }
        
        private void OnPlayerAttack(PlayerAttackInputEvent evt)
        {
            if (_pauseService.IsPaused) return;
            
            Debug.Log($"[PlayerInputHandler] Attack: {evt.Phase}");
        }
        
        private void OnPlayerJump(PlayerJumpInputEvent evt)
        {
            if (_pauseService.IsPaused) return;
            
            Debug.Log("[PlayerInputHandler] Jump");
        }
        
        private void OnPlayerPause(PlayerPauseInputEvent evt)
        {
            // Pause input should work even when paused (to unpause)
            _eventSystem.Publish(new PauseRequestedEvent());
        }
    }
}
