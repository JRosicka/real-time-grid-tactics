using System.Threading.Tasks;
using Gameplay.Config.Abilities;
using Util;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Represents a timer for tracking particular <see cref="AbilityChannel"/> durations - can be checked to see if
    /// <see cref="IAbility"/>s of those same channel are allowed to be used or if the ability effect should happen yet. 
    /// </summary>
    public class AbilityTimer : NetworkableTimer {
        /// <summary>
        /// The amount of time to wait between attempts to complete the post-completion step after the timer elapses
        /// </summary>
        private const int CooldownCheckMillis = 200;

        public readonly IAbility Ability;
        
        public AbilityTimer(IAbility ability, float overrideCooldownDuration) 
                : base(ability.PerformerTeam, overrideCooldownDuration > 0 ? overrideCooldownDuration : ability.CooldownDuration) {
            Ability = ability;
        }
        
        public bool DoesBlockChannelForTeam(AbilityChannel channel, GameTeam team) {
            if (team != Team) return false;
            if (Expired) return false;
            if (!Ability.AbilityData.ChannelBlockers.Contains(channel)) return false;
            
            return true;
        }

        protected override async Task TryCompleteTimerAsync() {
            await AsyncUtil.WaitUntilOnCallerThread(Ability.CompleteCooldown, CooldownCheckMillis);
            if (GameManager.Instance == null) return;
            GameManager.Instance.CommandManager.MarkAbilityTimerExpired(Ability);
        }
    }
}