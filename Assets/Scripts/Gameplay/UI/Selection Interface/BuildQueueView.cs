using System.Collections.Generic;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Visual display of a <see cref="GridEntity"/>'s build queue, displayed on the <see cref="SelectionInterface"/>
    /// </summary>
    public class BuildQueueView : MonoBehaviour {
        [SerializeField] private List<AbilitySlot> _slots;
        [SerializeField] private AbilityTimerView _buildTimer; 
        [SerializeField] private AbilityChannel _buildChannel;

        private int SlotCount => _slots.Count;
        private GridEntity _gridEntity;
        private AbilityTimer _activeBuildTimer;
        private AbilityAssignmentManager AbilityAssignmentManager => GameManager.Instance.AbilityAssignmentManager;

        public void SetUpForEntity(GridEntity entity) {
            if (_gridEntity != null) {
                _gridEntity.BuildQueue.BuildQueueUpdated -= UpdateBuildQueue;
            }

            _gridEntity = entity;
            if (entity == null) {
                return;
            }

            GameTeam localTeam = GameManager.Instance.LocalTeam;
            if (entity.EntityData.IsStructure && (entity.InteractBehavior == null || !entity.InteractBehavior.AllowedToSeeQueuedBuilds(localTeam))) {
                // This is a structure whose builds we are not allowed to see
                return;
            }

            gameObject.SetActive(true);
            entity.BuildQueue.BuildQueueUpdated += UpdateBuildQueue;
            UpdateBuildQueue(localTeam, entity.BuildQueue.Queue(localTeam));
        }

        public void Clear() {
            if (_gridEntity != null) {
                _gridEntity.BuildQueue.BuildQueueUpdated -= UpdateBuildQueue;
            }
            if (_buildTimer != null) {
                _buildTimer.UnsubscribeFromTimers();
            }
            _activeBuildTimer = null;

            gameObject.SetActive(false);
        }

        private void UpdateBuildQueue(GameTeam team, List<BuildAbility> currentQueuedAbilities) {
            if (team != GameManager.Instance.LocalTeam) return;
            
            _slots.ForEach(s => s.Clear());
            
            int slotNumber = 0;
            while (slotNumber < SlotCount && slotNumber < currentQueuedAbilities.Count) {
                BuildAbility buildAbility = currentQueuedAbilities[slotNumber];
                QueuedBuildAbilitySlotBehavior buildBehavior = new QueuedBuildAbilitySlotBehavior(buildAbility, _gridEntity);
                _slots[slotNumber].SetUpSlot(buildBehavior, _gridEntity);
                slotNumber++;
            }
            
            if (AbilityAssignmentManager.IsAbilityChannelOnCooldownForEntity(_gridEntity, _buildChannel, out List<AbilityTimer> timers)) {
                _activeBuildTimer = timers[0];
                _buildTimer.gameObject.SetActive(true);
                _buildTimer.Initialize(_activeBuildTimer, false, false, true);
            } else {
                _buildTimer.gameObject.SetActive(false);
            }
        }
    }
}