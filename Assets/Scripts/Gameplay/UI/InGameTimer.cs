using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Provides a visual for the match time length
    /// </summary>
    public class InGameTimer : MonoBehaviour {
        public TMP_Text TimerText;

        private bool _timerStarted;
        private float _matchLength;
        
        private void Start() {
            SetText(TimeSpan.Zero);
            GameManager.Instance.GameSetupManager.GameInitializedEvent += StartTimer;
        }

        private void Update() {
            if (!_timerStarted) return;
            
            _matchLength += Time.deltaTime;
            TimeSpan currentMatchLength = TimeSpan.FromSeconds(_matchLength);
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

        [Button]
        private void AddMinute() {
            _matchLength += 60;
        }

        [Button]
        private void AddHour() {
            _matchLength += 60 * 60;
        }
    }
}