using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay.Grid {
    /// <summary>
    /// Handles tiles overlayed on top of the "main" tilemap. such as to indicate movement restrictions
    /// 
    /// NOTE: If I ever dynamically set rules tiles to update their tiling rules, then I will probably need to call
    /// Tilemap.RefreshAllTiles() when done. See https://stackoverflow.com/questions/60824529/unity-2d-tilemap-custom-hexagonal-rule-tile
    /// </summary>
    public class OverlayTilemap {
        private readonly Tilemap _overlayMap;
        private readonly Tile _highlightTile;
        private readonly GridController _gridController;
        
        private GridEntity _entity;

        private GridData GridData => _gridController.GridData;

        public OverlayTilemap(Tilemap overlayMap, GridController gridController, Tile highlightTile) {
            _overlayMap = overlayMap;
            _gridController = gridController;
            _highlightTile = highlightTile;
        }

        public void UpdateCellOverlaysForAbility(List<Vector2Int> cellsToHighlight, GridEntity entity) {
            if (_entity != entity) {
                _entity.UnregisteredEvent -= OnEntityUnregistered;
                _entity = entity;
                // Register new entity events
                if (_entity) {
                    _entity.UnregisteredEvent += OnEntityUnregistered;
                }
            }
            
            ClearOverlays();

            if (cellsToHighlight != null) {
                // Highlight the specified cells
                SetTiles(cellsToHighlight, _highlightTile);
            }
        }

        /// <summary>
        /// Apply an overlay to all tiles of the given types
        /// </summary>
        private void SetTiles(IEnumerable<GameplayTileType> tilesToApplyOverlayTo, TileBase overlayTile) {
            // Find all of the locations of the given tiles
            List<Vector2Int> locationsToModify = new List<Vector2Int>();
            foreach (GameplayTileType tileType in tilesToApplyOverlayTo) {
                locationsToModify.AddRange(GridData.GetCells(tileType).Select(c => c.Location));
            }
            SetTiles(locationsToModify, overlayTile);
        }

        private void SetTiles(IReadOnlyCollection<Vector2Int> cellsToApplyOverlayTo, TileBase overlayTile) {
            // Make a collection of the tile to be applied, because Unity demands it be done this way
            List<TileBase> tilesToApply = new List<TileBase>();
            for (int i = 0; i < cellsToApplyOverlayTo.Count; i++) {
                tilesToApply.Add(overlayTile);
            }
            
            // Apply the overlay tiles
            _overlayMap.SetTiles(cellsToApplyOverlayTo.Select(c => (Vector3Int)c).ToArray(), tilesToApply.ToArray());
        }
        
        private void SetAllTiles(TileBase overlayTile) {
            SetTiles(_gridController.GetAllCellsInBounds(), overlayTile);
        }

        private void ClearOverlays() {
            _overlayMap.ClearAllTiles();
        }
        
        private void OnEntityUnregistered() {
            ClearOverlays();
        }
    }
}