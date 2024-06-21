using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.UI;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay.Grid {
    /// <summary>
    /// Keeps track of grid and tilemap visuals and data. For input-specific functionality, see <see cref="GridInputController"/>.
    /// </summary>
    public class GridController : MonoBehaviour {
        public const float CellWidth = 0.8659766f;
    
        [SerializeField] private UnityEngine.Grid _grid;
        [SerializeField] private Tilemap _gameplayTilemap;
        [SerializeField] private PathVisualizer _pathVisualizer;

        // The overlay tilemap for highlighting particular tiles
        private OverlayTilemap _overlayTilemap;
        [SerializeField] private Tilemap _overlayMap;
        [SerializeField] private Tile _inaccessibleTile;
        [SerializeField] private Tile _slowMovementTile;
        
        // Reticle for where the mouse is currently hovering
        [SerializeField] private SelectionReticle _mouseReticle;
        
        // Reticle for the selected unit
        [SerializeField] private SelectionReticle _selectedUnitReticle;
        private SelectionReticleEntityTracker _selectedUnitTracker = new SelectionReticleEntityTracker();
        
        // Reticle for the target unit (The place where the selected unit is moving towards or attacking)
        [SerializeField] private SelectionReticle _targetUnitReticle;    // TODO not currently doing anything with this. Call associated method in this class when we move or attack.
        private SelectionReticleEntityTracker _targetUnitTracker = new SelectionReticleEntityTracker();

        private List<Vector2Int> _selectableCells;

        private MapLoader _mapLoader;

        public GridData GridData { get; private set; }

        public void Initialize() {
            _pathVisualizer.Initialize();
            _mapLoader = GameManager.Instance.GameSetupManager.MapLoader;
            GridData = new GridData(_gameplayTilemap, this);
            _overlayTilemap = new OverlayTilemap(_overlayMap, this, _inaccessibleTile, _slowMovementTile);
            _selectedUnitTracker.Initialize(_selectedUnitReticle);
            _targetUnitTracker.Initialize(_targetUnitReticle);
            _selectableCells = null;    // Not sure why this is necessary, but it seems to be
        }
        
        public void TrackEntity(GridEntity entity) {
            _selectedUnitTracker.TrackEntity(entity);
            _overlayTilemap.UpdateCellOverlaysForEntity(entity);
        }

        private void UpdateCellOverlaysForAbility(List<Vector2Int> cells, GridEntity entity) {
            _overlayTilemap.UpdateCellOverlaysForAbility(cells, entity);
        }

        public void TargetEntity(GridEntity entity) {
            _targetUnitTracker.TrackEntity(entity);
        }

        /// <summary>
        /// Some abilities only allow for certain cells to be selected. Keep track of those and update tile overlays and
        /// the selection reticle to comply. 
        /// </summary>
        public void UpdateSelectableCells(List<Vector2Int> selectableCells, GridEntity selectedEntity) {
            _selectableCells = selectableCells;
            
            // Reset the hover-over-cell functionality to update
            StopHovering();
            ClearPath();
            GameManager.Instance.GridInputController.ReProcessMousePosition();
            
            UpdateCellOverlaysForAbility(selectableCells, selectedEntity);
        }

        public void HoverOverCell(Vector2Int cell) {
            if (_selectableCells != null && !_selectableCells.Contains(cell)) {
                return;
            }
            
            _mouseReticle.SelectTile(cell, GameManager.Instance.GetTopEntityAtLocation(cell));
            GameManager.Instance.EntitySelectionManager.TryFindPath(cell);
        }

        public void StopHovering() {
            _mouseReticle.Hide();
        }

        public void ClearPath() {
            _pathVisualizer.ClearPath();
        }

        public void VisualizePath(PathfinderService.Path path) {
            _pathVisualizer.Visualize(path);
        }

        private List<Vector2Int> _allCellsInBounds;
        public List<Vector2Int> GetAllCellsInBounds() {
            if (!_allCellsInBounds.IsNullOrEmpty()) return _allCellsInBounds;
            
            _allCellsInBounds = new List<Vector2Int>();
            
            // Even-numbered y-values default to having "thin" left sides
            bool needsExtraLeftColumnCells = Mathf.Abs(_mapLoader.LowerLeftCell.y) % 2 == 0 && _mapLoader.WideLeftSide;
            if (needsExtraLeftColumnCells) {
                // Add every other cell in the column to the left of the leftmost configured column
                for (int y = _mapLoader.LowerLeftCell.y + 1; y <= _mapLoader.UpperRightCell.y; y += 2) {
                    _allCellsInBounds.Add(new Vector2Int(_mapLoader.LowerLeftCell.x - 1, y));
                }
            }
            
            // Leave out the leftmost and rightmost cells if they should be "thin"
            bool needsLessLeftColumnCells = Mathf.Abs(_mapLoader.LowerLeftCell.y) % 2 == 1 && !_mapLoader.WideLeftSide;
            if (needsLessLeftColumnCells) {
                for (int y = _mapLoader.LowerLeftCell.y; y <= _mapLoader.UpperRightCell.y; y += 2) {
                    _allCellsInBounds.Add(new Vector2Int(_mapLoader.LowerLeftCell.x, y));
                }
            }
            bool needsLessRightColumnCells = Mathf.Abs(_mapLoader.UpperRightCell.y) % 2 == 0 && !_mapLoader.WideRightSide;

            // Fill in the middle. 
            for (int x = needsLessLeftColumnCells ? _mapLoader.LowerLeftCell.x + 1 : _mapLoader.LowerLeftCell.x; 
                     x <= (needsLessRightColumnCells ? _mapLoader.UpperRightCell.x - 1 : _mapLoader.UpperRightCell.x); 
                     x++) {
                for (int y = _mapLoader.LowerLeftCell.y; y <= _mapLoader.UpperRightCell.y; y++) {
                    _allCellsInBounds.Add(new Vector2Int(x, y));
                }
            }
            
            // Odd-numbered y-values default to having "thin" left sides
            bool needsExtraRightColumnCells = Mathf.Abs(_mapLoader.UpperRightCell.y) % 2 == 1 && _mapLoader.WideRightSide;
            if (needsExtraRightColumnCells) {
                // Add every other cell in the column to the right of the rightmost configured column
                for (int y = _mapLoader.UpperRightCell.y - 1; y >= _mapLoader.LowerLeftCell.y; y -= 2) {
                    _allCellsInBounds.Add(new Vector2Int(_mapLoader.UpperRightCell.x + 1, y));
                }
            }
            
            if (needsLessRightColumnCells) {
                for (int y = _mapLoader.UpperRightCell.y; y >= _mapLoader.LowerLeftCell.y; y -= 2) {
                    _allCellsInBounds.Add(new Vector2Int(_mapLoader.UpperRightCell.x, y));
                }
            }

            return _allCellsInBounds;
        }
        
        public bool IsInBounds(Vector2Int cell) {
            return GetAllCellsInBounds().Contains(cell);
        }

        #region Vector Conversion
        
        public Vector2 GetWorldPosition(Vector2Int cellPosition) {
            return _grid.CellToWorld((Vector3Int) cellPosition);
        }
    
        public Vector2Int GetCellPosition(Vector2 worldPosition) {
            Vector2Int cellPosition = (Vector2Int) _grid.WorldToCell(worldPosition);
            return cellPosition;
        }
        
        #endregion
    }
}
