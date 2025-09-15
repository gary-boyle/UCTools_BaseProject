using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Interfaces;

namespace GameFramework.Services
{
    /// <summary>
    /// Service that handles all game loading operations using EventSystem
    /// Simplified to focus on loading workflow and state management
    /// File operations and validation delegated to utility classes
    /// </summary>
    public class LoadService : ILoadService
    {
        public bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }
        
        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly IGameDataService _gameDataService;
        private readonly IGameStateMachine _stateMachine;
        
        public LoadService(
            IEventSystem eventSystem,
            IGameDataService gameDataService,
            IGameStateMachine stateMachine)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }
        
        #region Lifecycle
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            SubscribeToEvents();
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            UnsubscribeFromEvents();
            
            IsInitialized = false;
        }
        
        private void SubscribeToEvents()
        {
            //_eventSystem.Subscribe<LoadGameRequestedEvent>(OnLoadGameRequested);
            //_eventSystem.Subscribe<LoadSaveFileEvent>(OnLoadSaveFileRequested);
        }
        
        private void UnsubscribeFromEvents()
        {
            //_eventSystem.Unsubscribe<LoadGameRequestedEvent>(OnLoadGameRequested);
            //_eventSystem.Unsubscribe<LoadSaveFileEvent>(OnLoadSaveFileRequested);
        }
        #endregion
    }
}
