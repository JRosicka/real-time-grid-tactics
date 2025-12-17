using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    public class AmberForgeView : GridEntityParticularView {
        [SerializeField] private GameObject _notificationView;
        [SerializeField] private Image _notificationIcon;
        private GridEntity _amberForgeEntity;
        
        private AmberForgeAvailabilityNotifier AmberForgeAvailabilityNotifier => GameManager.Instance.AmberForgeAvailabilityNotifier;
        
        public override void Initialize(GridEntity entity) {
            _amberForgeEntity = entity;
            
            // Only do these for actual players, not spectators
            GameTeam localTeam = GameManager.Instance.LocalTeam;
            if (localTeam == GameTeam.Spectator) return;

            _notificationIcon.sprite = GameManager.Instance.GetPlayerForTeam(entity).Data.ColoredButtonData.Normal;
            
            AmberForgeAvailabilityNotifier.AmberForgeAvailabilityChanged += UpdateAvailability;
            UpdateAvailability(entity, false);
        }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            return true;
        }
        
        private void UpdateAvailability(GridEntity amberForge, bool available) {
            if (amberForge != _amberForgeEntity) return;
            _notificationView.SetActive(available);
        }
    }
}