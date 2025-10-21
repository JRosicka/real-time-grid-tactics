using Gameplay.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Displays some current/max stat of a <see cref="GridEntity"/> as a bar.
    /// Business logic is determined by the provided <see cref="IBarLogic"/> instance.
    /// Old logic. TODO combine this with VisualBar to allow for scaled health bars. 
    /// </summary>
    public class VisualBarV2 : MonoBehaviour {
        public Image BarFilling;
        public HealthBarDividersView HealthBarDividersView;

        private IBarLogic _barLogic;
        private bool _initialized;
        private bool _frameInitialized;
        
        public void Initialize(IBarLogic barLogic) {
            barLogic.BarUpdateEvent += UpdateBar;
            barLogic.BarDestroyEvent += DestroyBar;
            _barLogic = barLogic;
            HealthBarDividersView.Initialize(barLogic.MaxValue);
            
            _initialized = true;
            
            // Set the initial bar size
            UpdateBar();
        }
        
        private void OnDestroy() {
            if (_initialized) {
                _barLogic.UnsubscribeFromEvents();
            }
        }

        private void UpdateBar() {
            BarFilling.fillAmount = _barLogic.CurrentValue / _barLogic.MaxValue;
        }
        
        private void DestroyBar() {
            Destroy(gameObject);
        }
    }
}