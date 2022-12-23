using UnityEngine;

namespace GamePlay.Entities {
    /// <summary>
    /// Handles input for a <see cref="GridEntity"/>
    /// </summary>
    public interface IInteractBehavior {
        /// <summary>
        /// The user clicked to select this unit
        /// </summary>
        void Select(GridEntity entity);

        /// <summary>
        /// The user clicked to target a particular cell when they are selecting this unit.
        /// The target cell may or may not have a <see cref="GridEntity"/> on it
        /// </summary>
        void TargetCellWithUnit(GridEntity thisEntity, Vector2Int targetCell);
    }
}