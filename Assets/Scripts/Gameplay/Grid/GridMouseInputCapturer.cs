using Gameplay.UI;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Grid {
    /// <summary>
    /// Captures mouse inputs and reports them to the <see cref="GridController"/>
    /// </summary>
    public class GridMouseInputCapturer : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler {
        private const string ZoomAction = "Zoom";
        private const float ZoomSensitivity = 0.1f;
        
        [SerializeField] private GridInputController _gridInputController;

        private Player _playerInput;
        
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

        private void Start() {
            _playerInput = ReInput.players.GetPlayer(0);
        }
        
        private void Update() {
            if (!InputAllowed) return;

            float delta = _playerInput.GetAxis(ZoomAction);
            if (Mathf.Abs(delta) > ZoomSensitivity) {
                _gridInputController.ProcessZoom(delta);
            }
        }
    }
}