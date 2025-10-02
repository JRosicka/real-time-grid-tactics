namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// The result of performing an ability. This result informs what <see cref="AbilityExecutor"/> does after trying
    /// to perform the ability. 
    /// </summary>
    public enum AbilityResult {
        // The ability is complete and should be removed, but the effect did not happen
        CompletedWithoutEffect,
        // The ability is complete and should be removed, and the effect occurred (clients should show effect)
        CompletedWithEffect,
        // The ability is not yet complete, but the effect just happened (clients should show effect)
        IncompleteWithEffect,
        // The ability is not yet complete, and the effect did not just happen
        IncompleteWithoutEffect,
        // The ability failed to complete and should be removed
        Failed
    }
}