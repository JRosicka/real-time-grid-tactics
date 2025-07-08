namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// The legality state of an ability
    /// </summary>
    public enum AbilityLegality {
        // The ability is currently legal to perform
        Legal,
        // The ability is not currently legal, but might become legal later
        NotCurrentlyLegal,
        // The ability is not legal and probably won't be anytime soon
        IndefinitelyIllegal
    }
}