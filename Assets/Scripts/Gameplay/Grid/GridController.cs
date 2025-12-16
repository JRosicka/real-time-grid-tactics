using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.UI;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Gameplay.Grid {
    /// <summary>
    /// Keeps track of grid and tilemap visuals and data. For input-specific functionality, see <see cref="GridInputController"/>.
    /// </summary>
    public class GridController : MonoBehaviour {
        public const float CellWidth = 0.8659766f;
        public const float CellWidthWithBuffer = 0.88f;
    
        [SerializeField] private UnityEngine.Grid _grid;
        [SerializeField] private Tilemap _gameplayTilemap;
        [SerializeField] private PathVisualizer _pathVisualizer;

        // The overlay tilemap for highlighting particular tiles
        private OverlayTilemap _overlayTilemap;
        [SerializeField] private Tilemap _overlayMap;
        [SerializeField] private Tile _inaccessibleTile;
        
        // Tilemap for boundaries and cell outlines
        [SerializeField] private Tilemap _outlineTilemap;
        [SerializeField] private Tile _outOfBoundsTile;
        [SerializeField] private Tile _outlineTile;
        
        // Reticle for where the mouse is currently hovering
        [SerializeField] private SelectionReticle _mouseReticle;
        [SerializeField] private Image _invalidTargetedAbilityLocationIcon;
        
        // Reticle for the selected unit
        [SerializeField] private SelectionReticle _selectedUnitReticle;
        private SelectionReticleEntityTracker _selectedUnitTracker = new SelectionReticleEntityTracker();
        
        // Reticle for the target unit (The place where the selected unit is moving towards or attacking)
        [SerializeField] private SelectionReticle _targetUnitReticle;
        private SelectionReticleEntityTracker _targetUnitTracker = new SelectionReticleEntityTracker();

        private EntitySelectionManager _entitySelectionManager;

        private List<Vector2Int> _selectableCells;
        /// <summary>
        /// GameObject to hover over the selected cell to indicate the selected targetable ability
        /// </summary>
        private GameObject _targetedIcon;
        private MapLoader _mapLoader;

        public GridData GridData { get; private set; }

        public void Initialize(EntitySelectionManager entitySelectionManager) {
            _entitySelectionManager = entitySelectionManager;
            _pathVisualizer.Initialize();
            _mapLoader = GameManager.Instance.GameSetupManager.MapLoader;
            GridData = new GridData(_gameplayTilemap, this);
            _overlayTilemap = new OverlayTilemap(_overlayMap, this, _inaccessibleTile);
            _selectedUnitTracker.Initialize(_selectedUnitReticle, false);
            _targetUnitTracker.Initialize(_targetUnitReticle, true);
            _selectableCells = null;    // Not sure why this is necessary, but it seems to be
            _invalidTargetedAbilityLocationIcon.gameObject.SetActive(false);
        }

        /// <summary>
        /// Assigns gameplay tiles and outline/out-of-bounds tiles to the grid
        /// </summary>
        public void LoadMap(MapData mapData) {
            _gameplayTilemap.ClearAllTiles();
            _allCellsInBounds?.Clear();
            
            // Assign each gameplay tile
            List<Vector3Int> locations = new();
            List<TileBase> tiles = new();
            foreach (MapData.Cell cell in mapData.cells) {
                locations.Add((Vector3Int)cell.location);
                tiles.Add(GameConfigurationLocator.GameConfiguration.GetTile(cell.cellType));
            }
            
            _gameplayTilemap.SetTiles(locations.ToArray(), tiles.ToArray());
            
            // Assign the out-of-bounds tile to all locations
            List<TileBase> outOfBoundsTiles = new();
            for (int i = 0; i < locations.Count; i++) {
                outOfBoundsTiles.Add(_outOfBoundsTile);
            }
            _outlineTilemap.SetTiles(locations.ToArray(), outOfBoundsTiles.ToArray());

            // Now fill in the in-bounds tiles
            List<TileBase> outlineTiles = new();
            Vector3Int[] inBoundsLocations = GetAllCellsInBounds().Select(c => (Vector3Int)c).ToArray();
            for (int i = 0; i < inBoundsLocations.Length; i++) {
                outlineTiles.Add(_outlineTile);
            }
            _outlineTilemap.SetTiles(locations.ToArray(), outlineTiles.ToArray());
        }

        public List<MapData.Cell> GetAllCells() {
            // _gameplayTilemap.GetTile<GameplayTile>() TODO
            return null;
        }
        
        public void TrackEntity(GridEntity entity) {
            _selectedUnitTracker.TrackEntity(entity);
            _overlayTilemap.UpdateCellOverlaysForEntity(entity);
            if (entity != null) {
                entity.AttackTargetUpdated += UpdateTargetEntity;
                if (entity.InteractBehavior is { AllowedToSeeTargetLocation: true }) {
                    UpdateTargetEntity(entity, entity.GetAttackTarget());
                }
            }
        }

        public void UnTrackEntity(GridEntity entity) {
            if (entity != null) {
                entity.AttackTargetUpdated -= UpdateTargetEntity;
            }
            UpdateTargetEntity(null, null);
        }

        private void UpdateCellOverlaysForAbility(List<Vector2Int> cells, GridEntity entity) {
            _overlayTilemap.UpdateCellOverlaysForAbility(cells, entity);
        }

        private void UpdateTargetEntity(GridEntity trackedEntity, GridEntity targetedEntity) {
            _targetUnitTracker.TrackEntity(targetedEntity);
        }

        /// <summary>
        /// Some abilities only allow for certain cells to be selected. Keep track of those and update tile overlays and
        /// the selection reticle to comply. 
        /// </summary>
        public void UpdateSelectableCells(List<Vector2Int> selectableCells, GridEntity selectedEntity) {
            _selectableCells = selectableCells;
            
            // Reset the hover-over-cell functionality to update
            StopHovering();
            GameManager.Instance.GridInputController.ReProcessMousePosition();
            
            UpdateCellOverlaysForAbility(selectableCells, selectedEntity);
        }

        public void HoverOverCell(Vector2Int cell) {
            _mouseReticle.SelectTile(cell, GameManager.Instance.GetTopEntityAtLocation(cell));
            
            // Show/hide the invalid icon depending on if we can use the ability here
            if (_selectableCells != null && !_selectableCells.Contains(cell)) {
                _invalidTargetedAbilityLocationIcon.gameObject.SetActive(true);
            } else {
                _invalidTargetedAbilityLocationIcon.gameObject.SetActive(false);
            }

            _entitySelectionManager.HoverOverCell(cell);
        }

        public void StopHovering() {
            _mouseReticle.Hide();
            _entitySelectionManager.StopHovering();
        }

        public void SetTargetedIcon(GameObject targetedIcon) {
            _targetedIcon = targetedIcon;
            if (targetedIcon) {
                targetedIcon.transform.SetParent(_mouseReticle.transform, false);
                // Make sure that the invalid icon appears above the targeted icon
                _invalidTargetedAbilityLocationIcon.transform.SetAsLastSibling();
            }
        }

        public void ClearTargetedIcon() {
            if (_targetedIcon) {
                Destroy(_targetedIcon);
            }
            _targetedIcon = null;
        }

        public void ClearPath(bool onlyClearThickLines) {
            _pathVisualizer.ClearPath(true);
            if (!onlyClearThickLines) {
                _pathVisualizer.ClearPath(false);
            }
        }

        public void VisualizePath(PathfinderService.Path path, PathVisualizer.PathType pathType, Vector2Int targetLocation, bool hidePathDestination, bool thickLines) {
            _pathVisualizer.Visualize(path, pathType, targetLocation, hidePathDestination, thickLines);
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
