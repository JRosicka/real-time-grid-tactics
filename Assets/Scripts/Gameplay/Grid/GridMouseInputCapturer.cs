using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Grid {
    /// <summary>
    /// Captures mouse inputs and reports them to the <see cref="GridController"/>
    /// </summary>
    public class GridMouseInputCapturer : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler {
        public void OnPointerClick(PointerEventData eventData) {
            GameManager.Instance.GridController.ProcessClick(eventData);
        }


        public void OnPointerMove(PointerEventData eventData) {
            GameManager.Instance.GridController.ProcessMouseMove(eventData);
        }
        
        public void OnPointerEnter(PointerEventData eventData) {
            GameManager.Instance.GridController.ProcessMouseMove(eventData);
        }

        public void OnPointerExit(PointerEventData eventData) {
            GameManager.Instance.GridController.ProcessMouseExit(eventData);
        }
    }
}