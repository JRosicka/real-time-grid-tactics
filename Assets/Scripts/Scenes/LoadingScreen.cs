using System;
using TMPro;
using UnityEngine;

namespace Scenes {
    /// <summary>
    /// Visuals and wait logic for the loading screen
    /// </summary>
    public class LoadingScreen : MonoBehaviour {
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeSeconds = 1f;
        [SerializeField] private float _loadingTextFrequencySeconds = 2f;
        [SerializeField] private float _loadingTextMaxPeriodCount = 3;
        [SerializeField] private string _loadingTextFormat = "Loading{0}";

        private enum LoadingScreenState {
            FadeIn,
            Visible,
            FadeOut,
            Hidden
        }
        
        private float _currentFadeAmount;
        private float _currentLoadingTextAmount;
        private LoadingScreenState _state;
        
        public void ShowLoadingScreen(bool fadeIn) {
            if (fadeIn) {
                _state = LoadingScreenState.FadeIn;
            } else {
                _canvasGroup.alpha = 1;
                _state = LoadingScreenState.Visible;
            }
        }

        public void HideLoadingScreen(bool fadeOut) {
            if (fadeOut) {
                _state = LoadingScreenState.FadeOut;
            } else {
                _canvasGroup.alpha = 0;
                _state = LoadingScreenState.Hidden;
            }
        }

        private void Start() {
            _state = LoadingScreenState.Visible;
            _currentFadeAmount = 1;
        }

        private void Update() {
            switch (_state) {
                case LoadingScreenState.FadeIn:
                    _currentFadeAmount += Time.deltaTime;
                    _canvasGroup.alpha = _currentFadeAmount / _fadeSeconds;
                    if (_currentFadeAmount >= _fadeSeconds) {
                        _currentFadeAmount = _fadeSeconds;
                        _state = LoadingScreenState.Visible;
                    }
                    UpdateLoadingText();
                    break;
                case LoadingScreenState.FadeOut:
                    _currentFadeAmount -= Time.deltaTime;
                    _canvasGroup.alpha = _currentFadeAmount / _fadeSeconds;
                    if (_currentFadeAmount <= 0) {
                        _currentFadeAmount = 0;
                        _state = LoadingScreenState.Hidden;
                    }
                    UpdateLoadingText();
                    break;
                case LoadingScreenState.Visible:
                    UpdateLoadingText();
                    break;
                case LoadingScreenState.Hidden:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_state), $"Invalid loading screen state {_state}");
            }
        }

        private void UpdateLoadingText() {
            _currentLoadingTextAmount += Time.deltaTime; 
            _currentLoadingTextAmount %= _loadingTextFrequencySeconds;
            float loadingTextAmount01 = _currentLoadingTextAmount / _loadingTextFrequencySeconds;

            int amountOfPeriods = Mathf.FloorToInt(loadingTextAmount01 * (_loadingTextMaxPeriodCount + 1));
            string periods = "";
            for (int i = 0; i < amountOfPeriods; i++) {
                periods += ".";
            }
            _loadingText.text = string.Format(_loadingTextFormat, periods);
        }
    }
}