using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

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
            UpdateReticle(entity);
        }

        /// <summary>
        /// Move the reticle to the location of the tracked entity (or hide if it does not exist or is not interactable)
        /// </summary>
        private void UpdateReticle(Vector2Int newLocation) {
            if (_entity == null || !_entity.Interactable) {
                _reticle.Hide();
            } else {
                _reticle.SelectTile(newLocation, _entity); 
            }
        }

        private void UpdateReticle(GridEntity gridEntity) {
            UpdateReticle(gridEntity == null ? Vector2Int.zero : gridEntity.Location);
        }

        private void OnAbilityPerformed(IAbility ability, AbilityCooldownTimer timer) {
            // We only care about the entity moving
            if (ability is MoveAbility moveAbility) {
                UpdateReticle(moveAbility.AbilityParameters.NextMoveCell);
            }
        }

        private void OnEntityUnregistered() {
            UpdateReticle(_entity);
        }
    }
}