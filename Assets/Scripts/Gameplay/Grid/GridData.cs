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

        public GridData(Tilemap tilemap, MapLoader mapLoader) {
            BoundsInt bounds = tilemap.cellBounds;
            
            int xMin = Mathf.Max(bounds.xMin, mapLoader.LowerLeftCell.x);
            int xMax = Mathf.Min(bounds.xMax, mapLoader.UpperRightCell.x);
            int yMin = Mathf.Max(bounds.yMin, mapLoader.LowerLeftCell.y);
            int yMax = Mathf.Min(bounds.yMax, mapLoader.UpperRightCell.y);
            
            // Create each cell data object
            bool xMinIsEven = Mathf.Abs(xMin) % 2 == 0;
            for (int x = xMin; x <= xMax; x++) {
                for (int y = yMin; y <= yMax; y++) {
                    // HACK for when xMin is even - skip all xMin values with even y values, since that doesn't really work well with our setup
                    if (xMinIsEven && x == xMin && y % 2 == 0) continue;
                    
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

        /// <summary>
        /// Returns a list of all cells within range (up to x cells away) from the specified range. The list includes
        /// the provided location.
        /// </summary>
        public List<CellData> GetCellsInRange(Vector2Int location, int range) {
            CellData currentCell = GetCell(location);
            List<CellData> ret = new List<CellData>();
            List<CellData> searching = new List<CellData> { currentCell };
            List<CellData> toAdd = new List<CellData>();
            for (int i = 0; i < range; i++) {
                foreach (CellData cell in searching) {
                    toAdd.AddRange(cell.Neighbors);
                }

                toAdd = toAdd.Distinct().ToList();
                toAdd.RemoveAll(c => ret.Contains(c));
                ret.AddRange(toAdd);
                searching = new List<CellData>();
                searching.AddRange(toAdd);
                toAdd.Clear();
            }

            return ret;
        }
    }
}