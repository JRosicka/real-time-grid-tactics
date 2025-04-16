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
            gridEntity.HPHandler.HPChangedEvent += EntityHPChanged;
            gridEntity.KilledEvent += DestroyBar;
        }
        
        public void UnsubscribeFromEvents() {
            if (_gridEntity != null) {
                _gridEntity.HPHandler.HPChangedEvent -= EntityHPChanged;
                _gridEntity.KilledEvent -= DestroyBar;
            }
        }

        private void EntityHPChanged() {
            BarUpdateEvent?.Invoke();
        }

        private void DestroyBar() {
            BarDestroyEvent?.Invoke();
        }
        
        public float CurrentValue => _gridEntity.HPHandler.CurrentHP;
        public float MaxValue => _gridEntity.MaxHP;
        
        // TODO eventually configure these somewhere else
        public float MinConfigurableValue => 20;
        public float MaxConfigurableValue => 100;
    }
}