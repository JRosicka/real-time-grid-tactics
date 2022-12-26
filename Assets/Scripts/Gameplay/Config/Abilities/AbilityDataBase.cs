using System;
using System.Collections.Generic;
using GamePlay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    /// <summary>
    /// Configuration for an <see cref="IAbility"/> - some active action that a <see cref="GridEntity"/> can perform in the game.
    /// This class determines the behavior of when a player selects the ability, but the matching <see cref="IAbility"/>
    /// handles the actual performing of the ability which is the part that needs to be sent to the server. 
    /// </summary>
    [Serializable]
    public abstract class AbilityDataBase<T> : IAbilityData where T : IAbilityParameters, new() {
        /// <summary>
        /// All of these must be owned in order to perform the ability
        /// </summary>
        public List<PurchasableData> Requirements;
        public float PerformDuration;
        public float CooldownDuration;

        /// <summary>
        /// This gets set automatically in an editor script to match the name of the <see cref="BaseAbilityDataAsset{T,P}"/>
        /// where this comes from. Necessary for writing/reading over network. 
        /// </summary>
        public string ContentResourceID { get; set; }
        
        /// <summary>
        /// Respond to the user input intending to use this ability. Do not actually perform the ability (unless there is
        /// nothing else to do first) - rather, handle any client-side stuff by sending events to prompt for further input. 
        /// </summary>
        public abstract void SelectAbility(GridEntity selector);

        /// <summary>
        /// Create an instance of this ability, passing in any user input. This created instance should be passed to the
        /// server. 
        /// </summary>
        protected abstract IAbility CreateAbilityImpl(T parameters);
        
        /// <summary>
        /// Create an instance of this ability, passing in any user input. This created instance should be passed to the
        /// server. 
        /// </summary>
        public IAbility CreateAbility(IAbilityParameters parameters) => CreateAbilityImpl((T) parameters);

        /// <summary>
        /// Re-creates the <see cref="IAbility"/> by first deserializing the parameters from the provided reader
        /// </summary>
        public IAbility DeserializeAbility(NetworkReader reader) {
            T parameters = new T();
            parameters.Deserialize(reader);
            return CreateAbilityImpl(parameters);
        }
    }
}