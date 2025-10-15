using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    public class AmberForgeView : GridEntityParticularView {
        [SerializeField] private GameObject _notificationView;
        [SerializeField] private Image _notificationIcon;
        
        private AmberForgeAvailabilityNotifier AmberForgeAvailabilityNotifier => GameManager.Instance.AmberForgeAvailabilityNotifier;
        
        public override void Initialize(GridEntity entity) {
            // Only do these for actual players, not spectators
            GameTeam localTeam = GameManager.Instance.LocalTeam;
            if (localTeam == GameTeam.Spectator) return;

            _notificationIcon.sprite = GameManager.Instance.GetPlayerForTeam(entity).Data.ColoredButtonData.Normal;
            
            AmberForgeAvailabilityNotifier.AmberForgeAvailabilityChanged += UpdateAvailability;
            UpdateAvailability(AmberForgeAvailabilityNotifier.AmberForgeAvailable);
        }
        public override void LethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            return true;
        }
        
        private void UpdateAvailability(bool available) {
            _notificationView.SetActive(available);
        }
    }
}