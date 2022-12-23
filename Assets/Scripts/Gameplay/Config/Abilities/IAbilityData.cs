using GamePlay.Entities;
using Gameplay.Entities.Abilities;

namespace Gameplay.Config.Abilities {
    /// <summary>
    /// Data representing some active action that a <see cref="GridEntity"/> can perform in the game.
    /// This determines the behavior of when a player selects the ability, but the matching <see cref="IAbility"/>
    /// handles the actual performing of the ability which is the part that needs to be sent to the server. 
    /// </summary>
    public interface IAbilityData {
        string ContentResourceID { get; set; }
        /// <summary>
        /// Respond to the user input intending to use this ability. Do not actually perform the ability (unless there is
        /// nothing else to do first) - rather, handle any client-side stuff by sending events to prompt for further input. 
        /// </summary>
        void SelectAbility(GridEntity selector);

        /// <summary>
        /// Create an instance of this ability, passing in any user input. This created instance should be passed to the
        /// server.
        /// 
        /// TODO should add another method to check to see if the ability is legal (valid target, player has enough money, cooldowns, etc),
        /// and check that before creating the ability. Then that should be checked AGAIN on the server before performing the ability since
        /// things might have changed. Then the IAbility itself should have a method to handle paying the cost which is called at the beginning
        /// of IAbility.PerformAbility(). And if the server check for viability fails, we do an RPC call targeting the client who tried to
        /// do the ability so that the client knows the ability was canceled. Ye. 
        /// </summary>
        IAbility CreateAbility(IAbilityParameters parameters);
    }
}