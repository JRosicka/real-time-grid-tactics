using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Manages state and updates for a <see cref="GridEntity"/>'s HP. Handles incoming attacks and heals, and triggers
    /// death.  
    /// </summary>
    public class GridEntityHPHandler : NetworkBehaviour {
        public event Action HPChangedEvent;
        public event Action AttackedEvent;
        public event Action HealedEvent;
        
        [SerializeField] private GridEntity _gridEntity;
        private EntityData EntityData => _gridEntity.EntityData;

        // Server flag?
        public bool MarkedForDeath { get; private set; }

        public int CurrentHP => _currentHP?.Value.Value ?? 0;
        
        private NetworkableField<NetworkableIntegerValue> _currentHP;

        private void Awake() {
            _currentHP = new NetworkableField<NetworkableIntegerValue>(this, nameof(_currentHP), new NetworkableIntegerValue(0));
            _currentHP.ValueChanged += HPChanged;
        }
        
        #region Update HP
        
        public void SetCurrentHP(int newHP, bool fromGameEffect) {
            _currentHP.UpdateValue(new NetworkableIntegerValue(newHP), fromGameEffect.ToString());
        }

        private void HPChanged(NetworkableIntegerValue oldHP, NetworkableIntegerValue newHP, string metadata) {
            HPChangedEvent?.Invoke();
            
            bool fromGameEffect = Convert.ToBoolean(metadata);
            if (fromGameEffect && oldHP.Value < newHP.Value) {
                HealedEvent?.Invoke();
            } else if (fromGameEffect && oldHP.Value > newHP.Value) {
                AttackedEvent?.Invoke();
            }
        }
        
        #endregion

        public void ReceiveAttackFromEntity(GridEntity sourceEntity) {
            // Get base damage
            float damage = sourceEntity.Damage;
            
            // Apply any additive attack modifiers based on tags
            damage += sourceEntity.EntityData.TagsToApplyBonusDamageTo.Any(t => EntityData.Tags.Contains(t))
                ? sourceEntity.EntityData.BonusDamage
                : 0;
            
            // Apply any multiplicative defense modifiers from terrain
            damage *= _gridEntity.CurrentTileType!.GetDefenseModifier(EntityData);

            // Apply any multiplicative defense modifiers from structures (as long as this is not a structure)
            if (!EntityData.IsStructure) {
                List<GridEntity> structuresAtLocation = GameManager.Instance.CommandManager.EntitiesOnGrid.EntitiesAtLocation(_gridEntity.Location!.Value)?.Entities
                    ?.Select(e => e.Entity).Where(e => e.EntityData.IsStructure).ToList() ?? new List<GridEntity>();
                foreach (GridEntity structure in structuresAtLocation) {
                    if (structure.EntityData.SharedUnitDamageTakenModifierTags.Count == 0
                        || structure.EntityData.SharedUnitDamageTakenModifierTags.Any(t => EntityData.Tags.Contains(t))) {
                        damage *= structure.EntityData.SharedUnitDamageTakenModifier;
                    }
                }
            }
            
            SetCurrentHP(CurrentHP - Mathf.RoundToInt(damage), true);

            if (CurrentHP <= 0) {
                Kill();
            }
        }
        
        public void Heal(int healAmount) {
            if (CurrentHP == _gridEntity.MaxHP) return;

            int newHP = CurrentHP + healAmount;
            newHP = Mathf.Min(newHP, _gridEntity.MaxHP);
            SetCurrentHP(newHP, true);
        }
        
        private void Kill() {
            if (MarkedForDeath) return;
            MarkedForDeath = true;
            
            GameManager.Instance.CommandManager.UnRegisterEntity(_gridEntity, true);
        }
    }
}