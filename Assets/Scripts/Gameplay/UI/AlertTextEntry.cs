using System;
using TMPro;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// A single text alert displayed via <see cref="AlertTextDisplayer"/>
    /// </summary>
    public class AlertTextEntry : MonoBehaviour {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;

        private float _secondsToWaitBeforeFade;
        private float _fadeTimeSeconds;
        private Action<AlertTextEntry> _onDestroy;
        private bool _initialized;

        private float _currentLifeTimeSeconds;
        private float _canvasAlpha;

        public void Initialize(string text, float secondsToWaitBeforeFade, float fadeTimeSeconds, Action<AlertTextEntry> onDestroy) {
            _text.text = text;
            _secondsToWaitBeforeFade = secondsToWaitBeforeFade;
            _fadeTimeSeconds = fadeTimeSeconds;
            _onDestroy = onDestroy;
            _initialized = true;
        }

        public void DestroyEntry() {
            _onDestroy?.Invoke(this);
            Destroy(gameObject);
        }

        private void Update() {
            if (!_initialized) return;
            
            _currentLifeTimeSeconds += Time.deltaTime;
            if (_currentLifeTimeSeconds <= _secondsToWaitBeforeFade) return;
            
            // Fade progress
            float currentFadeLength = _currentLifeTimeSeconds - _secondsToWaitBeforeFade;
            if (currentFadeLength > _fadeTimeSeconds) {
                // Done fading - destroy
                DestroyEntry();
                return;
            }
            _canvasGroup.alpha = Mathf.Lerp(1, 0, currentFadeLength / _fadeTimeSeconds);
        }
    }
}