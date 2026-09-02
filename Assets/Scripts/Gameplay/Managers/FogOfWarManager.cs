using System.Collections.Generic;
using System.Linq;
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
        
        // true/false depending on hidden/shown
        private readonly Dictionary<Vector2Int, bool> _cellFoWState = new Dictionary<Vector2Int, bool>();
        private readonly GridController _gridController;
        
        public FogOfWarManager(GridController gridController) {
            _gridController = gridController;
            
            // Initialize cells and set initial FoW state for each cell 
            foreach (Vector2Int cell in gridController.GetAllCellsInBounds()) {
                _cellFoWState.Add(cell, DetermineFoWState(cell));
            }
        }
        
        public IEnumerable<FoWCell> GetAllCells() {
            return _cellFoWState.Select(kvp => new FoWCell { Position = kvp.Key, Hidden = kvp.Value });
        }

        private bool DetermineFoWState(Vector2Int cell) {
            // TODO read GridController state and perform logic here
            return false;
        }
    }
}