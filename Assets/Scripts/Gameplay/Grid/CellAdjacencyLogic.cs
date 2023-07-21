using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// Logic for determining cell adjacency. Since we work with hex cells, this can be a little unintuitive.
    /// </summary>
    public static class CellAdjacencyLogic {
        private static readonly Vector2Int
            Left = new Vector2Int(-1, 0),
            Right = new Vector2Int(1, 0),
            Down = new Vector2Int(0, -1),
            DownLeft = new Vector2Int(-1, -1),
            DownRight = new Vector2Int(1, -1),
            Up = new Vector2Int(0, 1),
            UpLeft = new Vector2Int(-1, 1),
            UpRight = new Vector2Int(1, 1);

        private static readonly Vector2Int[] DirectionsWhenYIsEven = { Left, Right, Down, DownLeft, Up, UpLeft };
        private static readonly Vector2Int[] DirectionsWhenYIsOdd = { Left, Right, Down, DownRight, Up, UpRight };

        /// <summary>
        /// Gets the set of cells that neighbor the given one. These cells may or may not actually exist - we always
        /// return the 6 cells that would be adjacent to the given one.
        /// </summary>
        public static IEnumerable<Vector2Int> Neighbors(Vector2Int cell) {
            Vector2Int[] directions = cell.y % 2 == 0 ? DirectionsWhenYIsEven : DirectionsWhenYIsOdd;
            return directions.Select(direction => cell + direction);
        }
    }
}