using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// <see cref="IRallyLogic"/> for an entity that can not rally
    /// </summary>
    public class NullRallyLogic : IRallyLogic {
        public bool CanRally => false;
        public Vector2Int RallyPoint { get => Vector2Int.zero; set { } }
    }
}