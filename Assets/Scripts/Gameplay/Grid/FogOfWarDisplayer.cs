using System.Collections.Generic;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// Central view logic for FoW display on the grid. Subscribes to <see cref="FogOfWarManager"/> events. Handles
    /// communicating with a collection of <see cref="CellFogOfWar"/>s.
    ///
    /// TODO should communication with GridEntities to show/hide them happen here? Probably better for in the manager huh, since that handles gameplay state. 
    /// </summary>
    public class FogOfWarDisplayer : MonoBehaviour {
        [SerializeField] private CellFogOfWar _cellFowPrefab;
        
        private readonly Dictionary<Vector2Int, CellFogOfWar> _cellViews = new Dictionary<Vector2Int, CellFogOfWar>();
        
        private FogOfWarManager _fowManager;


        public void Initialize(FogOfWarManager fowManager) {
            // Subscribe to events TODO
            _fowManager = fowManager;
            // TODO but first check to see if any FoW should be present for this player, based on game type and spectator status
            
            // Instantiate and set initial FoW state for all cells
            foreach (FogOfWarManager.FoWCell cell in fowManager.GetAllCells()) {
                CellFogOfWar cellView = Instantiate(_cellFowPrefab, GameManager.Instance.GridController.GetWorldPosition(cell.Position), Quaternion.identity, transform);
                _cellViews[cell.Position] = cellView;

                cellView.SetHiddenState(cell.Hidden, false);
            }
        }

        private void FogOfWarUpdated(IEnumerable<Vector2> newHiddenCells, IEnumerable<Vector2> newShownCells) {
            // TODO
        }
    }
}