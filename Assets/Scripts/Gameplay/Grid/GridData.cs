using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gameplay.Grid {
    /// <summary>
    /// Keeps track of the data for each cell in a gameplay grid. Acts as the backing for the gameplay Tilemap, useful
    /// for getting locations of particular tiles and keeping track of the state of specific tiles.
    ///
    /// No need for any networking for this since it is handled independently for each client, though
    /// TODO some networking would be needed if we ever want cell states to be mutable. Well, probably better to just have that stuff be in GridEntityCollection and have them be GridEntities. Resources might be a little tricky though. 
    /// </summary>
    public class GridData {
        public class CellData {
            public GameplayTile Tile;
            public Vector2Int Location;
            public List<CellData> Neighbors = new List<CellData>();
        }

        private readonly Dictionary<Vector2Int, CellData> _cells = new Dictionary<Vector2Int, CellData>();
        private readonly Dictionary<GameplayTile, List<CellData>> _tilesByTypeCache = new Dictionary<GameplayTile, List<CellData>>(); 

        public GridData(Tilemap tilemap) {
            BoundsInt bounds = tilemap.cellBounds;
            
            // Create each cell data object
            for (int x = bounds.xMin; x < bounds.xMax; x++) {
                for (int y = bounds.yMin; y < bounds.yMax; y++) {
                    GameplayTile tile = tilemap.GetTile<GameplayTile>(new Vector3Int(x, y));
                    if (tile == null) continue;
                    
                    Vector2Int location = new Vector2Int(x, y);
                    _cells.Add(location, new CellData {
                        Tile = tile,
                        Location = location
                    });
                }
            }
            
            // Now that we have all of the cells created, determine adjacency for each
            foreach (CellData cell in _cells.Values) {
                // Get all of the valid locations
                List<Vector2Int> neighborLocations = CellDistanceLogic.Neighbors(cell.Location)
                                                        .Where(l => _cells.Keys.Contains(l))
                                                        .ToList();
                // Add each neighboring cell to the current cell
                foreach (Vector2Int location in neighborLocations) {
                    cell.Neighbors.Add(_cells[location]);
                }
            }
        }

        /// <summary>
        /// Gets all cells of the given tile type
        /// </summary>
        public List<CellData> GetCells(GameplayTile tileType) {
            if (_tilesByTypeCache.ContainsKey(tileType)) {
                return _tilesByTypeCache[tileType];
            }
            
            List<CellData> cells = _cells.Values.Where(c => c.Tile == tileType).ToList();
            _tilesByTypeCache.Add(tileType, cells);
            return cells;
        }

        /// <summary>
        /// Gets all of the cells adjacent to the passed-in cell
        /// </summary>
        public CellData GetCell(Vector2Int location) {
            return _cells.TryGetValue(location, out CellData cell) ? cell : null;
        }
        
        /// <summary>
        /// Gets all of the cells adjacent to the passed-in location
        /// </summary>
        public List<CellData> GetAdjacentCells(Vector2Int location) {
            return GetCell(location)?.Neighbors;
        }
    }
}