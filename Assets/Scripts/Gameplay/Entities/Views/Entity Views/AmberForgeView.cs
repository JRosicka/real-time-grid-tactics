using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Upgrades;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using Gameplay.Managers;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    public class AmberForgeView : GridEntityParticularView {
        [SerializeField] private GameObject _notificationView;
        [SerializeField] private Image _notificationIcon;
        [SerializeField] private List<ParticleSystem> _upgradeParticles;
        [SerializeField] private List<ParticleSystem> _teamColorUpgradeParticles;
        [SerializeField] private List<ParticleSystem> _monoTeamColoredUpgradeParticles;
        
        private GridEntity _amberForgeEntity;
        
        private AmberForgeAvailabilityNotifier AmberForgeAvailabilityNotifier => GameManager.Instance.AmberForgeAvailabilityNotifier;
        
        public override void Initialize(GridEntity entity) {
            _amberForgeEntity = entity;
            
            GameManager.Instance.Player1.OwnedPurchasablesController.UpgradeCompletedEvent += UpgradeCompleted;
            GameManager.Instance.Player2.OwnedPurchasablesController.UpgradeCompletedEvent += UpgradeCompleted;
            
            // Only do these for actual players, not spectators
            GameTeam localTeam = GameManager.Instance.LocalTeam;
            if (localTeam == GameTeam.Spectator) return;
            
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
        public void UpgradeCompleted(UpgradeData upgrade, [CanBeNull] GridEntity performer, GameTeam team) {
            // This might have happened at a different Amber Forge
            if (performer != _amberForgeEntity) return;
            
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(team).ColorData;
            _notificationIcon.sprite = colorData.ColoredButtonData.Normal;

            // Set team color particles
            foreach (ParticleSystem particles in _teamColorUpgradeParticles) {
                ParticleSystem.MainModule main = particles.main;
                ParticleSystem.MinMaxGradient colors = main.startColor;
                colors.colorMin = colorData.BrightParticlesColor1;
                colors.colorMax = colorData.BrightParticlesColor2;
                main.startColor = colors;
            }
            foreach (ParticleSystem particles in _monoTeamColoredUpgradeParticles) {
                ParticleSystem.MainModule main = particles.main;
                ParticleSystem.MinMaxGradient colors = main.startColor;
                colors.color = colorData.TeamColor;
                main.startColor = colors;
            }

            _upgradeParticles.ForEach(p => p.Play());
        }

        private void UpdateAvailability(GridEntity amberForge, bool available) {
            if (amberForge != _amberForgeEntity) return;
            _notificationView.SetActive(available);
        }
    }
}