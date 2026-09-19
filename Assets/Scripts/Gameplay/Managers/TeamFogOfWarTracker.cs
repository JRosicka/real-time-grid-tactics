using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Grid;
using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles fog of war state and updates for a specific team
    /// </summary>
    public class TeamFogOfWarTracker {
        public class FoWCell {
            public Vector2Int Position;
            public bool Hidden;
        }

        private readonly Dictionary<Vector2Int, bool> _cellFoWState = new();
        private readonly Dictionary<GridEntity, Action<bool>> _entityListeners = new();
        
        private readonly GameTeam _team;
        private readonly FogOfWarSetting _fowSetting;
        private readonly GridController _gridController;
        private readonly ICommandManager _commandManager;
        private readonly bool _updateEntityVisuals;

        public Action<List<FoWCell>> FoWUpdated;
        
        // Cached until movement/register/unregister occurs
        private List<Vector2Int> FriendlyEntityPositions => _commandManager.EntitiesOnGrid.LocationsWithFriendlyEntities(_team);
        
        private IEnumerable<Vector2Int> CellsInRange(Vector2Int location) => _gridController.GridData.GetCellsInRange(location, VisionRange).Select(c => c.Location);
        private int VisionRange => (int)_fowSetting;
        
        public TeamFogOfWarTracker(GameTeam team, FogOfWarSetting fowSetting, bool updateEntityVisuals, GridController gridController, ICommandManager commandManager) {
            _team = team;
            _fowSetting = fowSetting;
            _gridController = gridController;
            _commandManager = commandManager;
            _updateEntityVisuals = updateEntityVisuals;
            
            if (_fowSetting != FogOfWarSetting.None) {
                // Subscribe to events
                _commandManager.EntityUpdatedEvent += EntityUpdated;
                
                // Initialize cells
                foreach (Vector2Int cell in gridController.GetAllCellsInBounds()) {
                    _cellFoWState.Add(cell, true);
                }
                
                // Set initial FoW state for each cell
                List<Vector2Int> processedCells = new();
                foreach (Vector2Int location in FriendlyEntityPositions) {
                    (_, List<Vector2Int> newProcessedCells) = RevealWithinRange(location, processedCells);
                    processedCells.AddRange(newProcessedCells);
                }
            }
        }
        
        public void UnregisterListeners() {
            if (_commandManager != null) {
                _commandManager.EntityUpdatedEvent -= EntityUpdated;
            }
        }
        
        /// <summary>
        /// Registers to listen for FoW updates for a specific entity. 
        /// </summary>
        public void RegisterEntityListener(GridEntity entity, Action<bool> hiddenStateChangedCallback) {
            _entityListeners[entity] = hiddenStateChangedCallback;
        }

        public void UnregisterEntityListener(GridEntity entity, Action<bool> hiddenStateChangedCallback) {
            if (_entityListeners.TryGetValue(entity, out Action<bool> callback) && callback == hiddenStateChangedCallback) {
                _entityListeners.Remove(entity);
            }
        }

        public bool IsEntityHidden([NotNull] GridEntity entity) {
            if (entity.Location == null) return false;
            return IsLocationHidden(entity.Location.Value);
        }

        public bool IsLocationHidden(Vector2Int location) {
            if (_cellFoWState.Count == 0) return false;
            return _cellFoWState[location];
        }
        
        public IEnumerable<FoWCell> GetAllCells() {
            return _cellFoWState.Select(kvp => new FoWCell { Position = kvp.Key, Hidden = kvp.Value });
        }

        #region Cell update logic
        
        private void EntityUpdated(GridEntity entity, GridEntityCollectionUpdate updateType, Vector2Int previousLocation, Vector2Int newLocation) {
            if (entity == null) {
                Debug.LogWarning("Entity is null, for some reason");
                return;
            }
            if (!EntityProvidesVision(entity)) {
                // This entity will not modify the map FoW, but the entity might need to visually update within the player's FoW view
                if (_updateEntityVisuals) {
                    entity.UpdateFoWHiddenStatus(_cellFoWState[newLocation]);
                }
                return;
            }
            
            List<FoWCell> updatedCells;
            switch (updateType) {
                case GridEntityCollectionUpdate.Register:
                    (updatedCells, _) = RevealWithinRange(newLocation, null);
                    break;
                case GridEntityCollectionUpdate.Unregister:
                    updatedCells = TryHideWithinRange(previousLocation, null);
                    break;
                case GridEntityCollectionUpdate.Move:
                    // Look through cells viewable from new location, mark as not hidden
                    List<Vector2Int> processedCells;
                    (updatedCells, processedCells) = RevealWithinRange(newLocation, null);
            
                    // Look through cells viewable from old location (and not from new), see if they should be marked as hidden
                    updatedCells.AddRange(TryHideWithinRange(previousLocation, processedCells));

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateType), updateType, null);
            }

            SendUpdatedEvents(updatedCells);
        }
        
        /// <summary>
        /// Returns a set of cells that were updated and a set of locations that this processed
        /// </summary>
        private (List<FoWCell>, List<Vector2Int>) RevealWithinRange(Vector2Int originLocation, List<Vector2Int> cellsToSkip) {
            List<FoWCell> updatedCells = new();
            List<Vector2Int> processedCells = CellsInRange(originLocation).ToList();
            cellsToSkip ??= new List<Vector2Int>();
            
            foreach (Vector2Int location in processedCells) {
                if (cellsToSkip.Contains(location)) continue;
                
                bool wasHidden = _cellFoWState[location];
                if (!wasHidden) continue;
                
                _cellFoWState[location] = false;
                updatedCells.Add(new FoWCell { Position = location, Hidden = false });
            }
            
            return (updatedCells, processedCells);
        }

        private List<FoWCell> TryHideWithinRange(Vector2Int originLocation, List<Vector2Int> cellsToSkip) {
            List<FoWCell> updatedCells = new();
            cellsToSkip ??= new List<Vector2Int>();
            
            foreach (Vector2Int location in CellsInRange(originLocation)) {
                if (cellsToSkip.Contains(location)) continue;
                // We know this was not hidden
                
                // Check to see if any other units can see it
                if (CellsInRange(location).All(l => !FriendlyEntityPositions.Contains(l))) {
                    // It must be hidden now
                    _cellFoWState[location] = true;
                    updatedCells.Add(new FoWCell { Position = location, Hidden = true });
                }
            }
            
            return updatedCells;
        }
        
        #endregion

        private bool EntityProvidesVision(GridEntity entity) {
            if (entity.Team == _team) return true;
            if (entity.Team.OpponentTeam() == _team) return false;
            if (_team == GameTeam.Spectator) return true;
            
            return false;
        }
        
        private void SendUpdatedEvents(List<FoWCell> updatedCells) {
            if (!updatedCells.Any()) return;
            
            FoWUpdated?.Invoke(updatedCells);

            foreach (KeyValuePair<GridEntity, Action<bool>> kvp in _entityListeners) {
                GridEntity entity = kvp.Key;
                if (entity.DeadOrDying || entity.Location == null) continue;
                
                FoWCell cell = updatedCells.FirstOrDefault(cell => cell.Position == entity.Location.Value);
                if (cell != null) {
                    kvp.Value.Invoke(cell.Hidden);
                }
            }
        }
    }
}