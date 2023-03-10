using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    /// <summary>
    /// Data for an ability that can target a particular cell/entity
    /// </summary>
    public interface ITargetableAbilityData : IAbilityData {
        /// <summary>
        /// Whether we can legally create a new <see cref="IAbility"/> targeting the specified cell. 
        /// </summary>
        bool CanTargetCell(Vector2Int cellPosition, GridEntity entity, GridEntity.Team selectorTeam);
        /// <summary>
        /// Create a new <see cref="IAbility"/> targeting the specified cell. Assumes that <see cref="CanTargetCell"/> is true.
        /// </summary>
        void DoTargetableAbility(Vector2Int cellPosition, GridEntity entity, GridEntity.Team selectorTeam);
    }
}