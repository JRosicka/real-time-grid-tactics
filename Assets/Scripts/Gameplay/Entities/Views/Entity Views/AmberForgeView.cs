using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Entities {
    public class AmberForgeView : GridEntityParticularView {
        [SerializeField] private GameObject _notificationView;
        
        private AmberForgeAvailabilityNotifier AmberForgeAvailabilityNotifier => GameManager.Instance.AmberForgeAvailabilityNotifier;
        
        public override void Initialize(GridEntity entity) {
            AmberForgeAvailabilityNotifier.EnhancementAvailabilityChanged += UpdateAvailability;
            UpdateAvailability(AmberForgeAvailabilityNotifier.EnhancementAvailable);
        }
        public override void LethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            return true;
        }
        
        private void UpdateAvailability(bool available) {
            _notificationView.SetActive(available);
        }
    }
}