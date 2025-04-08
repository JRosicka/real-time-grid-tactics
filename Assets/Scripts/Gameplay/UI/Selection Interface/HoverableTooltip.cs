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
        
        public async void Initialize(string text) {
            _text.text = text;
            
            _contentSizeFitter.SetLayoutVertical();
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
            _layoutGroup.CalculateLayoutInputVertical();
            _tooltipContentSizeFitter.SetLayoutVertical();
        }

        public void ShowTooltip() {
            _canvasGroup.alpha = 1;
        }
        
        public void HideTooltip() {
            _canvasGroup.alpha = 0;
        }
    }
}