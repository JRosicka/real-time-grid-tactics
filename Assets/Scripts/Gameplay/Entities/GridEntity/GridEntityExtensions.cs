namespace Gameplay.Entities {
    public static class GridEntityExtensions {
        /// <summary>
        /// Whether the entity is alive, registered, and on the grid.
        /// Dead, dying, and carried entities are considered inactive. 
        /// </summary>
        /// <returns></returns>
        public static bool IsActive(this GridEntity entity) {
            if (!entity) return false;
            if (!entity.Registered) return false;
            if (entity.DeadOrDying) return false;
            if (entity.Location == null) return false;

            return true;
        }
    }
}