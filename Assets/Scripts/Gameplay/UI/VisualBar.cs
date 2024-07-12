using Gameplay.Entities;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Displays some current/max stat of a <see cref="GridEntity"/> as a bar above/near the entity.
    /// Business logic is determined by the provided <see cref="IBarLogic"/> instance. 
    /// </summary>
    public class VisualBar : MonoBehaviour {
        public RectTransform BarFilling;
        public RectTransform BarFrame;
        public float MaxBarWidth;
        public float MinBarWidth;
        public float AdditionalWidthForFrame;

        private IBarLogic _barLogic;
        private float _maxWidthForThisEntity;
        private bool _frameInitialized;
        
        public void Initialize(IBarLogic barLogic) {
            barLogic.BarUpdateEvent += UpdateBar;
            barLogic.BarDestroyEvent += DestroyBar;
            _barLogic = barLogic;
            
            // Set the initial bar size
            float maxBarLerp = Mathf.InverseLerp(_barLogic.MinConfigurableValue, _barLogic.MaxConfigurableValue, _barLogic.MaxValue);
            _maxWidthForThisEntity = Mathf.Lerp(MinBarWidth, MaxBarWidth, maxBarLerp);

            UpdateBar();
        }
        
        private void OnDestroy() {
            _barLogic.UnsubscribeFromEvents();
        }

        private void UpdateBar() {
            BarFilling.sizeDelta = new Vector2(_barLogic.CurrentValue / _barLogic.MaxValue * _maxWidthForThisEntity, BarFilling.sizeDelta.y);
            InitializeFrame(); 
        }
    
        private void InitializeFrame() {
            if (_frameInitialized) return;
            
            _frameInitialized = _barLogic.CurrentValue > 0;
            BarFrame.sizeDelta = new Vector2(BarFilling.rect.width + AdditionalWidthForFrame, BarFrame.sizeDelta.y);
        }

        private void DestroyBar() {
            Destroy(gameObject);
        }
    }
}