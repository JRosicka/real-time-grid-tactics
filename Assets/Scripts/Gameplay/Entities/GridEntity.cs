using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GamePlay.Entities {
    /// <summary>
    /// Represents an entity that exists at a specific position on the gameplay grid.
    /// Has an <see cref="IInteractBehavior"/> field to handle player input. 
    /// </summary>
    public class GridEntity : NetworkBehaviour {
        public enum Team {
            Neutral = -1,
            Player1 = 1,
            Player2 = 2
        }
        private enum TargetType {
            Enemy = 1,
            Ally = 2,
            Neutral = 3
        }
        

        private GridEntityViewBase _view;

        [Header("Config")] 
        public string UnitName;
        public Team MyTeam;

        [HideInInspector] 
        public bool Registered;
        
        public EntityData Data;
        private IInteractBehavior _interactBehavior;
        
        [Header("Stats")]
        public int MaxHP;
        public int MaxMove;
        public int Range;
        public int Damage;
        public string DisplayName => Data.ID;
        public List<EntityData.EntityTag> Tags => Data.Tags;
        public List<AbilityDataScriptableObject> Abilities => Data.Abilities; // TODO maybe I do want these to be interfaces after all?

        [Header("Current")]
        public int CurrentHP;

        [ClientRpc]
        public void RpcInitialize(EntityData data, Team team) {
            DoInitialize(data, team);
        }

        public void DoInitialize(EntityData data, Team team) {
            Data = data;
            MyTeam = team;
            Team playerTeam = GameManager.Instance.LocalPlayer.Data.Team;
            
            if (MyTeam == Team.Neutral) {
                _interactBehavior = new NeutralInteractBehavior();
            } else if (MyTeam == playerTeam) {
                _interactBehavior = new OwnerInteractBehavior();
            } else {
                _interactBehavior = new EnemyInteractBehavior();
            }

            SetupStats();
            SetupView();

            // TODO we check for the registered flag on the entity, so it probably won't get registered twice (once from each client). But, there might be a better way to do this with authority
            GameManager.Instance.CommandController.RegisterEntity(this);
        }

        public bool CanTargetThings => true;
        public bool CanMove => true; // todo
        public Vector2Int Location => GameManager.Instance.GetLocationForEntity(this);

        public event Action<IAbility> AbilityPerformedEvent;
        public event Action SelectedEvent;
        public event Action<Vector2Int> MovedEvent;
        public event Action<Vector2Int> AttackPerformedEvent;
        public event Action AttackReceivedEvent;
        public event Action KilledEvent;

        public void Select() {
            Debug.Log($"Selecting {UnitName}");
            // Deselect the currently selected entity
            GameManager.Instance.SelectedEntity = null;
            _interactBehavior.Select(this);
            SelectedEvent?.Invoke();
        }

        /// <summary>
        /// Try to move or use an ability on the indicated location
        /// </summary>
        public void InteractWithCell(Vector2Int location) {
            _interactBehavior.TargetCellWithUnit(this, location);
        }

        public void MoveToCell(Vector2Int targetCell) {
            Debug.Log($"Moving {UnitName} to {targetCell}");
            GameManager.Instance.CommandController.MoveEntityToCell(this, targetCell);
        }

        public void OnMoveCompleted(Vector2Int targetCell) {
            MovedEvent?.Invoke(targetCell);
        }

        public void TryTargetEntity(GridEntity targetEntity, Vector2Int targetCell) {
            TargetType targetType = GetTargetType(this, targetEntity);

            // TODO figure out if target is in range

            if (targetType == TargetType.Enemy) {
                targetEntity.ReceiveAttackFromEntity(this);
            } else {
                // TODO remove after done testing. The grid entity selected itself or an ally or a neutral. Test the ability. 
                TestBuild();
            }
        }

        private static TargetType GetTargetType(GridEntity originEntity, GridEntity targetEntity) {
            if (targetEntity.MyTeam == Team.Neutral || originEntity.MyTeam == Team.Neutral) {
                return TargetType.Neutral;
            }

            return originEntity.MyTeam == targetEntity.MyTeam ? TargetType.Ally : TargetType.Enemy;
        }

        public void CreateAbility(IAbilityData abilityData, IAbilityParameters parameters) {
            IAbility abilityInstance = abilityData.CreateAbility(parameters);
            GameManager.Instance.CommandController.PerformAbility(abilityInstance, this);
        }

        /// <summary>
        /// Responds with any client-specific user-facing events for an ability being performed
        /// </summary>
        public void AbilityPerformed(IAbility abilityInstance) {
            AbilityPerformedEvent?.Invoke(abilityInstance);
        }

        public void TestSiege() {
            CreateAbility(Data.Abilities.First(a => a.Content.GetType() == typeof(SiegeAbilityData)).Content, new NullAbilityParameters());
        }
        
        public void TestBuild() {
            BuildAbilityData data = (BuildAbilityData) Data.Abilities.First(a => a.Content.GetType() == typeof(BuildAbilityData)).Content;
            CreateAbility(data, new BuildAbilityParameters{Buildable = data.Buildables[0], BuildLocation = Location});
        }

        private void SetupStats() {
            MaxHP = Data.HP;
            CurrentHP = Data.HP;
            MaxMove = Data.MaxMove;
            Range = Data.Range;
            Damage = Data.Damage;
        }

        private void SetupView() {
            _view = Instantiate(Data.ViewPrefab, transform);
            _view.Initialize(this);
        }

        public void ReceiveAttackFromEntity(GridEntity sourceEntity) {
            Debug.Log($"Attacked!!!! And from a {sourceEntity.UnitName} no less! OW");
            // For now, any attack just kills this
            Kill();
        }

        private void Kill() {
            GameManager.Instance.CommandController.UnRegisterAndDestroyEntity(this);    // TODO this should actually wait to destroy until all of the kill animations are done. So unregister now, kill later. 
        }
    }
}