namespace Gameplay.Entities.DeathAction {
    /// <summary>
    /// Business logic for some action to be performed on the server when a <see cref="GridEntity"/> dies.
    /// Configured via <see cref="IDeathAction"/>.
    /// No-op on clients. 
    /// </summary>
    public interface IDeathAction {
        void DoDeathAction(GridEntity dyingEntity, GridEntity attacker);
    }
}