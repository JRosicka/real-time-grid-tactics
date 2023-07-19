using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay.Grid {
    /// <summary>
    /// Handles tiles overlayed on top of the "main" tilemap. such as to indicate movement restrictions
    /// </summary>
    public class OverlayTilemap {
        private readonly Tilemap _overlayMap;
        private readonly Tile _inaccessibleTile;
        private readonly Tile _slowMovementTile;
        private readonly GridData _gridData;

        private GridEntity _entity;

        public OverlayTilemap(Tilemap overlayMap, GridData gridData, Tile inaccessibleTile, Tile slowMovementTile) {
            _overlayMap = overlayMap;
            _gridData = gridData;
            _inaccessibleTile = inaccessibleTile;
            _slowMovementTile = slowMovementTile;
        }

        public void UpdateCellOverlaysForEntity(GridEntity entity) {
            if (_entity) {
                _entity.UnregisteredEvent -= OnEntityUnregistered;
            }

            _entity = entity;
            // Register new entity events
            if (_entity) {
                _entity.UnregisteredEvent += OnEntityUnregistered;
            }
            ApplyTileOverlays();
        }

        private void ApplyTileOverlays() {
            ClearOverlays();

            if (_entity == null) return;

            // Apply any tile-specific overlays to tiles based on the selected entity's movement restrictions
            SetTiles(_entity.InaccessibleTiles, _inaccessibleTile);
            SetTiles(_entity.SlowTiles, _slowMovementTile);
        }

        /// <summary>
        /// Apply an overlay to all tiles of the given types
        /// </summary>
        private void SetTiles(List<GameplayTile> tilesToApplyOverlayTo, Tile overlayTile) {
            // Find all of the locations of the given tiles
            List<Vector3Int> locationsToModify = new List<Vector3Int>();
            foreach (GameplayTile tile in tilesToApplyOverlayTo) {
                locationsToModify.AddRange(_gridData.GetCells(tile).Select(c => c.Location));
            }
            // Make a collection of the tile to be applied, because Unity demands it be done this way
            List<TileBase> tilesToApply = new List<TileBase>();
            for (int i = 0; i < locationsToModify.Count; i++) {
                tilesToApply.Add(overlayTile);
            }
            
            // Apply the overlay tiles
            _overlayMap.SetTiles(locationsToModify.ToArray(), tilesToApply.ToArray());
        }

        private void ClearOverlays() {
            _overlayMap.ClearAllTiles();
        }
        
        private void OnEntityUnregistered() {
            ClearOverlays();
        }
    }
}