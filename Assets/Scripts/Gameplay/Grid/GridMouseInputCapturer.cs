using Gameplay.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Grid {
    /// <summary>
    /// Captures mouse inputs and reports them to the <see cref="GridController"/>
    /// </summary>
    public class GridMouseInputCapturer : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler {
        [SerializeField] private GridInputController _gridInputController;

        private bool InputAllowed => GameManager.Instance?.GameSetupManager.InputAllowed ?? false;

        public void OnPointerClick(PointerEventData eventData) {
            if (!InputAllowed) return;
            _gridInputController.ProcessClick(eventData);
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (!InputAllowed) return;
            _gridInputController.ProcessClickDown(eventData);
        }

        public void OnPointerMove(PointerEventData eventData) {
            if (!InputAllowed) return;
            _gridInputController.ProcessMouseMove(eventData);
        }
        
        public void OnPointerEnter(PointerEventData eventData) {
            if (!InputAllowed) return;
            _gridInputController.ProcessMouseMove(eventData);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (!InputAllowed) return;
            _gridInputController.ProcessMouseExit();
        }
    }
}