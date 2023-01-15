using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Grid {
    /// <summary>
    /// Captures clicks and reports them to the <see cref="GridController"/>
    /// </summary>
    public class GridClickCapturer : MonoBehaviour, IPointerClickHandler {
        public void OnPointerClick(PointerEventData eventData) {
            GameManager.Instance.GridController.ProcessClick(eventData);
        }
    }
}