using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities.DeathAction;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Manages state and updates for a <see cref="GridEntity"/>'s HP. Handles incoming attacks and heals, and triggers
    /// death.  
    /// </summary>
    public class GridEntityHPHandler : NetworkBehaviour {
        public event Action HPChangedEvent;
        // Parameters are:
        // - True if lethal damage is received, otherwise false
        // - True if the attack is disabling, otherwise false
        public event Action<bool, bool> AttackedEvent;
        public event Action<bool> HealedEvent;
        
        [SerializeField] private GridEntity _gridEntity;
        private EntityData EntityData => _gridEntity.EntityData;

        // Server flag?
        public bool MarkedForDeath { get; private set; }

        public int CurrentHP => ((NetworkableIntegerValue)_currentHP?.Value)?.Value ?? 0;
        private float LastDisablingAttackedTime => ((NetworkableFloatValue)_lastDisablingAttackedTime?.Value)?.Value ?? 0;
        
        private NetworkableField _currentHP;
        private NetworkableField _lastDisablingAttackedTime;

        private void Awake() {
            _currentHP = new NetworkableField(this, nameof(_currentHP), new NetworkableIntegerValue(0));
            _currentHP.ValueChanged += HPChanged;
            
            _lastDisablingAttackedTime = new NetworkableField(this, nameof(_lastDisablingAttackedTime), new NetworkableFloatValue(-1000));
        }
        
        #region Update HP
        
        [Serializable]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private struct HPChangedMetadata {
            public bool fromGameEffect;
            public bool disabling;
        }
        
        public void SetCurrentHP(int newHP, bool fromGameEffect, bool disabling) {
            string metadata = JsonUtility.ToJson(new HPChangedMetadata {
                fromGameEffect = fromGameEffect,
                disabling = disabling
            });
            
            _currentHP.UpdateValue(new NetworkableIntegerValue(newHP), metadata);
        }

        private void HPChanged(INetworkableFieldValue oldHP, INetworkableFieldValue newHP, string metadata) {
            NetworkableIntegerValue oldHPInt = (NetworkableIntegerValue)oldHP;
            NetworkableIntegerValue newHPInt = (NetworkableIntegerValue)newHP;
            HPChangedEvent?.Invoke();
            
            HPChangedMetadata metadataStruct = JsonUtility.FromJson<HPChangedMetadata>(metadata);
            if (metadataStruct.fromGameEffect && oldHPInt.Value < newHPInt.Value) {
                HealedEvent?.Invoke(newHPInt.Value == _gridEntity.MaxHP);
            } else if (metadataStruct.fromGameEffect && oldHPInt.Value > newHPInt.Value) {
                AttackedEvent?.Invoke(newHPInt.Value <= 0, metadataStruct.disabling);
            }
        }
        
        #endregion
        
        /// <returns>
        /// Deal damage. Server only. 
        /// True if this results in the entity dying, otherwise false
        /// </returns>
        public bool ReceiveAttackFromEntity([JetBrains.Annotations.NotNull] GridEntity sourceEntity, int bonusDamage) {
            // Only non-delicate units' attacks disable
            bool disabling = !sourceEntity.EntityData.Tags.Contains(EntityTag.Delicate);
            if (disabling) {
                _lastDisablingAttackedTime.UpdateValue(new NetworkableFloatValue(Time.time));
            }

            // Get base damage
            int damage = sourceEntity.Damage;
            
            // Apply bonus damange
            damage += bonusDamage;
            
            // Apply any additive attack modifiers based on tags
            damage += sourceEntity.EntityData.TagsToApplyBonusDamageTo.Any(t => EntityData.Tags.Contains(t))
                ? sourceEntity.EntityData.BonusDamage
                : 0;
            
            // Apply any defense modifiers from terrain
            damage -= _gridEntity.GetTerrainDefenseModifier();

            // Apply any defense modifiers from structures (as long as this is not a structure)
            if (!EntityData.IsStructure) {
                damage -= _gridEntity.GetStructureDefenseModifier();
            }

            damage -= _gridEntity.Armor;
            
            // Minimum of 1 damage
            damage = Mathf.Max(1, damage);
            
            SetCurrentHP(CurrentHP - Mathf.RoundToInt(damage), true, disabling);

            if (CurrentHP <= 0) {
                Kill(sourceEntity);
                return true;
            }

            return false;
        }
        
        public void Heal(int healAmount) {
            if (CurrentHP == _gridEntity.MaxHP) return;

            int newHP = CurrentHP + healAmount;
            newHP = Mathf.Min(newHP, _gridEntity.MaxHP);
            SetCurrentHP(newHP, true, false);
        }

        /// <summary>
        /// Returns the amount of seconds since the last attack that could "disable" this unit (i.e. disable income collection)
        /// </summary>
        public float TimeSinceLastReceivedDisablingAttack() {
            return Time.time - LastDisablingAttackedTime;
        }
        
        public void Kill([JetBrains.Annotations.NotNull] GridEntity sourceEntity) {
            if (MarkedForDeath) return;
            MarkedForDeath = true;
            
            foreach (IDeathAction deathAction in _gridEntity.DeathActions) {
                deathAction.DoDeathAction(_gridEntity, sourceEntity);
            }
            GameManager.Instance.CommandManager.AbilityExecutor.MarkForUnRegistration(_gridEntity, true, false);
        }
    }
}