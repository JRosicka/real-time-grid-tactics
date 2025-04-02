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
        
        public void Initialize(string text) {
            _text.text = text;
            
            _contentSizeFitter.SetLayoutVertical();
            _layoutGroup.CalculateLayoutInputVertical();
        }

        public void ShowTooltip() {
            _canvasGroup.alpha = 1;
        }
        
        public void HideTooltip() {
            _canvasGroup.alpha = 0;
        }
    }
}