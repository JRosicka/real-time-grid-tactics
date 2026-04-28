using System;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities.DeathAction;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Manages state and updates for a <see cref="GridEntity"/>'s HP. Handles incoming attacks and heals, and triggers
    /// death.  
    /// </summary>
    public class GridEntityHPHandler : NetworkBehaviour {
        public event Action HPChangedEvent;
        // Parameter is true if lethal damage is received, otherwise false
        public event Action<bool> AttackedEvent;
        public event Action HealedEvent;
        
        [SerializeField] private GridEntity _gridEntity;
        private EntityData EntityData => _gridEntity.EntityData;

        // Server flag?
        public bool MarkedForDeath { get; private set; }

        public int CurrentHP => ((NetworkableIntegerValue)_currentHP?.Value)?.Value ?? 0;
        private float LastAttackedTime => ((NetworkableFloatValue)_lastAttackedTime?.Value)?.Value ?? 0;
        
        private NetworkableField _currentHP;
        private NetworkableField _lastAttackedTime;

        private void Awake() {
            _currentHP = new NetworkableField(this, nameof(_currentHP), new NetworkableIntegerValue(0));
            _currentHP.ValueChanged += HPChanged;
            
            _lastAttackedTime = new NetworkableField(this, nameof(_lastAttackedTime), new NetworkableFloatValue(-1000));
        }
        
        #region Update HP
        
        public void SetCurrentHP(int newHP, bool fromGameEffect) {
            _currentHP.UpdateValue(new NetworkableIntegerValue(newHP), fromGameEffect.ToString());
        }

        private void HPChanged(INetworkableFieldValue oldHP, INetworkableFieldValue newHP, string metadata) {
            NetworkableIntegerValue oldHPInt = (NetworkableIntegerValue)oldHP;
            NetworkableIntegerValue newHPInt = (NetworkableIntegerValue)newHP;
            HPChangedEvent?.Invoke();
            
            bool fromGameEffect = Convert.ToBoolean(metadata);
            if (fromGameEffect && oldHPInt.Value < newHPInt.Value) {
                HealedEvent?.Invoke();
            } else if (fromGameEffect && oldHPInt.Value > newHPInt.Value) {
                AttackedEvent?.Invoke(newHPInt.Value <= 0);
            }
        }
        
        #endregion

        /// <summary>
        /// Report that an attack hit on this location and dealt damage, either onto this entity or onto some other entity
        /// at this location
        /// </summary>
        public void AttackLandedAtLocation() {
            _lastAttackedTime.UpdateValue(new NetworkableFloatValue(Time.time));
        }
        
        /// <returns>
        /// Deal damage. Server only. 
        /// True if this results in the entity dying, otherwise false
        /// </returns>
        public bool ReceiveAttackFromEntity([NotNull] GridEntity sourceEntity, int bonusDamage) {
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
            
            // Minimum of 1 damage
            damage = Mathf.Max(1, damage);
            
            SetCurrentHP(CurrentHP - Mathf.RoundToInt(damage), true);

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
            SetCurrentHP(newHP, true);
        }

        /// <summary>
        /// Returns the amount of seconds since the last attack. 
        /// </summary>
        public float TimeSinceLastReceivedAttack() {
            return Time.time - LastAttackedTime;
        }
        
        private void Kill([NotNull] GridEntity sourceEntity) {
            if (MarkedForDeath) return;
            MarkedForDeath = true;
            
            foreach (IDeathAction deathAction in _gridEntity.DeathActions) {
                deathAction.DoDeathAction(_gridEntity, sourceEntity);
            }
            GameManager.Instance.CommandManager.AbilityExecutor.MarkForUnRegistration(_gridEntity, true);
        }
    }
}