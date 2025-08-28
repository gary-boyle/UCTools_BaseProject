using System;
using System.Threading.Tasks;

namespace GameFramework.Services.Interfaces
{
    public interface IInputService : IGameService
    {
        bool IsInitialized { get; }
        
        Task InitializeAsync();
        void Shutdown();
        // void Update();
        
        // Action map control
        void EnableActionMap(string mapName);
        void DisableActionMap(string mapName);
        
        // Console input methods
        void EnableConsoleInput(bool enable);
        void SetConsoleInputEnabled(bool open);

        // bool IsConsoleTogglePressed();
        // bool IsConsoleSubmitPressed();
        // bool IsConsoleTabCompletePressed();
        // bool IsConsoleHistoryUpPressed();
        // bool IsConsoleHistoryDownPressed();
        
        // Input state access
        UnityEngine.Vector2 GetMovementInput();
        UnityEngine.Vector2 GetLookInput();
        UnityEngine.Vector2 GetMousePosition();
        
        // Advanced access
        InputSystem_Actions GetInputActions();
    }
}