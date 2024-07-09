using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Generic logic for tracking a rally point
    /// </summary>
    public interface IRallyLogic {
        bool CanRally { get; }
        /// <summary>
        /// Values only set on the server!
        /// </summary>
        Vector2Int RallyPoint { get; set; }
    }
}