using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Mirror;
using Sirenix.Utilities;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Represents an entity that exists at a specific position on the gameplay grid.
    /// Has an <see cref="IInteractBehavior"/> field to handle player input.
    /// TODO this is disorganized, would be good to update
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
        public Canvas ViewCanvas;

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
        public List<AbilityTimer> ActiveTimers = new List<AbilityTimer>();

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
            GameManager.Instance.CommandManager.RegisterEntity(this);
        }

        public bool CanTargetThings => true;
        public bool CanMove => true; // todo
        public Vector2Int Location => GameManager.Instance.GetLocationForEntity(this);

        public event Action<IAbility, AbilityTimer> AbilityPerformedEvent;
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
            GameManager.Instance.CommandManager.MoveEntityToCell(this, targetCell);
        }

        public void MovedCompleted(Vector2Int targetCell) {
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

        public bool CanUseAbility(IAbilityData data) {
            // Is this entity set up to use this ability?
            if (Abilities.All(a => a.Content != data)) {
                Debug.Log($"Can not use ability {data.ContentResourceID} because this entity was not configured to use it!");
                return false;
            }

            // Do we own the requirements for this ability?
            List<PurchasableData> ownedPurchasables = GameManager.Instance.GetPlayerForTeam(MyTeam).OwnedPurchasables;
            if (data.Requirements.Any(r => !ownedPurchasables.Contains(r))) {
                Debug.Log($"Can not use ability {data.ContentResourceID} because the player does not own all of the required purchasables");
                return false;
            }
            
            // Are there any active timers blocking this ability?
            if (ActiveTimers.Any(t => t.ChannelBlockers.Contains(data.Channel) && !t.Expired)) {
                Debug.Log($"Can not use ability {data.ContentResourceID} because it is blocked by an active timer");
                return false;
            }

            return true;
        }

        public void CreateAbilityTimer(IAbility ability) {
            if (!NetworkClient.active) {
                // SP
                DoCreateAbilityTimer(ability);
            } else if (NetworkServer.active) {
                // MP server
                RpcCreateAbilityTimer(ability);
            }
            // Else MP client, do nothing
        }

        [ClientRpc]
        private void RpcCreateAbilityTimer(IAbility ability) {
            DoCreateAbilityTimer(ability);
        }

        private void DoCreateAbilityTimer(IAbility ability) {
            AbilityTimer newTimer = new AbilityTimer(ability);
            ActiveTimers.Add(newTimer);
            newTimer.CompletedEvent += OnTimerExpired;
        }

        private void OnTimerExpired(AbilityTimer timer) {
            if (!NetworkClient.active) {
                // SP
                DoRemoveTimer(timer.Ability);
            } else if (NetworkServer.active) {
                // MP server
                RpcRemoveTimer(timer.Ability);
            }
            // Else MP client, do nothing
        }

        [ClientRpc]
        private void RpcRemoveTimer(IAbility ability) {
            DoRemoveTimer(ability);
        }

        private void DoRemoveTimer(IAbility ability) {
            AbilityTimer timer = ActiveTimers.FirstOrDefault(t => t.Ability == ability);    // TODO if this is networked, then will these instances be the same?
            if (timer == null) {
                Debug.LogError($"Timer for ability {ability.AbilityData.ContentResourceID} was not found");
                return;
            }

            timer.Expire();
            ActiveTimers.Remove(timer);
        }

        private void Update() {
            List<AbilityTimer> activeTimersCopy = new List<AbilityTimer>(ActiveTimers);
            activeTimersCopy.ForEach(t => t.UpdateTimer(Time.deltaTime));
        }

        public void DoAbility(IAbilityData abilityData, IAbilityParameters parameters) {
            if (!abilityData.AbilityLegal(parameters, this)) {
                AbilityFailed(abilityData);
                return;
            }
            IAbility abilityInstance = abilityData.CreateAbility(parameters, this);
            GameManager.Instance.CommandManager.PerformAbility(abilityInstance);
        }

        /// <summary>
        /// Responds with any client-specific user-facing events for an ability being performed
        /// </summary>
        public void AbilityPerformed(IAbility abilityInstance) {
            AbilityTimer timer = ActiveTimers.FirstOrDefault(t => t.Ability == abilityInstance);    // TODO if this is networked, then will these instances be the same?
            if (timer == null) {
                Debug.LogError($"Timer for ability {abilityInstance.AbilityData.ContentResourceID} was not found");
                return;
            }
            AbilityPerformedEvent?.Invoke(abilityInstance, timer);
        }

        /// <summary>
        /// Responds with any client-specific user-facing events for an ability failing to be performed. Occurs if
        /// ability validation check failed. 
        /// </summary>
        public void AbilityFailed(IAbilityData ability) {
            // TODO
        }

        public void TestSiege() {
            DoAbility(Data.Abilities.First(a => a.Content.GetType() == typeof(SiegeAbilityData)).Content, new NullAbilityParameters());
        }
        
        public void TestBuild() {
            BuildAbilityData data = (BuildAbilityData) Data.Abilities.First(a => a.Content.GetType() == typeof(BuildAbilityData)).Content;
            DoAbility(data, new BuildAbilityParameters{Buildable = data.Buildables[0], BuildLocation = Location});
        }

        private void SetupStats() {
            MaxHP = Data.HP;
            CurrentHP = Data.HP;
            MaxMove = Data.MaxMove;
            Range = Data.Range;
            Damage = Data.Damage;
        }

        private void SetupView() {
            _view = Instantiate(Data.ViewPrefab, ViewCanvas.transform);
            _view.Initialize(this);
        }

        public void ReceiveAttackFromEntity(GridEntity sourceEntity) {
            Debug.Log($"Attacked!!!! And from a {sourceEntity.UnitName} no less! OW");
            // For now, any attack just kills this
            Kill();
        }

        private void Kill() {
            GameManager.Instance.CommandManager.UnRegisterAndDestroyEntity(this);    // TODO this should actually wait to destroy until all of the kill animations are done. So unregister now, kill later. 
        }
    }
}