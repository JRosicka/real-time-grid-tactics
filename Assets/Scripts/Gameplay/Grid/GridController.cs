using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Pathfinding;
using Gameplay.UI;
using Sirenix.OdinInspector;
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
        
        public GridData GridData { get; private set; }

        public void Initialize() {
            _pathVisualizer.Initialize();
            GridData = new GridData(_gameplayTilemap);
            _overlayTilemap = new OverlayTilemap(_overlayMap, GridData, _inaccessibleTile, _slowMovementTile);
            _selectedUnitTracker.Initialize(_selectedUnitReticle);
            _targetUnitTracker.Initialize(_targetUnitReticle);
        }
        
        public void TrackEntity(GridEntity entity) {
            _selectedUnitTracker.TrackEntity(entity);
            _overlayTilemap.UpdateCellOverlaysForEntity(entity);
        }

        public void TargetEntity(GridEntity entity) {
            _targetUnitTracker.TrackEntity(entity);
        }

        public void HoverOverCell(Vector2Int cell) {
            _mouseReticle.SelectTile(cell, GameManager.Instance.GetTopEntityAtLocation(cell));
            GameManager.Instance.EntitySelectionManager.TryFindPath(cell);
        }

        public void StopHovering() {
            _mouseReticle.Hide();
            _pathVisualizer.ClearPath();
        }

        public void VisualizePath(List<GridNode> path) {
            _pathVisualizer.Visualize(path);
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
