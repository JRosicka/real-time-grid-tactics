using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Scenes {
    /// <summary>
    /// Visuals and wait logic for the loading screen
    /// </summary>
    public class LoadingScreen : MonoBehaviour {
        [SerializeField] private TMP_Text _loadingText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Canvas _canvas;
        
        [SerializeField] private float _fadeSeconds = 1f;
        [SerializeField] private float _loadingTextFrequencySeconds = 2f;
        [SerializeField] private float _loadingTextMaxPeriodCount = 3;
        [SerializeField] private string _loadingTextFormat = "Loading{0}";

        private const int HiddenSortingOrder = -5;
        private const int BehindMenuSortingOrder = 5;
        private const int InFrontOfMenuSortingOrder = 15;

        private enum LoadingScreenState {
            FadeIn,
            Visible,
            FadeOut,
            Hidden
        }
        
        private float _currentFadeAmount;
        private float _currentLoadingTextAmount;
        private LoadingScreenState _state;
        private TaskCompletionSource<object> _showLoadingScreenTcs;
        
        public async Task ShowLoadingScreen(bool fadeIn, bool inFrontOfMenus) {
            SetState(fadeIn ? LoadingScreenState.FadeIn : LoadingScreenState.Visible);
            _canvas.sortingOrder = inFrontOfMenus ? InFrontOfMenuSortingOrder : BehindMenuSortingOrder;

            if (fadeIn) {
                _showLoadingScreenTcs = new TaskCompletionSource<object>();
                await _showLoadingScreenTcs.Task;
            }
        }

        public void HideLoadingScreen(bool fadeOut) {
            SetState(fadeOut ? LoadingScreenState.FadeOut : LoadingScreenState.Hidden);
        }
        
        private void Awake() {
            SetState(LoadingScreenState.Visible);
        }
        
        private void SetState(LoadingScreenState state) {
            _state = state;
            switch (state) {
                case LoadingScreenState.FadeIn:
                    _canvas.gameObject.SetActive(true);
                    break;
                case LoadingScreenState.Visible:
                    _canvasGroup.alpha = 1;
                    _currentFadeAmount = _fadeSeconds;
                    _canvas.gameObject.SetActive(true);
                    _showLoadingScreenTcs?.SetResult(null);
                    _showLoadingScreenTcs = null;
                    break;
                case LoadingScreenState.FadeOut:
                    _canvas.gameObject.SetActive(true);
                    break;
                case LoadingScreenState.Hidden:
                    _canvasGroup.alpha = 0;
                    _currentFadeAmount = 0;
                    _canvas.sortingOrder = HiddenSortingOrder;
                    _canvas.gameObject.SetActive(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), $"Invalid loading screen state {state}");
            }
        }

        private void Update() {
            switch (_state) {
                case LoadingScreenState.FadeIn:
                    _currentFadeAmount += Time.deltaTime;
                    _canvasGroup.alpha = _currentFadeAmount / _fadeSeconds;
                    if (_currentFadeAmount >= _fadeSeconds) {
                        SetState(LoadingScreenState.Visible);
                    }
                    UpdateLoadingText();
                    break;
                case LoadingScreenState.FadeOut:
                    _currentFadeAmount -= Time.deltaTime;
                    _canvasGroup.alpha = _currentFadeAmount / _fadeSeconds;
                    if (_currentFadeAmount <= 0) {
                        SetState(LoadingScreenState.Hidden);
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