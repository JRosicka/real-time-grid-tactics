using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// Central view logic for FoW display on the grid. Subscribes to <see cref="FogOfWarManager"/> events. Handles
    /// communicating with a collection of <see cref="CellFogOfWar"/>s.
    ///
    /// This specifically handles tile dimming -- Entity FoW handling happens through <see cref="FogOfWarManager"/>
    /// </summary>
    public class FogOfWarDisplayer : MonoBehaviour {
        [SerializeField] private CellFogOfWar _cellFowPrefab;
        
        private readonly Dictionary<Vector2Int, CellFogOfWar> _cellViews = new Dictionary<Vector2Int, CellFogOfWar>();
        
        private FogOfWarManager _fowManager;


        public void Initialize(FogOfWarManager fowManager, GameTeam localTeam) {
            _fowManager = fowManager;
            
            // First check to see if any FoW should be present for this player
            if (fowManager.LocalFowSetting == FogOfWarSetting.None) return;
            
            // Subscribe to events
            TeamFogOfWarTracker tracker = fowManager.GetLocalTeamTracker();
            tracker!.FoWUpdated += FogOfWarUpdated;

            // Instantiate and set initial FoW state for all cells
            foreach (TeamFogOfWarTracker.FoWCell cell in tracker.GetAllCells()) {
                CellFogOfWar cellView = Instantiate(_cellFowPrefab, GameManager.Instance.GridController.GetWorldPosition(cell.Position), Quaternion.identity, transform);
                _cellViews[cell.Position] = cellView;

                cellView.SetHiddenState(cell.Hidden, false);
            }
        }

        private void OnDestroy() {
            TeamFogOfWarTracker tracker = _fowManager?.GetLocalTeamTracker();
            if (tracker != null) {
                tracker.FoWUpdated -= FogOfWarUpdated;
            }
        }

        private void FogOfWarUpdated(List<TeamFogOfWarTracker.FoWCell> foWCells) {
            foreach (TeamFogOfWarTracker.FoWCell cell in foWCells) {
                _cellViews[cell.Position].SetHiddenState(cell.Hidden, true);
            }
        }
    }
}