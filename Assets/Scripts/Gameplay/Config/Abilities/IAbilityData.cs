using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.UI;
using Mirror;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    /// <summary>
    /// Data representing some active action that a <see cref="GridEntity"/> can perform in the game.
    /// This determines the behavior of when a player selects the ability, but the matching <see cref="IAbility"/>
    /// handles the actual performing of the ability which is the part that needs to be sent to the server. 
    /// </summary>
    public interface IAbilityData {
        AbilitySlotInfo AbilitySlotInfo { get; }
        string ContentResourceID { get; set; }
        Sprite Icon { get; }
        Sprite SlotSprite { get; }
        AbilityTimerCooldownView AbilityTimerCooldownViewPrefab { get; }
        List<PurchasableData> Requirements { get; }
        float PerformDuration { get; }
        float CooldownDuration { get; }
        AbilityChannel Channel { get; }
        List<AbilityChannel> ChannelBlockers { get; }
        AbilitySlotLocation SlotLocation { get; }
        /// <summary>
        /// Whether to do the ability immediately after the associated <see cref="GridEntity"/> spawns. Note that
        /// <see cref="OnStartParameters"/> will be sent as the parameters. 
        /// </summary>
        bool PerformOnStart { get; }
        IAbilityParameters OnStartParameters { get; }
        /// <summary>
        /// Whether this ability can ever be canceled.
        /// </summary>
        bool CanBeCanceled { get; }
        /// <summary>
        /// Whether this ability can be canceled while there is an active timer. 
        /// </summary>
        bool CancelableWhileOnCooldown { get; }
        /// <summary>
        /// Whether this ability can be canceled while in-progress. 
        /// </summary>
        bool CancelableWhileInProgress { get; }
        /// <summary>
        /// Whether this ability can be canceled manually (i.e. by user input). 
        /// </summary>
        bool CancelableManually { get; }
        /// <summary>
        /// Whether to perform view logic when the cooldown is complete instead of when the ability is triggered 
        /// </summary>
        bool AnimateWhenCooldownComplete { get; }
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
        /// Whether this ability is automatically selected (but not performed) when selecting the performing entity.
        /// Does nothing if <see cref="Selectable"/> is false. 
        /// </summary>
        bool AutoSelect { get; }
        /// <summary>
        /// Whether we should do the on-select ability effect when selected by any player, not just by the owner
        /// </summary>
        bool SelectableForAllPlayers { get; }
        /// <summary>
        /// Whether this ability requires a cell to be targeted when using it
        /// </summary>
        bool Targeted { get; }
        /// <summary>
        /// Whether this ability should cancel all of this entity's active or queued builds at the moment that the player
        /// tries to issue the ability command
        /// </summary>
        bool TryingToPerformCancelsBuilds { get; }
        /// <summary>
        /// Whether we should show a visual for the ability cooldown timer on the selection interface's <see cref="AbilitySlot"/>
        /// for this ability. 
        /// </summary>
        bool ShowTimerOnSelectionInterface { get; }
        string GetAttackTooltipMessage(GameTeam team);
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
        AbilityLegality AbilityLegal(IAbilityParameters parameters, GridEntity entity, bool ignoreBlockingTimers);
        
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