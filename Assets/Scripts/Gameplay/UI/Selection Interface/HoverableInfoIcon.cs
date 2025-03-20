using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// A view on the <see cref="SelectionInterface"/> that displays a tooltip when moused over
    /// </summary>
    public class HoverableInfoIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        [SerializeField] private HoverableTooltip _tooltip;

        public void ShowIcon(string tooltipText) {
            _tooltip.Initialize(tooltipText);
            gameObject.SetActive(true);
        }

        public void HideIcon() {
            gameObject.SetActive(false);
        }
        
        public void OnPointerEnter(PointerEventData eventData) {
            _tooltip.ShowTooltip();
        }
        
        public void OnPointerExit(PointerEventData eventData) {
            _tooltip.HideTooltip();
        }
    }
}