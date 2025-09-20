using System;
using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Grid;
using UnityEngine;

namespace Gameplay.Pathfinding {
    /// <summary>
    /// Represents a single node in the grid and the related pathfinding components. A set of these represents a path. 
    /// </summary>
    public class GridNode : IComparable<GridNode>, IEqualityComparer<GridNode> {
        /// <summary>
        /// The cell this represents
        /// </summary>
        private readonly GridData.CellData _cellData;
        private readonly GridEntity _entity;
        private List<GridData.CellData> NeighborCells => _cellData.Neighbors;
        private List<GridNode> _neighbors;

        public IEnumerable<GridNode> Neighbors {
            get {
                if (_neighbors == null) {
                    _neighbors = new List<GridNode>();
                    foreach (GridData.CellData neighborCell in NeighborCells) {
                        _neighbors.Add(new GridNode(_entity, neighborCell, _ignoreOtherEntities));
                    }
                }

                return _neighbors;
            }
        }

        public Vector2Int Location => _cellData.Location;
        
        /// <summary>
        /// Whether this node can be included in the path
        /// </summary>
        public bool Walkable { get; }
        /// <summary>
        /// The fastest that the entity can enter any cell. We want to use this as a basis for getting the distance between
        /// nodes so that the H cost will always be below the actual cost. 
        /// </summary>
        private readonly float _fastestEnterTime;
        private readonly bool _ignoreOtherEntities;

        /// <summary>
        /// The fastest possible travel time between this node and the destination. Assumes the fastest entrance speed because this is
        /// for the heuristic.
        /// </summary>
        public float GetDistance(Vector2Int destination) => _fastestEnterTime
            * CellDistanceLogic.DistanceBetweenCells(_cellData.Location, destination);
        
        public GridNode(GridEntity entity, GridData.CellData cellData, bool ignoreOtherEntities) {
            _entity = entity;
            _cellData = cellData;
            _fastestEnterTime = entity.EntityData.NormalMoveTime;
            _ignoreOtherEntities = ignoreOtherEntities;

            Walkable = entity.CanPathFindToTile(cellData.Tile) && (ignoreOtherEntities || PathfinderService.CanEntityEnterCell(cellData.Location, 
                entity.EntityData, entity.Team, forRallying:entity.EntityData.CanRally));
        }

        public float CostToEnter() {
            return _entity.MoveTimeToTile(_cellData.Tile);
        }
        
        public GridNode Connection { get; private set; }
        public void SetConnection(GridNode node) {
            Connection = node;
        }
        
        public float G { get; private set; }
        public void SetG(float g) {
            G = g;
        }
        public float H { get; private set; }
        public void SetH(float h) {
            H = h;
        }
        public float F => G + H;

        /// <summary>
        /// Compare using F cost, then H cost
        /// </summary>
        public int CompareTo(GridNode other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var fComparison = F.CompareTo(other.F);
            if (fComparison != 0) return fComparison;
            return H.CompareTo(other.H);
        }

        /// <summary>
        /// Compare equality using location
        /// </summary>
        public bool Equals(GridNode x, GridNode y) {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            // We only really care about the locations - if the locations match, the rest of the nodes should have been set up in the same way
            return x.Location == y.Location;
        }

        public int GetHashCode(GridNode obj) {
            return obj.Location.GetHashCode();
        }
    }
}