using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Entities;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// For displaying reticles (i.e. icons representing some effect in an area) on the grid. 
    /// </summary>
    public class GridReticleVisualizer : MonoBehaviour {
        [SerializeField] private CatapultTargetReticle _catapultAimReticle;
        [SerializeField] private CatapultTargetReticle _incomingCatapultShotPrefab;

        public void Initialize(GameTeam localTeam) {
            _catapultAimReticle.Initialize(localTeam, false);
        }
        
        #region Catapult Aim Reticle
        
        public void UpdateCatapultAimReticle(HashSet<Vector2Int> vertex) {
            _catapultAimReticle.UpdateLocation(vertex);
        }

        public void HideCatapultAimReticle() {
            _catapultAimReticle.Hide();
        }

        #endregion
        #region Catapult Incoming Reticle
        
        public CatapultTargetReticle ShowIncomingCatapultTarget(HashSet<Vector2Int> vertex, GridEntity attacker) {
            CatapultTargetReticle newReticle = Instantiate(_incomingCatapultShotPrefab, GameManager.Instance.CommandManager.SpawnBucket);
            newReticle.Initialize(attacker.Team, true);
            newReticle.UpdateLocation(vertex);
            return newReticle;
        }
        
        #endregion
    }
}