using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// Logic for determining cell distances and adjacency. Since we work with hex cells, this can be a little unintuitive.
    /// </summary>
    public static class CellDistanceLogic {
        private const float FloatTolerance = 0.1f;
        
        private static readonly Vector2Int
            Left = new Vector2Int(-1, 0),
            Right = new Vector2Int(1, 0),
            Down = new Vector2Int(0, -1),
            DownLeft = new Vector2Int(-1, -1),
            DownRight = new Vector2Int(1, -1),
            Up = new Vector2Int(0, 1),
            UpLeft = new Vector2Int(-1, 1),
            UpRight = new Vector2Int(1, 1);

        /// <summary>
        /// The angle of each given hex direction in relation to Vector.Right
        /// </summary>
        public enum DirectionAngle {
            Right = 0,
            UpRight = 60,
            UpLeft = 120,
            Left = 180,
            DownLeft = 240,
            DownRight = 300
        }

        private static readonly Vector2Int[] DirectionsWhenYIsEven = { Right, Up, UpLeft, Left, DownLeft, Down };
        private static readonly Vector2Int[] DirectionsWhenYIsOdd = { Right, UpRight, Up, Left, Down, DownRight };

        /// <summary>
        /// Gets the set of cells that neighbor the given one. These cells may or may not actually exist - we always
        /// return the 6 cells that would be adjacent to the given one.
        /// </summary>
        public static IEnumerable<Vector2Int> Neighbors(Vector2Int cell) {
            Vector2Int[] directions = cell.y % 2 == 0 ? DirectionsWhenYIsEven : DirectionsWhenYIsOdd;
            return directions.Select(direction => cell + direction);
        }

        private static Vector2Int NeighborInDirection(Vector2Int cell, DirectionAngle direction) {
            Vector2Int[] directions = cell.y % 2 == 0 ? DirectionsWhenYIsEven : DirectionsWhenYIsOdd;
            return direction switch {
                DirectionAngle.Right => cell + directions[0],
                DirectionAngle.UpRight => cell + directions[1],
                DirectionAngle.UpLeft => cell + directions[2],
                DirectionAngle.Left => cell + directions[3],
                DirectionAngle.DownLeft => cell + directions[4],
                DirectionAngle.DownRight => cell + directions[5],
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid direction")
            };
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

        /// <summary>
        /// Gets either one or two of the closest <see cref="DirectionAngle"/>s from the origin to the target.
        /// - If the target cell is along the line of a given direction, then only that direction will be returned.
        /// - Otherwise the two directions of the lines on either side of the line between the origin and target are returned.
        /// Also returns a bool flag indicating whether the angle of the origin-to-target line is equidistant from the
        /// angles of the return lines
        /// </summary>
        private static (List<DirectionAngle>, bool) GetClosestDirections(Vector2Int originCell, Vector2Int targetCell) {
            Vector2 originWorldPosition = GameManager.Instance.GridController.GetWorldPosition(originCell);
            Vector2 targetWorldPosition = GameManager.Instance.GridController.GetWorldPosition(targetCell);
            float lineAngleRad = Mathf.Atan2(targetWorldPosition.y - originWorldPosition.y, targetWorldPosition.x - originWorldPosition.x);
            float lineAngleDeg = Mathf.Rad2Deg * lineAngleRad;
            if (lineAngleDeg < 0) lineAngleDeg += 360;

            if (Math.Abs(lineAngleDeg - (float)DirectionAngle.Right) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.Right }, true);
            }
            if (Math.Abs(lineAngleDeg - (float)DirectionAngle.UpRight) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.UpRight }, true);
            }
            if (Math.Abs(lineAngleDeg - (float)DirectionAngle.UpLeft) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.UpLeft }, true);
            }
            if (Math.Abs(lineAngleDeg - (float)DirectionAngle.Left) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.Left }, true);
            }
            if (Math.Abs(lineAngleDeg - (float)DirectionAngle.DownLeft) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.DownLeft }, true);
            }
            if (Math.Abs(lineAngleDeg - (float)DirectionAngle.DownRight) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.DownRight }, true);
            }

            if (Math.Abs(lineAngleDeg - 30f) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.Right, DirectionAngle.UpRight }, true);
            }
            if (Math.Abs(lineAngleDeg - 90f) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.UpRight, DirectionAngle.UpLeft }, true);
            }
            if (Math.Abs(lineAngleDeg - 150f) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.UpLeft, DirectionAngle.Left }, true);
            }
            if (Math.Abs(lineAngleDeg - 210f) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.Left, DirectionAngle.DownLeft }, true);
            }
            if (Math.Abs(lineAngleDeg - 270f) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.DownLeft, DirectionAngle.DownRight }, true);
            }
            if (Math.Abs(lineAngleDeg - 330f) < FloatTolerance) {
                return (new List<DirectionAngle> { DirectionAngle.DownRight, DirectionAngle.Right }, true);
            }
            
            return lineAngleDeg switch {
                < 30f => (new List<DirectionAngle> { DirectionAngle.Right, DirectionAngle.UpRight }, false),
                < 60f => (new List<DirectionAngle> { DirectionAngle.UpRight, DirectionAngle.Right }, false),
                < 90f => (new List<DirectionAngle> { DirectionAngle.UpRight, DirectionAngle.UpLeft }, false),
                < 120f => (new List<DirectionAngle> { DirectionAngle.UpLeft, DirectionAngle.UpRight }, false),
                < 150f => (new List<DirectionAngle> { DirectionAngle.UpLeft, DirectionAngle.Left }, false),
                < 180f => (new List<DirectionAngle> { DirectionAngle.Left, DirectionAngle.UpLeft }, false),
                < 210f => (new List<DirectionAngle> { DirectionAngle.Left, DirectionAngle.DownLeft }, false),
                < 240f => (new List<DirectionAngle> { DirectionAngle.DownLeft, DirectionAngle.Left }, false),
                < 270f => (new List<DirectionAngle> { DirectionAngle.DownLeft, DirectionAngle.DownRight }, false),
                < 300f => (new List<DirectionAngle> { DirectionAngle.DownRight, DirectionAngle.DownLeft }, false),
                < 330f => (new List<DirectionAngle> { DirectionAngle.DownRight, DirectionAngle.Right }, false),
                < 360f => (new List<DirectionAngle> { DirectionAngle.Right, DirectionAngle.DownRight }, false),
                _ => throw new Exception($"Failed to find direction angle between origin ({originCell.x}, {originCell.y}) and target ({targetCell.x}, {targetCell.y}) cells.")
            };
        }
        
        /// <summary>
        /// Gets either one or two lists of cell locations in the closest straight lines from the origin to the target.
        /// - If the target cell is along the line of a given direction, then only cells in that direction will be returned.
        /// - Otherwise cells in the two directions of the lines on either side of the line between the origin and target are returned.
        /// Also returns a bool flag indicating whether the angle of the origin-to-target line is equidistant from the
        /// angles of the return lines
        /// The origin cell is not included in the returned list(s).
        /// </summary>
        public static (List<Vector2Int>, List<Vector2Int>, bool) GetCellsInClosestStraightLines(Vector2Int origin, Vector2Int target, int range) {
            (List<DirectionAngle> directions, bool equidistant) = GetClosestDirections(origin, target);
            List<List<Vector2Int>> cellSets = new();

            foreach (DirectionAngle direction in directions) {
                List<Vector2Int> cellsInLine = new();
                Vector2Int searchingCell = origin;
                for (int i = 0; i < range; i++) {
                    searchingCell = NeighborInDirection(searchingCell, direction);
                    cellsInLine.Add(searchingCell);
                }
                
                cellSets.Add(cellsInLine);
            }
            
            return (cellSets[0], cellSets.Count > 1 ? cellSets[1] : null, equidistant);
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