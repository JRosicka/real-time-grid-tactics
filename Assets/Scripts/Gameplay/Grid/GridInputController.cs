using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.Grid {
    /// <summary>
    /// Processes mouse input for interacting with elements on the grid
    /// </summary>
    public class GridInputController : MonoBehaviour {
        private enum MouseClick { 
            None = -1,
            Left = 0,
            Middle = 1,
            Right = 2
        }

        [SerializeField] private GridController _gridController;

        private EntitySelectionManager _entitySelectionManager;
        private Vector2Int _previousMousePos;

        public void Initialize(EntitySelectionManager entitySelectionManager) {
            _entitySelectionManager = entitySelectionManager;
        }
        
        public void ProcessMouseMove(PointerEventData eventData) {
            Vector2Int mousePos = _gridController.GetCellPosition(eventData.pointerCurrentRaycast.worldPosition);
            if (mousePos == _previousMousePos) return;
            _previousMousePos = mousePos;
            
            _gridController.HoverOverCell(mousePos);
        }

        public void ProcessMouseExit() {
            _gridController.StopHovering();
        }
        
        /// <summary>
        /// Handle mouse input
        /// </summary>
        public void ProcessClick(PointerEventData eventData) {
            ProcessMouseMove(eventData);
            Vector2Int mousePos = _gridController.GetCellPosition(eventData.pointerPressRaycast.worldPosition);
        
            MouseClick click;
            switch (eventData.button) {
                case PointerEventData.InputButton.Left:
                    // Left mouse button click
                    Debug.Log("Click on grid at " + mousePos);
                    click = MouseClick.Left;
                    break;
                case PointerEventData.InputButton.Right:
                    Debug.Log("Right click at " + mousePos);
                    click = MouseClick.Right;
                    break;
                case PointerEventData.InputButton.Middle:
                    Debug.Log("Middle mouse click at " + mousePos);
                    click = MouseClick.Middle;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            TryClickOnCell(click, mousePos);
        }

        private void TryClickOnCell(MouseClick clickType, Vector2Int clickPosition) {
            switch (clickType) {
                case MouseClick.Left:
                    // See if we have a targetable ability we want to use. If so, use it.
                    if (_entitySelectionManager.TryUseTargetableAbility(clickPosition)) {
                        return;
                    }
                    // Otherwise select whatever is at the clicked cell
                    _entitySelectionManager.SelectEntityAtCell(clickPosition);
                    break;
                case MouseClick.Middle:
                    // Don't do anything with this
                    break;
                case MouseClick.Right:
                    _entitySelectionManager.TryInteractWithCell(clickPosition);
                    break;
                case MouseClick.None:
                    // We cover the hover action elsewhere, so nothing else to do
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(clickType), clickType, null);
            }
        }
    }
}