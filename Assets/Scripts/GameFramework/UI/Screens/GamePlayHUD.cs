using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Gameplay HUD screen implementation
    /// </summary>
    public class GameplayHUD : UIScreen
    {
        private Label _scoreLabel;
        private Label _healthLabel;
        private Label _timeLabel;
        private Button _pauseButton;
    
        public GameplayHUD(VisualElement rootElement) : base(rootElement)
        {
            InitializeHUD();
        }
    
        private void InitializeHUD()
        {
            _scoreLabel = RootElement?.Q<Label>("ScoreLabel");
            _healthLabel = RootElement?.Q<Label>("HealthLabel");
            _timeLabel = RootElement?.Q<Label>("TimeLabel");
            _pauseButton = RootElement?.Q<Button>("PauseButton");
        
            _pauseButton?.RegisterCallback<ClickEvent>(OnPauseClicked);
        }
    
        private void OnPauseClicked(ClickEvent evt)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new PauseRequestedEvent());
        }
    
        public void UpdateScore(int score)
        {
            if (_scoreLabel != null)
                _scoreLabel.text = $"Score: {score}";
        }
    
        public void UpdateHealth(int health, int maxHealth)
        {
            if (_healthLabel != null)
                _healthLabel.text = $"Health: {health}/{maxHealth}";
        }
    
        public void UpdateTime(float timeInSeconds)
        {
            if (_timeLabel != null)
            {
                var minutes = Mathf.FloorToInt(timeInSeconds / 60);
                var seconds = Mathf.FloorToInt(timeInSeconds % 60);
                _timeLabel.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }
}