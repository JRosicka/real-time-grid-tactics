using System.Collections.Generic;
using Gameplay.Config;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// For displaying reticles (i.e. icons representing some effect in an area) on the grid. 
    /// </summary>
    public class GridReticleVisualizer : MonoBehaviour {
        [SerializeField] private CatapultTargetReticle _catapultAimReticle;

        public void Initialize(PlayerColorData localPlayerColors) {
            _catapultAimReticle.SetTeam(localPlayerColors);
        }
        
        public void UpdateCatapultAimReticle(HashSet<Vector2Int> cellsAroundVertex) {
            _catapultAimReticle.UpdateLocation(cellsAroundVertex);
        }

        public void HideCatapultAimReticle() {
            _catapultAimReticle.Hide();
        }
    }
}