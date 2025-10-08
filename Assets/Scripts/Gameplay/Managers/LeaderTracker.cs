using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Grid;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles tracking and providing state info about the leaders in the game.
    /// Entirely client-side.
    /// </summary>
    public class LeaderTracker {
        private readonly Dictionary<GridEntity, Vector2Int> _leaders =  new Dictionary<GridEntity, Vector2Int>();

        public event Action<GridEntity> LeaderMoved;

        public void Initialize(ICommandManager commandManager) {
            List<GridEntity> leaders = commandManager.EntitiesOnGrid.AllEntities()
                .Where(e => e.EntityData == GameManager.Instance.Configuration.KingEntityData).ToList();
            foreach (GridEntity leader in leaders) {
                _leaders[leader] = leader.Location!.Value;
            }
            
            commandManager.EntityCollectionChangedEvent += EntityCollectionChanged;
        }

        public bool IsAdjacentToFriendlyLeader(Vector2Int position, GameTeam team) {
            Vector2Int? leaderPosition = LeaderPosition(team);
            if (leaderPosition == null) return false;
            return CellDistanceLogic.Neighbors(leaderPosition.Value).Contains(position);
        }

        private Vector2Int? LeaderPosition(GameTeam team) {
            GridEntity leader = _leaders.Keys.FirstOrDefault(l => l.Team == team);
            return leader?.Location;
        }

        private void EntityCollectionChanged() {
            List<GridEntity> leaders = _leaders.Keys.ToList();
            foreach (GridEntity leader in leaders) {
                if (leader.Location != null && leader.Location.Value != _leaders[leader]) {
                    _leaders[leader] = leader.Location.Value;
                    LeaderMoved?.Invoke(leader);
                }
            }
        }
    }
}