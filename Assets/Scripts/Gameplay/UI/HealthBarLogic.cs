using System;
using Gameplay.Entities;

namespace Gameplay.UI {
    /// <summary>
    /// <see cref="IBarLogic"/> implementation for a health bar
    /// </summary>
    public class HealthBarLogic : IBarLogic {
        private readonly GridEntity _gridEntity;

        public event Action BarUpdateEvent;
        public event Action BarDestroyEvent;

        public HealthBarLogic(GridEntity gridEntity) {
            _gridEntity = gridEntity;
            gridEntity.HPHandler.CurrentHP.ValueChanged += EntityHPChanged;
            gridEntity.KilledEvent += DestroyBar;
        }
        
        public void UnsubscribeFromEvents() {
            if (_gridEntity != null) {
                _gridEntity.HPHandler.CurrentHP.ValueChanged -= EntityHPChanged;
                _gridEntity.KilledEvent -= DestroyBar;
            }
        }

        private void EntityHPChanged(int oldValue, int newValue, object metadata) {
            BarUpdateEvent?.Invoke();
        }

        private void DestroyBar() {
            BarDestroyEvent?.Invoke();
        }
        
        public float CurrentValue => _gridEntity.HPHandler.CurrentHP.Value;
        public float MaxValue => _gridEntity.MaxHP;
        
        // TODO eventually configure these somewhere else
        public float MinConfigurableValue => 10;
        public float MaxConfigurableValue => 50;
    }
}