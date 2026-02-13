using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Task = System.Threading.Tasks.Task;

namespace Gameplay.UI {
    /// <summary>
    /// A tooltip that appears at a given location to display text info. Not to be confused with <see cref="TooltipView"/>
    /// </summary>
    public class HoverableTooltip : MonoBehaviour {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private VerticalLayoutGroup _layoutGroup;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        [SerializeField] private ContentSizeFitter _tooltipContentSizeFitter;

        private RectTransform _rectTransform;
        private int _initializingCount = 0;
        protected bool Showing;
        
        private void Awake() {
            _rectTransform = GetComponent<RectTransform>();
        }

        public async void Initialize(string text) {
            _initializingCount++;
            
            _text.text = text;
            
            _contentSizeFitter.SetLayoutVertical();
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
            _layoutGroup.CalculateLayoutInputVertical();
            _tooltipContentSizeFitter.SetLayoutVertical();
            CheckForProperAdjustment();
            _initializingCount--;
        }

        public void ShowTooltip() {
            _canvasGroup.alpha = 1;
            Showing = true;
        }
        
        public void HideTooltip() {
            _canvasGroup.alpha = 0;
            Showing = false;
        }

        private void CheckForProperAdjustment() {
            if (_rectTransform.rect.height == 0) {
                Debug.LogWarning($"Hoverable tooltip content height was not adjusted! Initializing count: {_initializingCount}");
                MonitorAfterImproperAdjustment();
            }
        }

        private async void MonitorAfterImproperAdjustment() {
            for (int i = 0; i < 5; i++) {
                await Task.Delay(TimeSpan.FromSeconds(0.1f));
                if (_rectTransform.rect.height == 0) {
                    Debug.LogWarning($"Hoverable tooltip content height STILL not adjusted after {.1f * (i+1)}s! Initializing count: {_initializingCount}");
                }
            }
        }
    }
}