using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Grid {
    /// <summary>
    /// Captures mouse inputs and reports them to the <see cref="GridController"/>
    /// </summary>
    public class GridMouseInputCapturer : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler {
        [SerializeField] private GridInputController _gridInputController;
        public void OnPointerClick(PointerEventData eventData) {
            _gridInputController.ProcessClick(eventData);
        }


        public void OnPointerMove(PointerEventData eventData) {
            _gridInputController.ProcessMouseMove(eventData);
        }
        
        public void OnPointerEnter(PointerEventData eventData) {
            _gridInputController.ProcessMouseMove(eventData);
        }

        public void OnPointerExit(PointerEventData eventData) {
            _gridInputController.ProcessMouseExit();
        }
    }
}