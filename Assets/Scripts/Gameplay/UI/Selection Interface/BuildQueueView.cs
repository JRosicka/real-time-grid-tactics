using System.Collections.Generic;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Visual display of a <see cref="GridEntity"/>'s build queue, displayed on the <see cref="SelectionInterface"/>
    /// </summary>
    public class BuildQueueView : MonoBehaviour {
        [SerializeField] private List<AbilitySlot> _slots;
        [SerializeField] private AbilityTimerCooldownView _buildTimer; 
        [SerializeField] private AbilityChannel _buildChannel;

        private int SlotCount => _slots.Count;
        private GridEntity _gridEntity;
        private AbilityCooldownTimer _activeBuildCooldownTimer;

        public void SetUpForEntity(GridEntity entity) {
            if (_gridEntity != null) {
                _gridEntity.BuildQueue.BuildQueueUpdated -= UpdateBuildQueue;
            }

            _gridEntity = entity;
            if (entity == null) {
                return;
            }

            gameObject.SetActive(true);
            entity.BuildQueue.BuildQueueUpdated += UpdateBuildQueue;
            UpdateBuildQueue(entity.BuildQueue.Queue);
            
            if (entity.IsAbilityChannelOnCooldown(_buildChannel, out _activeBuildCooldownTimer)) {
                _buildTimer.gameObject.SetActive(true);
                _buildTimer.Initialize(_activeBuildCooldownTimer, false, true);
            } else {
                _buildTimer.gameObject.SetActive(false);
            }
        }

        public void Clear() {
            if (_gridEntity != null) {
                _gridEntity.BuildQueue.BuildQueueUpdated -= UpdateBuildQueue;
            }
            if (_buildTimer != null) {
                _buildTimer.UnsubscribeFromTimers();
            }
            _activeBuildCooldownTimer = null;

            gameObject.SetActive(false);
        }

        private void UpdateBuildQueue(List<BuildAbility> currentQueuedAbilities) {
            _slots.ForEach(s => s.Clear());
            
            int slotNumber = 0;
            while (slotNumber < SlotCount && slotNumber < currentQueuedAbilities.Count) {
                BuildAbility buildAbility = currentQueuedAbilities[slotNumber];
                QueuedBuildAbilitySlotBehavior buildBehavior = new QueuedBuildAbilitySlotBehavior(buildAbility, _gridEntity);
                _slots[slotNumber].SetUpSlot(buildBehavior, _gridEntity);
                slotNumber++;
            }
        }
    }
}