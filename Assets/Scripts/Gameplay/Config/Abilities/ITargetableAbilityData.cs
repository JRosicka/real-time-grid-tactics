using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    /// <summary>
    /// Data for an ability that can target a particular cell/entity
    /// </summary>
    public interface ITargetableAbilityData : IAbilityData {
        /// <summary>
        /// Whether we can legally create a new <see cref="IAbility"/> targeting the specified cell. 
        /// </summary>
        bool CanTargetCell(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData);
        /// <summary>
        /// Create a new <see cref="IAbility"/> targeting the specified cell. Assumes that <see cref="CanTargetCell"/> is true.
        /// </summary>
        void DoTargetableAbility(Vector2Int cellPosition, GridEntity selectedEntity, GameTeam selectorTeam, object targetData);
        /// <summary>
        /// Redo any on-selection calculations that need to be updated when the map state changes
        /// </summary>
        void RecalculateTargetableAbilitySelection(GridEntity selector);
        /// <summary>
        /// Whether we should move to the target cell before attempting to do the ability
        /// </summary>
        bool MoveToTargetCellFirst { get; }
        /// <summary>
        /// Instantiate and return a new GameObject to be displayed over the cell being hovered over.
        /// </summary>
        [CanBeNull]
        GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData);
    }
}