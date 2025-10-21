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
        void RecalculateTargetableAbilitySelection(GridEntity selector, object targetData);
        /// <summary>
        /// Perform any ability selection logic in response to the player hovering the mouse over a different cell
        /// </summary>
        void UpdateHoveredCell(GridEntity selector, Vector2Int? cell);
        /// <summary>
        /// Triggered when the owned purchasables collection for the local player has been updated
        /// </summary>
        void OwnedPurchasablesChanged(GridEntity selector);
        /// <summary>
        /// The targetable ability was just deselected. Occurs both when being selected due to the ability being performed
        /// and when being deselected via canceling. 
        /// </summary>
        void Deselect();
        /// <summary>
        /// Whether we should move to the target cell before attempting to do the ability
        /// </summary>
        bool MoveToTargetCellFirst { get; }
        /// <summary>
        /// Instantiate and return a new GameObject to be displayed over the cell being hovered over.
        /// </summary>
        [CanBeNull]
        GameObject CreateIconForTargetedCell(GameTeam selectorTeam, object targetData);
        string AbilityVerb { get; }
        bool ShowIconOnGridWhenSelected { get; }
    }
}