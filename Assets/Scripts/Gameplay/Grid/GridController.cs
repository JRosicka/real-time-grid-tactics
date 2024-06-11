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
            GridData = new GridData(_gameplayTilemap, _mapLoader);
            _overlayTilemap = new OverlayTilemap(_overlayMap, this, _inaccessibleTile, _slowMovementTile);
            _selectedUnitTracker.Initialize(_selectedUnitReticle);
            _targetUnitTracker.Initialize(_targetUnitReticle);
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
            
            // HACK - skip all xMin values with even y values, since that doesn't really work well with our setup
            for (int y = _mapLoader.LowerLeftCell.y; y <= _mapLoader.UpperRightCell.y; y++) {
                if (Mathf.Abs(y % 2) == 1) {
                    _allCellsInBounds.Add(new Vector2Int(_mapLoader.LowerLeftCell.x, y));
                }
            }
            
            // Fill in the rest
            for (int x = _mapLoader.LowerLeftCell.x + 1; x <= _mapLoader.UpperRightCell.x; x++) {
                for (int y = _mapLoader.LowerLeftCell.y; y <= _mapLoader.UpperRightCell.y; y++) {
                    _allCellsInBounds.Add(new Vector2Int(x, y));
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
