using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Represents an entity that exists at a specific position on the gameplay grid.
    /// Has an <see cref="IInteractBehavior"/> field to handle player input.
    /// TODO There is a ton of stuff here, would be good to break this up in a refactor
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

        public ClientsStatusHandler InitializationStatusHandler;
        public ClientsStatusHandler DeathStatusHandler;

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
        [SyncVar(hook = nameof(OnHPChanged))] 
        private int _currentHP;
        public int CurrentHP {
            get => _currentHP;
            set {
                _currentHP = value;
                if (!NetworkClient.active) {
                    // SP, so syncvars won't work... Trigger manually.
                    HPChangedEvent?.Invoke();
                }
            }
        }
        private void OnHPChanged(int oldValue, int newValue) {
            HPChangedEvent?.Invoke();
        }

        [SyncVar(hook = nameof(OnMovesChanged))]
        private int _currentMoves;
        public int CurrentMoves {
            get => _currentMoves;
            set {
                _currentMoves = value;
                if (!NetworkClient.active) {
                    // SP, so syncvars won't work... Trigger manually.
                    MovesChangedEvent?.Invoke();
                }
            }
        }
        private void OnMovesChanged(int oldValue, int newValue) {
            MovesChangedEvent?.Invoke();
        }

        public List<AbilityCooldownTimer> ActiveTimers = new List<AbilityCooldownTimer>();


        [ClientRpc]
        public void RpcInitialize(EntityData data, Team team) {
            DoInitialize(data, team);
        }

        /// <summary>
        /// Initialization that runs on each client
        /// </summary>
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
            
            InitializationStatusHandler.Initialize(OnEntityInitializedAcrossAllClients);
            InitializationStatusHandler.SetLocalClientReady();
            DeathStatusHandler.Initialize(OnEntityReadyToDie);
        } 

        private void OnEntityInitializedAcrossAllClients() {
            PerformOnStartAbilities();
        }

        public bool CanTargetThings => Range > 0;
        public bool CanMove => MaxMove > 0;
        public Vector2Int Location => GameManager.Instance.GetLocationForEntity(this);

        public event Action<IAbility, AbilityCooldownTimer> AbilityPerformedEvent;
        public event Action<IAbility, AbilityCooldownTimer> CooldownTimerExpiredEvent;
        public event Action SelectedEvent;
        public event Action HPChangedEvent;
        public event Action KilledEvent;
        public event Action MovesChangedEvent;

        public void Select() {
            Debug.Log($"Selecting {UnitName}");
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
            Debug.Log($"Attempting to move {UnitName} to {targetCell}");
            MoveAbilityData data = (MoveAbilityData) Data.Abilities.First(a => a.Content.GetType() == typeof(MoveAbilityData)).Content;
            DoAbility(data, new MoveAbilityParameters { Destination = targetCell, SelectorTeam = MyTeam});
        }

        public void TryTargetEntity(GridEntity targetEntity, Vector2Int targetCell) {
            TargetType targetType = GetTargetType(this, targetEntity);

            // TODO figure out if target is in range

            if (targetType == TargetType.Enemy) {
                AttackAbilityData data = (AttackAbilityData) Data.Abilities
                    .First(a => a.Content.GetType() == typeof(AttackAbilityData)).Content;
                DoAbility(data, new AttackAbilityParameters { Target = targetEntity, Attacker = this });
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

        public bool IsAbilityChannelOnCooldown(AbilityChannel channel, out AbilityCooldownTimer timer) {
            timer = ActiveTimers.FirstOrDefault(t => t.Ability.AbilityData.Channel == channel);
            return timer != null;
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
            AbilityCooldownTimer newCooldownTimer = new AbilityCooldownTimer(ability);
            ActiveTimers.Add(newCooldownTimer);
        }

        public void ExpireTimerForAbility(IAbility ability) {
            // Find the timer with the indicated ability. The timers themselves are not synchronized, but since their abilities are we can use those. 
            AbilityCooldownTimer cooldownTimer = ActiveTimers.FirstOrDefault(t => t.Ability.UID == ability.UID);
            if (cooldownTimer == null) {
                Debug.LogError($"Timer for ability {ability.AbilityData.ContentResourceID} was not found");
                return;
            }

            cooldownTimer.Expire();
            ActiveTimers.Remove(cooldownTimer);
            CooldownTimerExpiredEvent?.Invoke(ability, cooldownTimer);
        }

        private void Update() {
            List<AbilityCooldownTimer> activeTimersCopy = new List<AbilityCooldownTimer>(ActiveTimers);
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

        private void PerformOnStartAbilities() {
            foreach (IAbilityData abilityData in Abilities.Select(a => a.Content).Where(a => a.PerformOnStart)) {
                DoAbility(abilityData, new NullAbilityParameters());
            }
        }

        /// <summary>
        /// Responds with any client-specific user-facing events for an ability being performed
        /// </summary>
        public void AbilityPerformed(IAbility abilityInstance) {
            AbilityCooldownTimer cooldownTimer = ActiveTimers.FirstOrDefault(t => t.Ability.UID == abilityInstance.UID);
            if (cooldownTimer == null) {
                Debug.LogError($"Timer for ability {abilityInstance.AbilityData.ContentResourceID} was not found");
                return;
            }

            if (abilityInstance.GetType() == typeof(MoveAbility)) {    // TODO Grooooooss. Really it would be good if the view could handle this. Or if necessary, we could set up handling of abilities as necessary similar to how the view does it, but only for special abilities that affect the GridEntity like this one.
                transform.position = GameManager.Instance.GridController.GetWorldPosition(((MoveAbilityParameters)abilityInstance.BaseParameters).Destination);
            }
            AbilityPerformedEvent?.Invoke(abilityInstance, cooldownTimer);
        }

        /// <summary>
        /// Responds with any client-specific user-facing events for an ability failing to be performed. Occurs if
        /// ability validation check failed. 
        /// </summary>
        public void AbilityFailed(IAbilityData ability) {
            // TODO
        }

        public void TestBuild() {
            BuildAbilityData data = (BuildAbilityData) Data.Abilities.First(a => a.Content.GetType() == typeof(BuildAbilityData)).Content;
            DoAbility(data, new BuildAbilityParameters{Buildable = data.Buildables[0].data, BuildLocation = Location});
        }

        private void SetupStats() {
            MaxHP = Data.HP;
            CurrentHP = Data.HP;
            MaxMove = Data.MaxMove;
            CurrentMoves = Data.MaxMove;
            Range = Data.Range;
            Damage = Data.Damage;
        }

        private void SetupView() {
            ViewCanvas.sortingOrder = Data.GetStackOrder();
            _view = Instantiate(Data.ViewPrefab, ViewCanvas.transform);
            _view.Initialize(this);
        }

        public void ReceiveAttackFromEntity(GridEntity sourceEntity) {
            Debug.Log($"Attacked!!!! And from a {sourceEntity.UnitName} no less! OW");
            
            CurrentHP -= sourceEntity.Damage;

            if (!NetworkClient.active) {
                // SP
                OnHPChanged();
            } else if (NetworkServer.active) {
                // MP server
                RpcOnHPChanged();
            }
            
            if (CurrentHP <= 0) {
                Kill();
            }
        }

        [ClientRpc]
        private void RpcOnHPChanged() {
            OnHPChanged();
        }

        private void OnHPChanged() {
            HPChangedEvent?.Invoke();
        }

        private void Kill() {
            // if (!NetworkClient.active) {
            //     // SP
            //     OnKilled();
            // } else if (NetworkServer.active) {
            //     // MP server
            //     RpcOnKilled();
            // }
            GameManager.Instance.CommandManager.UnRegisterEntity(this);
        }

        // [ClientRpc]
        // private void RpcOnKilled() {
        //     OnKilled();
        // }

        /// <summary>
        /// Client event letting us know that we have finished being unregistered.
        ///
        /// There might be other clients that need this to be around still.
        /// So instead of destroying this, just disallow interaction.
        /// </summary>
        public void OnUnregistered() {
            if (!NetworkClient.active) {   
                // SP
                // TODO Instead of destroying immediately, tell the view to do destroy animations and give the view a callback to destroy the entity when done. (and also, you know, make it so that this can't be interacted with by this client anymore)
            } else {
                // MP client

                // TODO Do the stop-interaction and view destroy logic outlined above, but as the callback to the view
                // destroy logic we should acknowledge to the server that we are ready to be destroyed. The server
                // listens for all clients to send this ready status (on a per-entity basis, so maybe the GridEntity itself keeps
                // track or has a new NetworkBehaviour subclass that handles this), and once all clients have sent the ready status
                // then it calls the destroy CommandManager command.  
                
            }
            
            DisallowInteraction();
            // When the view is done animating death, mark this client as ready to die so that the server knows when it can destroy this entity
            _view.KillAnimationFinishedEvent += DeathStatusHandler.SetLocalClientReady;
            KilledEvent?.Invoke();
        }

        private void DisallowInteraction() {
            // Huh, actually I don't think there's anything to do here
        }

        /// <summary>
        /// We have just detected that all clients are ready for this entity to be destroyed. Do that. 
        /// </summary>
        private void OnEntityReadyToDie() {
            GameManager.Instance.CommandManager.DestroyEntity(this);
        }
    }
}