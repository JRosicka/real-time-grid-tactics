using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.BuildQueue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Selectable <see cref="GameplayTile"/> info for display in the <see cref="SelectionInterface"/>
    /// </summary>
    public class SelectableGameplayTileLogic : ISelectableObjectLogic {
        private readonly GameplayTile _tile;
        
        public SelectableGameplayTileLogic(GameplayTile tile) {
            _tile = tile;
        }

        public GridEntity Entity => null;
        public IBuildQueue BuildQueue => null;
        public string Name => _tile.DisplayName;
        public string ShortDescription => _tile.ShortDescription;
        public string LongDescription => _tile.LongDescription;
        public string Tags => string.Empty;
        public bool DisplayHP => false;
        public BuildAbility InProgressBuild => null;
        public Color TeamBannerColor => GameManager.Instance.Configuration.NeutralBannerColor;
        
        public void SetUpIcons(Image entityIcon, Image entityColorsIcon) {
            entityIcon.sprite = _tile.m_DefaultSprite;
            entityColorsIcon.gameObject.SetActive(false);
        }

        public void SetUpMoveView(GameObject movesRow, TMP_Text movesField) {
            movesRow.SetActive(false);
            movesField.text = string.Empty;
        }

        public void SetUpAttackView(GameObject attackRow, TMP_Text attackField) {
            attackRow.SetActive(false);
        }
        public void SetUpResourceView(GameObject resourceRow, TMP_Text resourceLabel, TMP_Text resourceField) {
            resourceRow.SetActive(false);
        }

        public void SetUpRangeView(GameObject rangeRow, TMP_Text rangeField) {
            rangeRow.SetActive(false);
        }

        public void SetUpBuildQueueView(BuildQueueView buildQueueForStructure, BuildQueueView buildQueueForWorker) {
            // Nothing to do
        }

        public void SetUpKillCountView(GameObject killCountRow, TMP_Text killCountField) {
            killCountRow.SetActive(false);
            killCountField.text = string.Empty;
        }

        public void SetUpHoverableInfo(HoverableInfoIcon defenseHoverableInfoIcon, HoverableInfoIcon attackHoverableInfoIcon, HoverableInfoIcon moveHoverableInfoIcon) {
            string defenseTooltip = _tile.GetDefenseTooltip();
            if (!string.IsNullOrEmpty(defenseTooltip)) {
                defenseHoverableInfoIcon.ShowIcon(defenseTooltip);
            }

            string movementTooltip = _tile.GetMovementTooltip();
            if (!string.IsNullOrEmpty(movementTooltip)) {
                moveHoverableInfoIcon.ShowIcon(movementTooltip);
            }
        }

        public void UnregisterListeners() {
            // Nothing to do
        }
    }
}