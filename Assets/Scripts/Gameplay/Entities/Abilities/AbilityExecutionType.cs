namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Categorization for helping <see cref="AbilityQueueExecutor"/> determine the order that abilities should be executed
    /// </summary>
    public enum AbilityExecutionType {
        DuringGridUpdates = 0,
        AfterGridUpdates = 1,
    }
}