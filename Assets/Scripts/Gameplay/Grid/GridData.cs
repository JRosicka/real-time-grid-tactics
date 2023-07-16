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
    /// TODO some networking would be needed if we ever want cell states to be mutable. 
    /// </summary>
    public class GridData {
        public class CellData {
            public GameplayTile Tile;
            public Vector3Int Location;
        }

        private List<CellData> _cells = new List<CellData>();
        private Dictionary<GameplayTile, List<CellData>> _tilesByTypeCache = new Dictionary<GameplayTile, List<CellData>>(); 

        public GridData(Tilemap tilemap) {
            BoundsInt bounds = tilemap.cellBounds;
            for (int i = bounds.xMin; i < bounds.xMax; i++) {
                for (int j = bounds.yMin; j < bounds.yMax; j++) {
                    GameplayTile tile = tilemap.GetTile<GameplayTile>(new Vector3Int(i, j));
                    if (tile != null) {
                        _cells.Add(new CellData {
                            Tile = tile,
                            Location = new Vector3Int(i, j)
                        });
                    }
                }
            }
        }

        public List<CellData> GetCells(GameplayTile tileType) {
            if (_tilesByTypeCache.ContainsKey(tileType)) {
                return _tilesByTypeCache[tileType];
            }
            
            List<CellData> cells = _cells.Where(c => c.Tile == tileType).ToList();
            _tilesByTypeCache.Add(tileType, cells);
            return cells;
        }
    }
}