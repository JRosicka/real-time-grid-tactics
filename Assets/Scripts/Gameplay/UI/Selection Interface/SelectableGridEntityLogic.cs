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
            
            entity.AbilityPerformedEvent += OnEntityAbilityPerformed;
            entity.CooldownTimerExpiredEvent += OnEntityAbilityCooldownExpired;
            entity.CurrentResources.ValueChanged += OnEntityResourceAmountChanged;
            entity.KillCountChanged += KillCountChanged;
            
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(entity.Team);
            if (player != null) {
                player.OwnedPurchasablesController.OwnedPurchasablesChangedEvent += OnOwnedPurchasablesChanged;
            }
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

        private GameObject _movesRow;
        private TMP_Text _movesField;
        private AbilityTimerCooldownView _moveTimer;
        private AbilityChannel _moveChannel;
        public void SetUpMoveView(GameObject movesRow, TMP_Text movesField, AbilityTimerCooldownView moveTimer, AbilityChannel moveChannel) {
            _movesRow = movesRow;
            _movesField = movesField;
            _moveTimer = moveTimer;
            _moveChannel = moveChannel;
            
            if (Entity.CanMove) {
                movesRow.SetActive(true);
                movesField.text = Entity.MoveTime.ToString("N2");
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
        
        private GameObject _resourceRow;
        private TMP_Text _resourceLabel;
        private TMP_Text _resourceField;
        public void SetUpResourceView(GameObject resourceRow, TMP_Text resourceLabel, TMP_Text resourceField) {
            _resourceRow = resourceRow;
            _resourceLabel = resourceLabel;
            _resourceField = resourceField;
            
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
                resourceRow.SetActive(false);
            } else {
                // There are indeed resources to be shown here
                resourceRow.SetActive(true);
                resourceLabel.text = $"Remaining {resourceAmount.Type.DisplayIcon()}:";
                resourceField.text = resourceAmount.Amount.ToString();
            }
        }

        public void SetUpRangeView(GameObject rangeRow, TMP_Text rangeField) {
            if (Entity.EntityData.Range > 0) {
                rangeRow.SetActive(true);
                rangeField.text = Entity.EntityData.Range.ToString();
            } else {
                rangeRow.SetActive(false);
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
        
        private TMP_Text _killCountField;
        public void SetUpKillCountView(GameObject killCountRow, TMP_Text killCountField) {
            _killCountField = killCountField;
            
            bool tracksKills = Entity.EntityData.Damage > 0;
            killCountRow.SetActive(tracksKills);
            if (tracksKills) {
                killCountRow.SetActive(true);
                killCountField.text = Entity.KillCount.ToString();
            } else {
                killCountRow.SetActive(false);
                killCountField.text = string.Empty;
            }
        }

        private HoverableInfoIcon _defenseHoverableInfoIcon;
        private HoverableInfoIcon _attackHoverableInfoIcon;
        public void SetUpHoverableInfo(HoverableInfoIcon defenseHoverableInfoIcon, HoverableInfoIcon attackHoverableInfoIcon, HoverableInfoIcon moveHoverableInfoIcon) {
            _defenseHoverableInfoIcon = defenseHoverableInfoIcon;
            _attackHoverableInfoIcon = attackHoverableInfoIcon;
            
            string defenseTooltip = GetDefenseTooltip();
            if (!string.IsNullOrEmpty(defenseTooltip)) {
                defenseHoverableInfoIcon.ShowIcon(defenseTooltip);
            } else {
                defenseHoverableInfoIcon.HideIcon();
            }
            
            string attackTooltip = GetAttackTooltip();
            if (!string.IsNullOrEmpty(attackTooltip)) {
                attackHoverableInfoIcon.ShowIcon(attackTooltip);
            } else {
                attackHoverableInfoIcon.HideIcon();
            }
        }

        public void UnregisterListeners() {
            Entity.AbilityPerformedEvent -= OnEntityAbilityPerformed;
            Entity.CooldownTimerExpiredEvent -= OnEntityAbilityCooldownExpired;
            Entity.CurrentResources.ValueChanged -= OnEntityResourceAmountChanged;
            Entity.KillCountChanged -= KillCountChanged;
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(Entity.Team);
            if (player != null) {
                player.OwnedPurchasablesController.OwnedPurchasablesChangedEvent -= OnOwnedPurchasablesChanged;
            }
        }
        
        private void OnEntityAbilityPerformed(IAbility iAbility, AbilityCooldownTimer abilityCooldownTimer) {
            SetUpMoveView(_movesRow, _movesField, _moveTimer, _moveChannel);
            SetUpResourceView(_resourceRow, _resourceLabel, _resourceField);
            SetUpHoverableInfo(_defenseHoverableInfoIcon, _attackHoverableInfoIcon, null);
        }

        private void OnEntityAbilityCooldownExpired(IAbility ability, AbilityCooldownTimer abilityCooldownTimer) {
            SetUpMoveView(_movesRow, _movesField, _moveTimer, _moveChannel);
        }

        private void OnEntityResourceAmountChanged(INetworkableFieldValue oldValue, INetworkableFieldValue newValue, object metadata) {
            SetUpResourceView(_resourceRow, _resourceLabel, _resourceField);
        }

        private void OnOwnedPurchasablesChanged() {
            SetUpHoverableInfo(_defenseHoverableInfoIcon, _attackHoverableInfoIcon, null);
        }

        private void KillCountChanged(int newKillCount) {
            if (!_killCountField) return;
            _killCountField.text = newKillCount.ToString();
        }
        
        #region Tooltips
        
        private const string DefenseFormatStructure = "Reduces incoming attack damage by {0} for {1}.";
        private const string DefenseFormatUnit = "Friendly structure reduces incoming attack damage by {0}.";
        private const string DefenseFormatTerrain = "Terrain reduces incoming attack damage by {0}.";
        private string GetDefenseTooltip() {
            int defenseModifier = Entity.GetStructureDefenseModifier();
            if (defenseModifier != 0) {
                // Defense modifier from structure (friendly or itself)
                if (Entity.EntityData.IsStructure) {
                    string entitiesReceivingDefense = "all units";
                    if (Entity.EntityData.SharedUnitDamageTakenModifierTags.Count > 0) {
                        entitiesReceivingDefense = GetStringListForEntityTags(Entity.EntityData.SharedUnitDamageTakenModifierTags);
                    }
                    return string.Format(DefenseFormatStructure, defenseModifier, entitiesReceivingDefense);
                }
            
                return string.Format(DefenseFormatUnit, defenseModifier);
            }
            
            defenseModifier = Entity.GetTerrainDefenseModifier();
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (defenseModifier != 0) {
                // Defense modifier from terrain
                return string.Format(DefenseFormatTerrain, defenseModifier);
            }
            
            return "";
        }
        
        private const string AttackFormat = "Deals {0} additional damage to {1}.";
        private string GetAttackTooltip() {
            string tooltipMessage = Entity.GetAttackTooltipMessageFromAbilities();
            string bonusDamage = Entity.EntityData.BonusDamage == 0 
                ? "" 
                : string.Format(AttackFormat, Entity.EntityData.BonusDamage, GetStringListForEntityTags(Entity.EntityData.TagsToApplyBonusDamageTo));
            if (!string.IsNullOrEmpty(tooltipMessage) && !string.IsNullOrEmpty(bonusDamage)) {
                tooltipMessage += "<br>";
            }
            return tooltipMessage + bonusDamage;
        }

        private static string GetStringListForEntityTags(List<EntityTag> tags) {
            string ret = "";
            for (int i = 0; i < tags.Count; i++) {
                ret += tags[i].UnitDescriptorPlural();
                if (i == tags.Count - 1) {
                    // Nothing to add
                } else if (i == tags.Count - 2) {
                    if (tags.Count > 2) {
                        ret += ", and ";
                    } else {
                        ret += " and ";
                    }
                } else {
                    ret += ", ";
                }
            }

            return ret;
        }
        
        #endregion
    }
}