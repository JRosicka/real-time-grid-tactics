using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    /// <summary>
    /// Data representing some active action that a <see cref="GridEntity"/> can perform in the game.
    /// This determines the behavior of when a player selects the ability, but the matching <see cref="IAbility"/>
    /// handles the actual performing of the ability which is the part that needs to be sent to the server. 
    /// </summary>
    public interface IAbilityData {
        string ContentResourceID { get; set; }
        Sprite Icon { get; }
        List<PurchasableData> Requirements { get; }
        float PerformDuration { get; }
        float CooldownDuration { get; }
        AbilityChannel Channel { get; }
        List<AbilityChannel> ChannelBlockers { get; }
        /// <summary>
        /// Whether to do the ability immediately after the associated <see cref="GridEntity"/> spawns. Note that
        /// <see cref="NullAbilityParameters"/> will be sent as the parameters. 
        /// </summary>
        bool PerformOnStart { get; }
        /// <summary>
        /// Whether this is an active ability that should be selectable via the ability interface or hotkeys
        /// </summary>
        bool Selectable { get; }
        /// <summary>
        /// Whether this ability is always selectable even when the performer entity has an active timer blocking its usage.
        /// Does nothing if <see cref="Selectable"/> is false. 
        /// </summary>
        bool SelectableWhenBlocked { get; }
        /// <summary>
        /// Respond to the user input intending to use this ability. Do not actually perform the ability (unless there is
        /// nothing else to do first) - rather, handle any client-side stuff by sending events to prompt for further input. 
        /// </summary>
        void SelectAbility(GridEntity selector);

        /// <summary>
        /// Whether the ability is legal to be used with the given parameters (valid target, player has enough money, cooldowns, etc).
        ///
        /// Should be checked on the client before creating the ability, and checked again on the server before performing
        /// the ability. If the server check fails, let the client know. 
        /// </summary>
        bool AbilityLegal(IAbilityParameters parameters, GridEntity entity);
        
        /// <summary>
        /// Create an instance of this ability, passing in any user input. This created instance should be passed to the
        /// server.
        /// </summary>
        IAbility CreateAbility(IAbilityParameters parameters, GridEntity performer);

        /// <summary>
        /// Re-creates the <see cref="IAbility"/> by first deserializing the parameters from the provided reader
        /// </summary>
        IAbility DeserializeAbility(NetworkReader reader);
    }
}