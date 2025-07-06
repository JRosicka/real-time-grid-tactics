namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Categorization for helping <see cref="AbilityQueueExecutor"/> determine the order that abilities should be executed
    /// </summary>
    public enum AbilityExecutionType {
        PreInteractionGridUpdate = 0,
        Interaction = 1,
        PostInteractionGridUpdate = 2,
    }
}