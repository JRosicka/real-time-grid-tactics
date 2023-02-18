using System;
using System.Collections.Generic;
using Gameplay.Entities;
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
        [SerializeField] [HideInInspector]
        private string _contentResourceID;
        /// <summary>
        /// This gets set automatically in an editor script to match the name of the <see cref="BaseAbilityDataAsset{T,P}"/>
        /// where this comes from. Necessary for writing/reading over network. 
        /// </summary>
        public string ContentResourceID { get => _contentResourceID; set => _contentResourceID = value; }

        [SerializeField] 
        private Sprite _icon;
        public Sprite Icon => _icon;

        /// <summary>
        /// All of these must be owned in order to perform the ability
        /// </summary>
        [SerializeField]
        private List<PurchasableData> _requirements;
        public List<PurchasableData> Requirements => _requirements;
        
        [SerializeField]
        private float _performDuration;
        public float PerformDuration => _performDuration;
        
        [SerializeField]
        private float _cooldownDuration;
        public float CooldownDuration => _cooldownDuration;

        [SerializeField]
        private AbilityChannel _channel;
        public AbilityChannel Channel => _channel;
        
        /// <summary>
        /// Collection of any <see cref="AbilityChannel"/>s that usage of this ability blocks. 
        /// </summary>
        [SerializeField]
        private List<AbilityChannel> _channelBlockers = new List<AbilityChannel>();
        public List<AbilityChannel> ChannelBlockers => _channelBlockers;

        [SerializeField] 
        private bool _performOnStart;
        public bool PerformOnStart => _performOnStart;
        [SerializeField]
        private bool _repeatWhenCooldownFinishes;
        public bool RepeatWhenCooldownFinishes => _repeatWhenCooldownFinishes;

        [SerializeField] 
        private bool _selectable;
        public bool Selectable => _selectable;

        /// <summary>
        /// Respond to the user input intending to use this ability. Do not actually perform the ability (unless there is
        /// nothing else to do first) - rather, handle any client-side stuff by sending events to prompt for further input.
        ///
        /// This method can assume that the selector entity can legally select the ability (no cooldown, etc)
        /// </summary>
        public abstract void SelectAbility(GridEntity selector);
        
        /// <summary>
        /// Whether the ability is legal to be used with the given parameters (valid target, player has enough money, cooldowns, etc).
        ///
        /// Should be checked on the client before creating the ability, and checked again on the server before performing
        /// the ability. If the server check fails, let the client know. 
        /// </summary>
        public bool AbilityLegal(IAbilityParameters parameters, GridEntity entity) {
            return entity.CanUseAbility(this) && AbilityLegalImpl((T) parameters, entity);
        }
        
        protected abstract bool AbilityLegalImpl(T parameters, GridEntity entity);

        /// <summary>
        /// Create an instance of this ability, passing in any user input. This created instance should be passed to the
        /// server. 
        /// </summary>
        public IAbility CreateAbility(IAbilityParameters parameters, GridEntity performer) => CreateAbilityImpl((T) parameters, performer);
        
        /// <summary>
        /// Create an instance of this ability, passing in any user input. This created instance should be passed to the
        /// server. 
        /// </summary>
        protected abstract IAbility CreateAbilityImpl(T parameters, GridEntity performer);

        /// <summary>
        /// Re-creates the <see cref="IAbility"/> by first deserializing the parameters from the provided reader
        /// </summary>
        public IAbility DeserializeAbility(NetworkReader reader) {
            GridEntity performer = reader.Read<GridEntity>();
            T parameters = new T();
            
            parameters.Deserialize(reader);
            IAbility ability = CreateAbilityImpl(parameters, performer);
            ability.DeserializeImpl(reader);
            
            return ability;
        }
    }
}