using System.Collections.Generic;
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


        public void Initialize(FogOfWarManager fowManager) {
            _fowManager = fowManager;
            
            // First check to see if any FoW should be present for this player
            if (fowManager.FowSetting == FogOfWarSetting.None) return;
            
            // Subscribe to events
            fowManager.FoWUpdated += FogOfWarUpdated;

            // Instantiate and set initial FoW state for all cells
            foreach (FogOfWarManager.FoWCell cell in fowManager.GetAllCells()) {
                CellFogOfWar cellView = Instantiate(_cellFowPrefab, GameManager.Instance.GridController.GetWorldPosition(cell.Position), Quaternion.identity, transform);
                _cellViews[cell.Position] = cellView;

                cellView.SetHiddenState(cell.Hidden, false);
            }
        }

        private void OnDestroy() {
            if (_fowManager != null) {
                _fowManager.FoWUpdated -= FogOfWarUpdated;
            }
        }

        private void FogOfWarUpdated(List<FogOfWarManager.FoWCell> foWCells) {
            foreach (FogOfWarManager.FoWCell cell in foWCells) {
                _cellViews[cell.Position].SetHiddenState(cell.Hidden, true);
            }
        }
    }
}