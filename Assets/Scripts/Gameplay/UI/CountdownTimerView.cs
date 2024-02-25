using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Plays a countdown animation before the start of the game. 
    /// </summary>
    public class CountdownTimerView : MonoBehaviour {
        public GameObject View;
        public TMP_Text TimerText;
        public TMP_Text LoadingText;
        public string TimerMessage = "Game start in {0}";

        private float _remainingCountdownTime;
        private bool _timerActive;

        public void ShowLoadingStatus() {
            View.SetActive(true);
            TimerText.gameObject.SetActive(false);
            LoadingText.gameObject.SetActive(true);
        }
        
        public void StartCountdown(float countdownTime) {
            _timerActive = true;
            _remainingCountdownTime = countdownTime;
            View.SetActive(true);
            TimerText.gameObject.SetActive(true);
            LoadingText.gameObject.SetActive(false);
            UpdateTimerText();
        }

        private void Update() {
            if (!_timerActive) return;

            _remainingCountdownTime -= Time.deltaTime;
            if (_remainingCountdownTime <= 0) {
                _timerActive = false;
                View.SetActive(false);
                return;
            }

            UpdateTimerText();
        }

        private void UpdateTimerText() {
            TimerText.text = string.Format(TimerMessage, Mathf.Ceil(_remainingCountdownTime));
        }
    }
}