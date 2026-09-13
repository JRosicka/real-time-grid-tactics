using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Grid;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Central logic for Fog of War. Handles state management, events, and communicating to entities when FoW state changes. 
    /// Entirely client-side.
    /// </summary>
    public class FogOfWarManager {
        public class FoWCell {
            public Vector2Int Position;
            public bool Hidden;
        }

        public Action<List<FoWCell>> FoWUpdated;
        
        // Calculated per player
        public FogOfWarSetting FowSetting { get; }

        // true/false depending on hidden/shown. Can be empty if FoW is set to None. 
        private readonly Dictionary<Vector2Int, bool> _cellFoWState = new();
        private readonly GridController _gridController;
        private readonly ICommandManager _commandManager;
        private readonly GameTeam _localTeam;
        
        // Cached until movement/register/unregister occurs
        private List<Vector2Int> FriendlyEntityPositions => _commandManager.EntitiesOnGrid.LocationsWithFriendlyEntities(_localTeam);
        
        private IEnumerable<Vector2Int> CellsInRange(Vector2Int location) => _gridController.GridData.GetCellsInRange(location, VisionRange).Select(c => c.Location);
        private int VisionRange => (int)FowSetting;

        public FogOfWarManager(GridController gridController, ICommandManager commandManager, FogOfWarSetting fowSetting, bool realGame, GameTeam localTeam) {
            _gridController = gridController;
            _commandManager = commandManager;
            _localTeam = localTeam;
            FowSetting = DetermineFoWSettingForMatch(fowSetting, realGame, localTeam);

            if (FowSetting != FogOfWarSetting.None) {
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

        public bool IsEntityHidden(GridEntity entity) {
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

        private void EntityUpdated(GridEntity entity, GridEntityCollectionUpdate updateType, Vector2Int previousLocation, Vector2Int newLocation) {
            if (entity.InteractBehavior == null || !entity.InteractBehavior.ProvidesVision) {
                // This entity will not modify the map FoW, but the entity might need to visually update within the player's FoW view
                entity.UpdateFoWHiddenStatus(_cellFoWState[newLocation]);
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

            SendUpdatedEvent(updatedCells);
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

        private void SendUpdatedEvent(List<FoWCell> updatedCells) {
            if (updatedCells.Any()) {
                FoWUpdated?.Invoke(updatedCells);
            }
        }
        
        private FogOfWarSetting DetermineFoWSettingForMatch(FogOfWarSetting fowSetting, bool realGame, GameTeam localPlayerTeam) {
            if (!realGame) return FogOfWarSetting.None;
            if (localPlayerTeam == GameTeam.Spectator) return FogOfWarSetting.None;
            return fowSetting;
        }
    }
}