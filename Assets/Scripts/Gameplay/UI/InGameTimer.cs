using System;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Provides a visual for the match time length
    /// </summary>
    public class InGameTimer : MonoBehaviour {
        public TMP_Text TimerText;

        public string MatchLengthString => TimeSpan.FromSeconds(MatchLength).ToString("g");
        
        private bool _timerStarted;
        private float MatchLength => GameManager.Instance.CommandManager.AbilityExecutor.MatchLength;

        private GameSetupManager GameSetupManager => GameManager.Instance.GameSetupManager;
        
        private void Start() {
            SetText(TimeSpan.Zero);
            if (GameSetupManager.GameRunning) {
                StartTimer();
            } else {
                GameManager.Instance.GameSetupManager.GameRunningEvent += StartTimer;
            }
        }

        private void Update() {
            if (!_timerStarted) return;
            
            TimeSpan currentMatchLength = TimeSpan.FromSeconds(MatchLength);
            SetText(currentMatchLength);
        }
        
        private void StartTimer() {
            _timerStarted = true;
        }

        private void SetText(TimeSpan time) {
            var timeString = time.TotalMinutes < 1 ? $"0:{time:ss}"
                    // : time.TotalMinutes < 10 ? time.ToString(@"m\:ss")
                : time.TotalHours < 1 ? time.ToString(@"m\:ss")
                    // : time.TotalHours < 10 ? $"{time:h}:{time:mm}:{time:ss}"
                : time.ToString(@"h\:mm\:ss");
            TimerText.text = timeString;
        }
    }
}