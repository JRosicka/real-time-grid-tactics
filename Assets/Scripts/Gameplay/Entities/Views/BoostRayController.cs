using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Handles enabling/disabling/initializing/aiming a <see cref="BoostRayEffect"/>
    /// </summary>
    public class BoostRayController : MonoBehaviour {
        [SerializeField] private BoostRayEffect _effect;
        
        public void Activate(GridEntity target) {
            if (target == null) {
                Deactivate();
                return;
            }
            if (target.View == null) return;
            
            _effect.Initialize(target.View.BoostPosition);
        }

        private void Deactivate() {
            _effect.Hide();
        }
    }
}