using Gameplay.Entities;
using Gameplay.Entities.Abilities;

namespace Gameplay.UI {
    /// <summary>
    /// Handles moving a <see cref="SelectionReticle"/> around to different <see cref="GridEntity"/>s and following
    /// their movements.
    /// </summary>
    public class SelectionReticleEntityTracker {
        private SelectionReticle _reticle;
        private GridEntity _entity;

        public void Initialize(SelectionReticle reticle) {
            _reticle = reticle;
        }

        public void TrackEntity(GridEntity entity) {
            // Unregister previous entity events
            if (_entity) {
                _entity.AbilityPerformedEvent -= OnAbilityPerformed;
                _entity.UnregisteredEvent -= OnEntityUnregistered;
            }

            _entity = entity;
            // Register new entity events
            if (_entity) {
                _entity.AbilityPerformedEvent += OnAbilityPerformed;
                _entity.UnregisteredEvent += OnEntityUnregistered;
            }
            UpdateReticle();
        }

        /// <summary>
        /// Move the reticle to the location of the tracked entity (or hide if it does not exist or is not interactable)
        /// </summary>
        private void UpdateReticle() {
            if (_entity == null || !_entity.Interactable) {
                _reticle.Hide();
            } else {
                _reticle.SelectTile(_entity.Location, _entity);
            }
        }

        private void OnAbilityPerformed(IAbility ability, AbilityCooldownTimer timer) {
            // We only care about the entity moving
            if (ability is MoveAbility) {
                UpdateReticle();
            }
        }

        private void OnEntityUnregistered() {
            UpdateReticle();
        }
    }
}