using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.BuildQueue;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
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
            entity.CurrentResources.ValueChanged += OnEntityResourceAmountChanged;
            entity.KillCountChanged += KillCountChanged;
            entity.IncomeRateChanged += IncomeRateChanged;
            
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(entity);
            if (player != null) {
                player.OwnedPurchasablesController.OwnedPurchasablesChangedEvent += OnOwnedPurchasablesChanged;
            }

            GameManager.Instance.LeaderTracker.LeaderMoved += OnLeaderMoved;
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
                List<BuildAbility> buildQueue = BuildQueue.Queue(GameManager.Instance.LocalTeam);
                if (!Entity.EntityData.IsStructure && buildQueue.Count > 0 &&
                    !Entity.InProgressAbilities.Select(a => a.UID).Contains(buildQueue[0].UID)) {
                    return buildQueue[0];
                }

                return null;
            }
        }

        public Color TeamBannerColor {
            get {
                if (Entity.Team == GameTeam.Neutral) {
                    return GameManager.Instance.Configuration.NeutralBannerColor;
                }
                
                IGamePlayer player = GameManager.Instance.GetPlayerForTeam(Entity);
                return player.Data.TeamBannerColor;
            }
        }

        public void SetUpIcons(Image entityIcon, Image entityColorsIcon) {
            EntityData entityData = Entity.EntityData;
            entityIcon.sprite = entityData.BaseSpriteIconOverride == null ? entityData.BaseSprite : entityData.BaseSpriteIconOverride;
            entityColorsIcon.sprite = entityData.TeamColorSprite;
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(Entity);
            entityColorsIcon.gameObject.SetActive(true);
            entityColorsIcon.color = player != null 
                ? entityData.TeamColorSprite
                    ? player.Data.TeamColor 
                    : Color.clear
                : Color.clear;
        }

        public void SetUpMoveView(GameObject movesRow, TMP_Text movesField) {
            if (Entity.CanMove) {
                movesRow.SetActive(true);
                movesField.text = $"{Entity.MoveTime:N2}s";
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
                GridEntity resourceProvider = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(Entity, Entity.EntityData);
                if (resourceProvider != null) {
                    resourceAmount = resourceProvider.CurrentResourcesValue;
                }
            }
            
            if (resourceAmount == null) {
                resourceRow.SetActive(false);
            } else {
                // There are indeed resources to be shown here
                resourceRow.SetActive(true);
                resourceLabel.text = "Resources:";
                resourceField.text = $"{resourceAmount.Amount} {resourceAmount.Type.DisplayIcon()}";
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
            if (!Entity.EntityData.CanBuild || Entity.EntityData.NeverShowBuildQueue) return;
            
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
            if (tracksKills) {
                killCountRow.SetActive(true);
                killCountField.text = Entity.KillCount.ToString();
            } else {
                killCountRow.SetActive(false);
                killCountField.text = string.Empty;
            }
        }

        private TMP_Text _incomeRateField;
        public void SetUpIncomeRateView(GameObject incomeRateRow, TMP_Text incomeRateField) {
            _incomeRateField = incomeRateField;
            
            bool tracksIncomeRate = Entity.IncomeRate > 0;
            if (tracksIncomeRate) {
                incomeRateRow.SetActive(true);
                incomeRateField.text = Entity.IncomeRate.ToString();
            } else  {
                incomeRateRow.SetActive(false);
                incomeRateField.text = string.Empty;
            }
        }

        private HoverableInfoIcon _defenseHoverableInfoIcon;
        private HoverableInfoIcon _attackHoverableInfoIcon;
        private HoverableInfoIcon _moveHoverableInfoIcon;
        public void SetUpHoverableInfo(HoverableInfoIcon defenseHoverableInfoIcon, HoverableInfoIcon attackHoverableInfoIcon, HoverableInfoIcon moveHoverableInfoIcon) {
            _defenseHoverableInfoIcon = defenseHoverableInfoIcon;
            _attackHoverableInfoIcon = attackHoverableInfoIcon;
            _moveHoverableInfoIcon = moveHoverableInfoIcon;
            
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

            string moveTooltip = GetMoveTooltip();
            if (!string.IsNullOrEmpty(moveTooltip)) {
                moveHoverableInfoIcon.ShowIcon(moveTooltip);
            } else {
                moveHoverableInfoIcon.HideIcon();
            }
        }

        public void UnregisterListeners() {
            Entity.AbilityPerformedEvent -= OnEntityAbilityPerformed;
            Entity.CurrentResources.ValueChanged -= OnEntityResourceAmountChanged;
            Entity.KillCountChanged -= KillCountChanged;
            Entity.IncomeRateChanged -= IncomeRateChanged;
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(Entity);
            if (player != null) {
                player.OwnedPurchasablesController.OwnedPurchasablesChangedEvent -= OnOwnedPurchasablesChanged;
            }
            GameManager.Instance.LeaderTracker.LeaderMoved -= OnLeaderMoved;
        }
        
        private void OnEntityAbilityPerformed(IAbility iAbility, AbilityTimer abilityTimer) {
            SetUpResourceView(_resourceRow, _resourceLabel, _resourceField);
            SetUpHoverableInfo(_defenseHoverableInfoIcon, _attackHoverableInfoIcon, _moveHoverableInfoIcon);
        }
        
        private void OnEntityResourceAmountChanged(INetworkableFieldValue oldValue, INetworkableFieldValue newValue, object metadata) {
            SetUpResourceView(_resourceRow, _resourceLabel, _resourceField);
        }

        private void OnOwnedPurchasablesChanged() {
            SetUpHoverableInfo(_defenseHoverableInfoIcon, _attackHoverableInfoIcon, _moveHoverableInfoIcon);
        }

        private void OnLeaderMoved(GridEntity leader) {
            if (leader.Team != Entity.Team) return;
            SetUpHoverableInfo(_defenseHoverableInfoIcon, _attackHoverableInfoIcon, _moveHoverableInfoIcon);
        }

        private void KillCountChanged(int newKillCount) {
            if (!_killCountField) return;
            _killCountField.text = newKillCount.ToString();
        }

        private void IncomeRateChanged(int newIncomeRate) {
            if (!_incomeRateField) return;
            _incomeRateField.text = newIncomeRate.ToString();
        }

        #region Tooltips
        
        private const string DefenseFormatStructure = "{0} occupying this structure receive {1} less damage from attacks.";
        private const string DefenseFormatUnit = "Receives {0} less damage from attacks due to friendly structure.";
        private const string DefenseFormatTerrain = "Receives {0} less damage from attacks due to terrain.";
        private string GetDefenseTooltip() {
            int defenseModifier = Entity.GetStructureDefenseModifier();
            if (defenseModifier != 0) {
                // Defense modifier from structure (friendly or itself)
                if (Entity.EntityData.IsStructure) {
                    string entitiesReceivingDefense = "all units";
                    if (Entity.EntityData.SharedUnitDamageTakenModifierTags.Count > 0) {
                        entitiesReceivingDefense = GetStringListForEntityTags(Entity.EntityData.SharedUnitDamageTakenModifierTags);
                    }
                    return string.Format(DefenseFormatStructure, entitiesReceivingDefense, defenseModifier).FirstCharacterToUpper();
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
            tooltipMessage = Entity.GetAttackTooltipMessageFromUpgrades(tooltipMessage);
            string bonusDamage = Entity.EntityData.BonusDamage == 0 
                ? "" 
                : string.Format(AttackFormat, Entity.EntityData.BonusDamage, GetStringListForEntityTags(Entity.EntityData.TagsToApplyBonusDamageTo));
            if (!string.IsNullOrEmpty(tooltipMessage) && !string.IsNullOrEmpty(bonusDamage)) {
                tooltipMessage += "<br>";
            }
            return tooltipMessage + bonusDamage;
        }

        private string GetMoveTooltip() {
            string tooltipMessage = Entity.GetMoveTooltipMessageFromUpgrades();
            return tooltipMessage;
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