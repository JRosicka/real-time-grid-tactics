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
        [SerializeField] private CameraManager _cameraManager;

        private EntitySelectionManager _entitySelectionManager;
        private Vector2Int? _currentHoveredCell;

        public void Initialize(EntitySelectionManager entitySelectionManager) {
            _entitySelectionManager = entitySelectionManager;
            _entitySelectionManager.SelectedEntityMoved += UpdateSelectedEntityPath;
        }
        
        public void ProcessMouseMove(PointerEventData eventData) {
            Vector2Int mousePos = _gridController.GetCellPosition(eventData.pointerCurrentRaycast.worldPosition);
            if (mousePos == _currentHoveredCell) return;
            _currentHoveredCell = mousePos;
            
            _gridController.HoverOverCell(mousePos);
        }

        public void ProcessMouseExit() {
            _currentHoveredCell = null;
            _gridController.StopHovering();
            _gridController.ClearPath();
        }
        
        private void UpdateSelectedEntityPath() {
            if (_currentHoveredCell == null) return;
            _gridController.HoverOverCell((Vector2Int)_currentHoveredCell);
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
        
        public void ProcessClickDown(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Middle) {
                _cameraManager.StartMiddleMousePan(eventData.pointerPressRaycast.screenPosition);
            }
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
                    // If we were holding down the middle mouse button to pan the camera, then make sure we stop now
                    _cameraManager.StopMiddleMousePan();
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