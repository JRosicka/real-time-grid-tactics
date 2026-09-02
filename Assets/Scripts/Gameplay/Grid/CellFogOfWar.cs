using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Grid {
    /// <summary>
    /// Handles showing/hiding a FoW visual for a single cell
    /// </summary>
    public class CellFogOfWar : MonoBehaviour {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeSeconds = .1f;

        private bool _targetHidden;
        private float _fadeProgress01;

        public void SetHiddenState(bool hidden, bool animate) {
            _targetHidden = hidden;
            if (!animate) {
                _canvasGroup.alpha = hidden ? 1 : 0;
            }
        }

        [Button]
        public void SetHidden() {
            SetHiddenState(true, false);
        }
        
        [Button]
        public void SetShown() {
            SetHiddenState(false, true);
        }

        private void Update() {
            if (_fadeProgress01 <= 0 && !_targetHidden) return;
            if (_fadeProgress01 >= 1 && _targetHidden) return;
            
            int multiplier = _targetHidden ? 1 : -1;
            _fadeProgress01 += multiplier * Time.deltaTime / _fadeSeconds;
            _fadeProgress01 = Mathf.Clamp01(_fadeProgress01);
            _canvasGroup.alpha = _fadeProgress01;
        }
    }
}