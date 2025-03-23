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
        private GameManager _gameManager;
        
        public Vector2Int? CurrentHoveredCell { get; private set; }

        public void Initialize(EntitySelectionManager entitySelectionManager, GameManager gameManager) {
            _entitySelectionManager = entitySelectionManager;
            _gameManager = gameManager;
            if (_gameManager.GameSetupManager.GameInitialized) {
                RegisterEvents();
            } else {
                gameManager.GameSetupManager.GameInitializedEvent += RegisterEvents;
            }
        }

        private void RegisterEvents() {
            _entitySelectionManager.SelectedEntityMoved += UpdateSelectedEntityPath;
            // TODO I don't like that we do this and re-path-find every time an entity spawns/despawns/moves. That's a lot of pathfinding. 
            _gameManager.CommandManager.EntityCollectionChangedEvent += ReProcessMousePosition;
        }
        
        public void ProcessMouseMove(PointerEventData eventData) {
            Vector2Int mousePos = _gridController.GetCellPosition(eventData.pointerCurrentRaycast.worldPosition);
            if (mousePos == CurrentHoveredCell) return;
            if (!_gridController.IsInBounds(mousePos)) return;
            CurrentHoveredCell = mousePos;
            
            _gridController.HoverOverCell(mousePos);
        }

        public void ReProcessMousePosition() {
            if (CurrentHoveredCell == null) return;
            if (!_gridController.IsInBounds(CurrentHoveredCell.Value)) return;
            _gridController.HoverOverCell(CurrentHoveredCell.Value);
        }

        public void ProcessMouseExit() {
            CurrentHoveredCell = null;
            _gridController.StopHovering();
        }
        
        private void UpdateSelectedEntityPath() {
            if (CurrentHoveredCell == null) return;
            _gridController.HoverOverCell((Vector2Int)CurrentHoveredCell);
        }
        
        /// <summary>
        /// Handle mouse input
        /// </summary>
        public void ProcessClick(PointerEventData eventData) {
            ProcessMouseMove(eventData);
            Vector2Int mousePos = _gridController.GetCellPosition(eventData.pointerPressRaycast.worldPosition);
            if (!_gridController.IsInBounds(mousePos)) return;

            MouseClick click = eventData.button switch {
                PointerEventData.InputButton.Left => MouseClick.Left,
                PointerEventData.InputButton.Right => MouseClick.Right,
                PointerEventData.InputButton.Middle => MouseClick.Middle,
                _ => throw new ArgumentOutOfRangeException()
            };

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
                    _entitySelectionManager.SelectCell(clickPosition);
                    break;
                case MouseClick.Right:
                    _entitySelectionManager.TryInteractWithCell(clickPosition);
                    break;
                case MouseClick.Middle:
                case MouseClick.None:
                    // We cover the hover action elsewhere, so nothing else to do
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(clickType), clickType, null);
            }
        }
    }
}