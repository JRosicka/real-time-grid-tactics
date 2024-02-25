using Gameplay.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Grid {
    /// <summary>
    /// Captures mouse inputs and reports them to the <see cref="GridController"/>
    /// </summary>
    public class GridMouseInputCapturer : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler {
        [SerializeField] private GridInputController _gridInputController;
        [SerializeField] private InGamePauseMenu _pauseMenu;

        private bool InputAllowed => !_pauseMenu.Paused 
                                     && GameManager.Instance.GameSetupManager.GameInitialized;
                                    // TODO check to see if the game is over

        public void OnPointerClick(PointerEventData eventData) {
            if (!InputAllowed) return;
            _gridInputController.ProcessClick(eventData);
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