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
        public void SetUpIcons(Image entityIcon, Image entityColorsIcon, Canvas entityColorsCanvas, int teamColorsCanvasSortingOrder) {
            entityIcon.sprite = _tile.m_DefaultSprite;
            entityColorsIcon.gameObject.SetActive(false);
        }

        public void SetUpMoveView(GameObject movesRow, TMP_Text movesField, AbilityTimerCooldownView moveTimer, AbilityChannel moveChannel) {
            movesRow.SetActive(false);
            movesField.text = string.Empty;
            moveTimer.gameObject.SetActive(false);
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
    }
}