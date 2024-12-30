using Gameplay.UI;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Handles input for a <see cref="GridEntity"/>
    /// </summary>
    public interface IInteractBehavior {
        /// <summary>
        /// Whether this entity belongs to the local player
        /// </summary>
        bool IsLocalTeam { get; }
        
        /// <summary>
        /// Whether we should show the path to the entity's target location
        /// </summary>
        bool AllowedToSeeTargetLocation { get; }

        /// <summary>
        /// Whether we should show queued builds for this entity
        /// </summary>
        bool AllowedToSeeQueuedBuilds { get; }

        /// <summary>
        /// The type of reticle selection for this entity
        /// </summary>
        SelectionReticle.ReticleSelection ReticleSelection { get; }
        
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