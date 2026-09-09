using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Operations that update the <see cref="GridEntityCollection"/>
    /// </summary>
    public enum GridEntityCollectionUpdate {
        Register,
        Unregister,
        Move
    }
    
    public delegate void EntityUpdatedDelegate(GridEntity entity, GridEntityCollectionUpdate updateType, Vector2Int previousLocation, Vector2Int newLocation);
}