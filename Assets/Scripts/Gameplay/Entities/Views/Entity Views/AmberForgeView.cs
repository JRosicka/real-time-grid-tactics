using System.Collections.Generic;
using Gameplay.Config.Upgrades;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using Gameplay.Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    public class AmberForgeView : GridEntityParticularView {
        [SerializeField] private GameObject _notificationView;
        [SerializeField] private Image _notificationIcon;
        [SerializeField] private List<ParticleSystem> _upgradeParticles;
        
        private GridEntity _amberForgeEntity;
        
        private AmberForgeAvailabilityNotifier AmberForgeAvailabilityNotifier => GameManager.Instance.AmberForgeAvailabilityNotifier;
        
        public override void Initialize(GridEntity entity) {
            _amberForgeEntity = entity;
            
            // Only do these for actual players, not spectators
            GameTeam localTeam = GameManager.Instance.LocalTeam;
            if (localTeam == GameTeam.Spectator) return;

            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(entity);
            _notificationIcon.sprite = player.ColorData.ColoredButtonData.Normal;

            player.OwnedPurchasablesController.UpgradeCompletedEvent += UpgradeCompleted;
            
            AmberForgeAvailabilityNotifier.AmberForgeAvailabilityChanged += UpdateAvailability;
            UpdateAvailability(entity, false);
        }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            return true;
        }
        
        public override void UpgradeApplied(IUpgrade upgrade) { }

        [Button]
        public void UpgradeCompleted(UpgradeData upgrade) {
            _upgradeParticles.ForEach(p => p.Play());
        }

        private void UpdateAvailability(GridEntity amberForge, bool available) {
            if (amberForge != _amberForgeEntity) return;
            _notificationView.SetActive(available);
        }
    }
}