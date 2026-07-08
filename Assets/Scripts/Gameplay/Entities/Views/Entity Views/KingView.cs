using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Config.Upgrades;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Entities {
    public class KingView : GridEntityParticularView {
        [SerializeField] private int _minSecondsBetweenUnderAttackAlerts = 30;
        [SerializeField] private ParadeAnimationBehavior _paradeAnimationPrefab;
        
        [SerializeField] private InspiringPresenceUpgradeData _inspiringPresenceUpgrade;
        [SerializeField] private List<ParticleSystem> _inspiringPresenceParticles;

        private GridEntity _entity;
        private float _timeOfLastDamageReceived;

        public override void Initialize(GridEntity entity) {
            _entity = entity;
            SetParticleColors();
        }

        public override void LethalDamageReceived() {
            DoInspiringPresenceAnimation(false);
        }
        
        public override void NonLethalDamageReceived() {
            if (_entity.Team != GameManager.Instance.LocalTeam) return;
            if (Time.time - _timeOfLastDamageReceived < _minSecondsBetweenUnderAttackAlerts) return;
            
            _timeOfLastDamageReceived = Time.time;
            GameManager.Instance.AlertTextDisplayer.DisplayAlert("Your King is under attack!");
        }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability.AbilityData) {
                case ParadeAbilityData _:
                    DoParadeAnimation();
                    return false;
                default:
                    return true;
            }
        }

        public override void UpgradeApplied(IUpgrade upgrade) {
            if (upgrade.UpgradeData == _inspiringPresenceUpgrade) {
                DoInspiringPresenceAnimation(true);
            }
        }

        private void DoParadeAnimation() {
            ParadeAnimationBehavior animationBehavior = Instantiate(_paradeAnimationPrefab, GameManager.Instance.CommandManager.SpawnBucket);
            animationBehavior.transform.position = GameManager.Instance.GridController.GetWorldPosition(_entity.Location!.Value);
            animationBehavior.Initialize(_entity);
        }

        private void SetParticleColors() {
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(_entity).ColorData;

            foreach (ParticleSystem particles in _inspiringPresenceParticles) {
                ParticleSystem.MainModule main = particles.main;
                ParticleSystem.MinMaxGradient colors = main.startColor;
                colors.colorMin = colorData.BrightParticlesColor1;
                colors.colorMax = colorData.BrightParticlesColor2;
                main.startColor = colors;
            }
        }
        
        private void DoInspiringPresenceAnimation(bool enable) {
            if (enable) {
                _inspiringPresenceParticles.ForEach(particle => particle.Play());
            } else {
                _inspiringPresenceParticles.ForEach(particle => particle.Stop(true, ParticleSystemStopBehavior.StopEmitting));
            }
        }
    }
}