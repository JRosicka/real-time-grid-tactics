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
    /// Represents something that can be selected and observed in the <see cref="SelectionInterface"/>
    /// </summary>
    public interface ISelectableObjectLogic {
        [CanBeNull] GridEntity Entity { get; }
        [CanBeNull] IBuildQueue BuildQueue { get; }
        string Name { get; }
        string ShortDescription { get; }
        string LongDescription { get; }
        string Tags { get; }
        bool DisplayHP { get; }
        [CanBeNull] BuildAbility InProgressBuild { get; }
        Color TeamBannerColor { get; }
        
        void SetUpIcons(Image entityIcon, Image entityColorsIcon);
        void SetUpMoveView(GameObject movesRow, TMP_Text movesField);
        void SetUpAttackView(GameObject attackRow, TMP_Text attackField);
        void SetUpResourceView(GameObject resourceRow, TMP_Text resourceLabel, TMP_Text resourceField);
        void SetUpRangeView(GameObject rangeRow, TMP_Text rangeField);
        void SetUpBuildQueueView(BuildQueueView buildQueueForStructure, BuildQueueView buildQueueForWorker);
        void SetUpKillCountView(GameObject killCountRow, TMP_Text killCountField);
        void SetUpHoverableInfo(HoverableInfoIcon defenseHoverableInfoIcon, HoverableInfoIcon attackHoverableInfoIcon, HoverableInfoIcon moveHoverableInfoIcon);

        void UnregisterListeners();
    }
}