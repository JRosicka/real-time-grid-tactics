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

        public int CurrentHP { get; private set; }
        
        #region Update HP
        
        public void SetCurrentHP(int newHP, bool fromGameEffect) {
            int oldHP = CurrentHP;
            if (!NetworkClient.active) {
                // SP
                DoSetCurrentHP(newHP, oldHP, fromGameEffect);
            } else {
                // MP
                CurrentHP = newHP;  // Set HP value immediately
                CmdSetCurrentHP(newHP, oldHP, fromGameEffect);
            }
        }
        [Command(requiresAuthority = false)]
        private void CmdSetCurrentHP(int newHP, int oldHP, bool fromGameEffect) {
            RpcSetCurrentHP(newHP, oldHP, fromGameEffect);
        }
        [ClientRpc]
        private void RpcSetCurrentHP(int newHP, int oldHP, bool fromGameEffect) {
            DoSetCurrentHP(newHP, oldHP, fromGameEffect);
        }
        private void DoSetCurrentHP(int newHP, int oldHP, bool fromGameEffect) {
            CurrentHP = newHP;

            HPChangedEvent?.Invoke();
            if (fromGameEffect && oldHP < newHP) {
                HealedEvent?.Invoke();
            } else if (fromGameEffect && oldHP > newHP) {
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
                    ?.Select(e => e.Entity)?.Where(e => e.EntityData.IsStructure).ToList() ?? new List<GridEntity>();
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