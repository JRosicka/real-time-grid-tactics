using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.BuildQueue;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Selectable <see cref="GridEntity"/> info for display in the <see cref="SelectionInterface"/>
    /// </summary>
    public class SelectableGridEntityLogic : ISelectableObjectLogic {
        public SelectableGridEntityLogic(GridEntity entity) {
            Entity = entity;
        }

        [NotNull] public GridEntity Entity { get; }
        [NotNull] public IBuildQueue BuildQueue => Entity.BuildQueue;
        public string Name => Entity.EntityData.ID;
        public string ShortDescription => Entity.EntityData.ShortDescription;
        public string LongDescription => Entity.EntityData.Description;
        public string Tags => string.Join(", ", Entity.EntityData.Tags);
        public bool DisplayHP => Entity.EntityData.HP > 0;
        public BuildAbility InProgressBuild {
            get {
                List<BuildAbility> buildQueue = BuildQueue.Queue;
                if (!Entity.EntityData.IsStructure && buildQueue.Count > 0 &&
                    !Entity.QueuedAbilities.Select(a => a.UID).Contains(buildQueue[0].UID)) {
                    return buildQueue[0];
                }

                return null;
            }
        }

        public void SetUpIcons(Image entityIcon, Image entityColorsIcon, Canvas entityColorsCanvas, int teamColorsCanvasSortingOrder) {
            EntityData entityData = Entity.EntityData;
            entityIcon.sprite = entityData.BaseSpriteIconOverride == null ? entityData.BaseSprite : entityData.BaseSpriteIconOverride;
            entityColorsIcon.sprite = entityData.TeamColorSprite;
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(Entity.Team);
            entityColorsIcon.gameObject.SetActive(true);
            entityColorsIcon.color = player != null ? player.Data.TeamColor : Color.clear;
            if (teamColorsCanvasSortingOrder >= 0 && entityColorsCanvas != null) {
                entityColorsCanvas.sortingOrder = teamColorsCanvasSortingOrder;
            }
        }

        public void SetUpMoveView(GameObject movesRow, TMP_Text movesField, AbilityTimerCooldownView moveTimer, AbilityChannel moveChannel) {
            if (Entity.CanMove) {
                movesRow.SetActive(true);
                movesField.text = $"{Entity.MoveTime}";
                if (GameManager.Instance.AbilityAssignmentManager.IsAbilityChannelOnCooldownForEntity(Entity, moveChannel, out AbilityCooldownTimer activeMoveCooldownTimer)) {
                    moveTimer.gameObject.SetActive(true);
                    moveTimer.Initialize(activeMoveCooldownTimer, false, true);
                } else {
                    moveTimer.gameObject.SetActive(false);
                }
            } else {
                movesRow.SetActive(false);
            }
        }

        public void SetUpAttackView(GameObject attackRow, TMP_Text attackField) {
            if (Entity.EntityData.Damage > 0) {
                attackRow.SetActive(true);
                attackField.text = Entity.Damage.ToString();
            } else {
                attackRow.SetActive(false);
            }
        }
        
        public void SetUpResourceView(GameObject resourceRow, TMP_Text resourceLabel, TMP_Text resourceField) {
            ResourceAmount resourceAmount = null;
            
            // If this entity has starting resources, display for those
            if (Entity.EntityData.StartingResourceSet.Amount > 0) {
                resourceAmount = Entity.CurrentResourcesValue;
            }

            if (resourceAmount == null && Entity.EntityData.IsResourceExtractor) {
                // Find the resource set for the resource provider on this cell
                GridEntityCollection.PositionedGridEntityCollection entitiesAtLocation = GameManager.Instance.GetEntitiesAtLocation(Entity.Location!.Value);
                GridEntity resourceProvider = entitiesAtLocation?.Entities.FirstOrDefault(e => e.Entity.EntityData.StartingResourceSet is { Amount: > 0 })?.Entity;
                if (resourceProvider != null) {
                    resourceAmount = resourceProvider.CurrentResourcesValue;
                }
            }
            
            if (resourceAmount == null) {
                resourceRow.gameObject.SetActive(false);
            } else {
                // There are indeed resources to be shown here
                resourceRow.gameObject.SetActive(true);
                resourceLabel.text = $"Remaining {resourceAmount.Type.DisplayIcon()}:";
                resourceField.text = resourceAmount.Amount.ToString();
            }
        }
        
        public void SetUpBuildQueueView(BuildQueueView buildQueueForStructure, BuildQueueView buildQueueForWorker) {
            if (!Entity.EntityData.CanBuild) return;
            
            if (Entity.EntityData.IsStructure) {
                buildQueueForStructure.SetUpForEntity(Entity);
            } else {
                buildQueueForWorker.SetUpForEntity(Entity);
            }
        }
    }
}