namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAttackLogic"/> for a generic melee attack
    /// </summary>
    public class MeleeAttackLogic : IAttackLogic {
        public void DoAttack(GridEntity attacker, GridEntity target, int bonusDamage) {
            target.ReceiveAttackFromEntity(attacker, bonusDamage);
        }
    }
}