using System.Collections.Generic;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Entities {
    public class WorkshopView : GridEntityParticularView {
        [SerializeField] private List<ParticleSystem> _smokeParticleSystems;

        private GridEntity _entity;

        private TeamFogOfWarTracker LocalTracker => GameManager.Instance.FogOfWarManager!.GetLocalTeamTracker();

        public override void Initialize(GridEntity entity) {
            _entity = entity;
        }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            return ability.AbilityData switch {
                _ => true
            };
        }

        public override void UpgradeApplied(IUpgrade upgrade) { }

        public override void InitializeFoW() {
            ReEvaluateFoWHiddenStatus();
            if (LocalTracker != null) {
                LocalTracker.FoWUpdated += FoWUpdated;
            }
        }

        private void FoWUpdated(List<TeamFogOfWarTracker.FoWCell> foWCells) {
            ReEvaluateFoWHiddenStatus();
        }

        private void ReEvaluateFoWHiddenStatus() {
            if (LocalTracker == null) return;

            bool hidden = LocalTracker.IsEntityHidden(_entity);
            foreach (ParticleSystem particles in _smokeParticleSystems) {
                if (hidden) {
                    particles.Stop();
                } else {
                    particles.Play();
                }
            }
        }
    }
}