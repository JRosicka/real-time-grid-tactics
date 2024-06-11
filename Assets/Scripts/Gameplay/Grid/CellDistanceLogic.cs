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
            int x1 = cell1.x - Mathf.FloorToInt(cell1.y / 2f);  // This looks funny, but it's how to convert to cubic coordinates
            int y1 = cell1.y;
            int x2 = cell2.x - Mathf.FloorToInt(cell2.y / 2f);
            int y2 = cell2.y;
            int dx = x2 - x1;
            int dy = y2 - y1;
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), Mathf.Abs(dx + dy));
        }
        
        public static bool AreCellsInStraightLine(Vector2Int cell1, Vector2Int cell2) {
            // If on the same row, then that's easy
            if (cell1.y == cell2.y) return true;
            
            // Convert offset coordinates to cube coordinates
            Vector3Int cell1Cubic = OffsetToCubic(cell1);
            Vector3Int cell2Cubic = OffsetToCubic(cell2);
    
            // Calculate differences
            int dq = cell2Cubic.x - cell1Cubic.x;
            int dr = cell2Cubic.y - cell1Cubic.y;
            int ds = cell2Cubic.z - cell1Cubic.z;

            // If any of these are 0, then these two cells are along that corresponding axis, i.e. they are in a straight line
            return dq == 0 || dr == 0 || ds == 0;
        }

        /// <summary>
        /// Retrieve a list of cells between the origin cell (not inclusive) and the target cell (inclusive).
        /// If the cells are not in a straight line, returns null.
        /// The return list is ordered by distance from the origin cell, from closest to furthest. 
        /// </summary>
        public static List<Vector2Int> GetCellsInStraightLine(Vector2Int originCell, Vector2Int targetCell) {
            // Convert offset coordinates to cube coordinates
            Vector3Int cell1Cubic = OffsetToCubic(originCell);
            Vector3Int cell2Cubic = OffsetToCubic(targetCell);

            // Calculate differences
            int dq = cell2Cubic.x - cell1Cubic.x;
            int dr = cell2Cubic.y - cell1Cubic.y;
            int ds = cell2Cubic.z - cell1Cubic.z;

            // If none of these are 0, then these two cells are not on a straight line
            if (dq != 0 && dr != 0 && ds != 0) return null;

            int Sign(int num) {
                return num > 0 ? 1 : num < 0 ? -1 : 0;
            }
            
            int distance = DistanceBetweenCells(originCell, targetCell);
            if (distance == 0) return null;
            
            List<Vector2Int> cells = new List<Vector2Int>();
            // Interpolate between the two cells in the correct direction, and add each cell on the way to the return list
            for (int i = 1; i < distance; i++) {
                int qInterp = cell1Cubic.x + Sign(dq) * i;
                int rInterp = cell1Cubic.y + Sign(dr) * i;
                int sInterp = cell1Cubic.z + Sign(ds) * i;
                Vector2Int offsetInterp = CubicToOffset(new Vector3Int(qInterp, rInterp, sInterp));
                cells.Add(offsetInterp);
            }
            cells.Add(targetCell);
            return cells;
        }
        
        private static Vector3Int OffsetToCubic(Vector2Int cell) {
            int q = cell.x - Mathf.FloorToInt(cell.y / 2f);
            int r = cell.y;
            int s = -q - r;
            return new Vector3Int(q, r, s);
        }

        private static Vector2Int CubicToOffset(Vector3Int cell) {
            int x = cell.x + Mathf.FloorToInt(cell.y / 2f);
            int y = cell.y;
            return new Vector2Int(x, y);
        }
    }
}