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
        
        // true/false depending on hidden/shown. Can be empty if FoW is set to None. 
        private readonly Dictionary<Vector2Int, bool> _cellFoWState = new Dictionary<Vector2Int, bool>();
        private readonly GridController _gridController;
        // Calculated per player
        private readonly FogOfWarSetting _fowSetting;
        
        public FogOfWarManager(GridController gridController, FogOfWarSetting fowSetting, bool realGame, GameTeam localTeam) {
            _gridController = gridController;
            _fowSetting = DetermineFoWSettingForMatch(fowSetting, realGame, localTeam);

            if (_fowSetting != FogOfWarSetting.None) {
                // Initialize cells and set initial FoW state for each cell 
                foreach (Vector2Int cell in gridController.GetAllCellsInBounds()) {
                    _cellFoWState.Add(cell, DetermineFoWState(cell));
                }
            }
        }
        
        public IEnumerable<FoWCell> GetAllCells() {
            return _cellFoWState.Select(kvp => new FoWCell { Position = kvp.Key, Hidden = kvp.Value });
        }

        private bool DetermineFoWState(Vector2Int cell) {
            if (_fowSetting == FogOfWarSetting.None) return false;
            
            // TODO read GridController state and perform logic here
            return false;
        }

        private FogOfWarSetting DetermineFoWSettingForMatch(FogOfWarSetting fowSetting, bool realGame, GameTeam localPlayerTeam) {
            if (!realGame) return FogOfWarSetting.None;
            if (localPlayerTeam == GameTeam.Spectator) return FogOfWarSetting.None;
            return fowSetting;
        }
    }
}