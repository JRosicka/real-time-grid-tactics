using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// Logic for determining cell distances and adjacency. Since we work with hex cells, this can be a little unintuitive.
    /// </summary>
    public static class CellDistanceLogic {
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

        /// <summary>
        /// How far apart two cells are, in number cells it takes to draw a straight-ish path to them. Does not consider
        /// any movement restrictions or pathfinding stuff.
        /// </summary>
        public static int DistanceBetweenCells(Vector2Int cell1, Vector2Int cell2) {
            int x1 = cell1.x - Mathf.FloorToInt(cell1.y / 2f);
            int y1 = cell1.y;
            int x2 = cell2.x - Mathf.FloorToInt(cell2.y / 2f);
            int y2 = cell2.y;
            int dx = x2 - x1;
            int dy = y2 - y1;
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dx + dy));
        }
    }
}