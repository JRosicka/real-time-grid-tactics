using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

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
        // TODO move this team stuff to a new class
        public static Team OpponentTeam(Team myTeam) {
            return myTeam switch {
                Team.Neutral => Team.Neutral,
                Team.Player1 => Team.Player2,
                Team.Player2 => Team.Player1,
                _ => throw new ArgumentOutOfRangeException(nameof(myTeam), myTeam, null)
            };
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

        [FormerlySerializedAs("Data")] public EntityData EntityData;
        private IInteractBehavior _interactBehavior;
        
        [Header("Stats")]
        public int MaxHP;
        public float MoveTime;
        public int Range;
        public int Damage;
        public string DisplayName => EntityData.ID;
        public List<EntityData.EntityTag> Tags => EntityData.Tags;
        public List<AbilityDataScriptableObject> Abilities => EntityData.Abilities; // TODO maybe I do want these to be interfaces after all?
        public List<GameplayTile> SlowTiles;
        public List<GameplayTile> InaccessibleTiles;

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

        public List<AbilityCooldownTimer> ActiveTimers = new List<AbilityCooldownTimer>();
        
        /// <summary>
        /// This entity's current ability queue.
        /// TODO if this gets complicated then we should extract this out into a new AbilityQueue class. 
        /// </summary>
        public List<IAbility> QueuedAbilities = new List<IAbility>();
        
        public bool Interactable { get; private set; }

        [ClientRpc]
        public void RpcInitialize(EntityData data, Team team) {
            transform.parent = GameManager.Instance.CommandManager.SpawnBucket;
            DoInitialize(data, team);
        }

        /// <summary>
        /// Initialization that runs on each client
        /// </summary>
        public void DoInitialize(EntityData data, Team team) {
            EntityData = data;
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
            
            Interactable = true;
        } 

        private void OnEntityInitializedAcrossAllClients() {
            PerformOnStartAbilities();
        }

        public bool CanTargetThings => Range > 0;
        public bool CanMove => MoveTime > 0;
        public Vector2Int Location => GameManager.Instance.GetLocationForEntity(this);

        public event Action<IAbility, AbilityCooldownTimer> AbilityPerformedEvent;
        public event Action<IAbility, AbilityCooldownTimer> CooldownTimerStartedEvent;
        public event Action<IAbility, AbilityCooldownTimer> CooldownTimerExpiredEvent;
        public event Action SelectedEvent;
        public event Action HPChangedEvent;
        public event Action KilledEvent;
        public event Action UnregisteredEvent;

        public void Select() {
            if (!Interactable) return;
            Debug.Log($"Selecting {UnitName}");
            _interactBehavior.Select(this);
            SelectedEvent?.Invoke();
        }

        /// <summary>
        /// Try to move or use an ability on the indicated location
        /// </summary>
        public void InteractWithCell(Vector2Int location) {
            if (!Interactable) return;
            _interactBehavior.TargetCellWithUnit(this, location);
        }

        public bool TryMoveToCell(Vector2Int targetCell) {
            Debug.Log($"Attempting to move {UnitName} to {targetCell}");
            if (!CanMove) return false;

            MoveAbilityData data = (MoveAbilityData) EntityData.Abilities.First(a => a.Content.GetType() == typeof(MoveAbilityData)).Content;
            PerformAbility(data, new MoveAbilityParameters { Destination = targetCell, NextMoveCell = targetCell, SelectorTeam = MyTeam}, true);
            return true;
        }

        public void TryTargetEntity(GridEntity targetEntity, Vector2Int targetCell) {
            TargetType targetType = GetTargetType(this, targetEntity);
            
            if (targetType == TargetType.Enemy) {
                AttackAbilityData data = (AttackAbilityData) EntityData.Abilities
                    .First(a => a.Content is AttackAbilityData).Content;
                PerformAbility(data, new AttackAbilityParameters {
                    Target = targetEntity, 
                    TargetFire = true,
                    Destination = targetCell
                }, true);
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
                return false;
            }

            // Do we own the requirements for this ability?
            List<PurchasableData> ownedPurchasables = GameManager.Instance.GetPlayerForTeam(MyTeam)
                .OwnedPurchasablesController.OwnedPurchasables;
            if (data.Requirements.Any(r => !ownedPurchasables.Contains(r))) {
                return false;
            }
            
            // Are there any active timers blocking this ability?
            if (ActiveTimers.Any(t => t.ChannelBlockers.Contains(data.Channel) && !t.Expired)) {
                return false;
            }

            return true;
        }

        public bool IsAbilityChannelOnCooldown(AbilityChannel channel, out AbilityCooldownTimer timer) {
            timer = ActiveTimers.FirstOrDefault(t => t.Ability.AbilityData.Channel == channel);
            return timer != null;
        }
        
        public void CreateAbilityTimer(IAbility ability, float overrideCooldownDuration = -1) {
            if (!NetworkClient.active) {
                // SP
                DoCreateAbilityTimer(ability, overrideCooldownDuration);
            } else if (NetworkServer.active) {
                // MP server
                RpcCreateAbilityTimer(ability, overrideCooldownDuration);
            }
            // Else MP client, do nothing
        }

        [ClientRpc]
        private void RpcCreateAbilityTimer(IAbility ability, float overrideCooldownDuration) {
            DoCreateAbilityTimer(ability, overrideCooldownDuration);
        }

        private void DoCreateAbilityTimer(IAbility ability, float overrideCooldownDuration) {
            AbilityCooldownTimer newCooldownTimer = new AbilityCooldownTimer(ability, overrideCooldownDuration);
            ActiveTimers.Add(newCooldownTimer);
            CooldownTimerStartedEvent?.Invoke(ability, newCooldownTimer);
        }

        private void AddTimeToAbilityTimer(IAbility ability, float timeToAdd) {
            if (!NetworkClient.active) {
                // SP
                DoAddTimeToAbilityTimer(ability, timeToAdd);
            } else if (NetworkServer.active) {
                // MP server
                RpcAddTimeToAbilityTimer(ability, timeToAdd);
            }
            // Else MP client, do nothing
        }
        
        [ClientRpc]
        private void RpcAddTimeToAbilityTimer(IAbility ability, float timeToAdd) {
            DoAddTimeToAbilityTimer(ability, timeToAdd);
        }

        private void DoAddTimeToAbilityTimer(IAbility ability, float timeToAdd) {
            List<AbilityCooldownTimer> activeTimersCopy = new List<AbilityCooldownTimer>(ActiveTimers);
            AbilityCooldownTimer timer = activeTimersCopy.FirstOrDefault(t => t.Ability.AbilityData == ability.AbilityData);
            if (timer == null) {
                Debug.Log("Tried to add time for an ability that does not currently have an active timer");
                return;
            }

            timer.AddTime(timeToAdd);
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

        /// <summary>
        /// Add time to the movement cooldown timer due to another ability being performed.
        /// If there is an active cooldown timer, then this amount is added to that timer.
        /// Otherwise, a new cooldown timer is added with this amount.
        /// </summary>
        public void AddMovementTime(float timeToAdd) {
            AbilityDataScriptableObject moveAbilityScriptable = Abilities.FirstOrDefault(a => a.Content is MoveAbilityData);
            if (moveAbilityScriptable == null) return;
            
            List<AbilityCooldownTimer> activeTimersCopy = new List<AbilityCooldownTimer>(ActiveTimers);
            AbilityCooldownTimer movementTimer = activeTimersCopy.FirstOrDefault(t => t.Ability is MoveAbility);
            if (movementTimer != null) {
                if (movementTimer.Expired) {
                    Debug.LogWarning("Tried to add movement cooldown timer time from another ability, but that " +
                                     "timer is expired. Adding new movement cooldown timer instead. This might not behave correctly.");
                } else {
                    // Add this time to the current movement cooldown timer
                    AddTimeToAbilityTimer(movementTimer.Ability, timeToAdd);
                    return;
                }
            }
            
            // Add a new movement cooldown timer
            MoveAbilityData moveAbilityData = (MoveAbilityData)moveAbilityScriptable.Content;
            CreateAbilityTimer(new MoveAbility(moveAbilityData, new MoveAbilityParameters {
                Destination = Location,
                NextMoveCell = Location,
                SelectorTeam = MyTeam
            }, this), timeToAdd);
        }

        private void Update() {
            List<AbilityCooldownTimer> activeTimersCopy = new List<AbilityCooldownTimer>(ActiveTimers);
            activeTimersCopy.ForEach(t => t.UpdateTimer(Time.deltaTime));
        }

        public bool PerformAbility(IAbilityData abilityData, IAbilityParameters parameters, bool queueIfNotLegal) {
            if (!abilityData.AbilityLegal(parameters, this)) {
                if (queueIfNotLegal) {
                    // We specified to perform the ability now, but we can't legally do that. So queue it. 
                    QueueAbility(abilityData, parameters, true, true, false);
                    return true;
                } else {
                    AbilityFailed(abilityData);
                    return false;
                }
            }
            IAbility abilityInstance = abilityData.CreateAbility(parameters, this);
            abilityInstance.WaitUntilLegal = queueIfNotLegal;
            GameManager.Instance.CommandManager.PerformAbility(abilityInstance, true);
            return true;
        }

        public bool PerformQueuedAbility(IAbility ability) {
            if (!ability.AbilityData.AbilityLegal(ability.BaseParameters, ability.Performer)) {
                AbilityFailed(ability.AbilityData);
                return false;
            }
            
            GameManager.Instance.CommandManager.PerformAbility(ability, false);
            return true;
        }

        public void QueueAbility(IAbilityData abilityData, IAbilityParameters parameters, bool waitUntilLegal, bool clearQueueFirst, bool insertAtFront) {
            IAbility abilityInstance = abilityData.CreateAbility(parameters, this);
            abilityInstance.WaitUntilLegal = waitUntilLegal;
            GameManager.Instance.CommandManager.QueueAbility(abilityInstance, clearQueueFirst, insertAtFront);
        }

        /// <summary>
        /// Only occurs on server. That's the only place where we keep track of queued abilities anyway. 
        /// </summary>
        public void ClearAbilityQueue() {
            QueuedAbilities.Clear();
            // Also cancel any abilities that are in the middle of being resolved
            List<AbilityCooldownTimer> activeTimersToCancel = ActiveTimers
                .Where(a => a.Ability.AbilityData.CancelWhenNewCommandGivenToPerformer).ToList();
            activeTimersToCancel.ForEach(t => t.CancelAbility());
        }

        /// <summary>
        /// Auto-select any abilities that we have configured as auto-selectable.
        /// This probably won't behave well if this entity has multiple abilities configured as auto-selectable... 
        /// </summary>
        public void PerformAutoSelection() {
            foreach (IAbilityData abilityData in Abilities.Select(a => a.Content).Where(a => a.AutoSelect)) {
                abilityData.SelectAbility(this);
            }
        }
        
        private void PerformOnStartAbilities() {
            foreach (IAbilityData abilityData in Abilities.Select(a => a.Content).Where(a => a.PerformOnStart)) {
                PerformAbility(abilityData, new NullAbilityParameters(), false);
            }
        }

        public void PerformDefaultAbility() {
            if (!EntityData.AttackByDefault) return;
            
            AttackAbilityData data = (AttackAbilityData) EntityData.Abilities
                .FirstOrDefault(a => a.Content is AttackAbilityData)?.Content;
            if (data == null) return;
            
            PerformAbility(data, new AttackAbilityParameters {
                TargetFire = false,
                Destination = Location
            }, true);
        }

        /// <summary>
        /// Responds with any client-specific user-facing events for an ability being performed
        /// </summary>
        public void AbilityPerformed(IAbility abilityInstance) {
            AbilityCooldownTimer cooldownTimer = ActiveTimers.FirstOrDefault(t => t.Ability.UID == abilityInstance.UID);
            if (cooldownTimer == null) {
                return;
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

        /// <summary>
        /// Whether this entity can move to a cell with the given tile, assuming it starts adjacent to it.
        /// </summary>
        public bool CanEnterTile(GameplayTile tile) {
            if (!CanMove) return false;
            return !InaccessibleTiles.Contains(tile);
        }
        
        /// <summary>
        /// The total amount of time it takes for this entity to move to a cell with the given tile, assuming it starts
        /// adjacent to it.
        /// </summary>
        public float MoveTimeToTile(GameplayTile tile) {
            if (!CanEnterTile(tile)) {
                return -1;
            }

            if (SlowTiles.Contains(tile)) {
                return EntityData.SlowMoveTime;
            }

            return EntityData.NormalMoveTime;
        }
        
        private void SetupStats() {
            MaxHP = EntityData.HP;
            CurrentHP = EntityData.HP;
            MoveTime = EntityData.NormalMoveTime;
            Range = EntityData.Range;
            Damage = EntityData.Damage;

            List<GameplayTile> tiles = GameManager.Instance.Configuration.Tiles;
            // Add any tiles that have at least one of our tags in its inaccessible tags list
            InaccessibleTiles = tiles.Where(t => t.InaccessibleTags.Intersect(Tags).Any()).ToList();
            // Add any tiles that have at least one of our tags in its slow tags list, and is not an inaccessible tile
            SlowTiles = tiles.Where(t => t.SlowTags.Intersect(Tags).Any())
                .Where(t => !InaccessibleTiles.Contains(t))
                .ToList();
        }

        private void SetupView() {
            int stackOrder = EntityData.GetStackOrder();
            ViewCanvas.sortingOrder = stackOrder;
            _view = Instantiate(EntityData.ViewPrefab, ViewCanvas.transform);
            _view.Initialize(this, stackOrder);
        }

        public void ReceiveAttackFromEntity(GridEntity sourceEntity) {
            Debug.Log($"Attacked!!!! And from a {sourceEntity.UnitName} no less! OW");
            
            // TODO a-move to the target location if no abilities are queued and configured to attack by default.
            // Necessary so that the entity doesn't just sit there if attacked by something outside of its range. 
            
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

        private bool _markedForDeath;
        private void Kill() {
            if (_markedForDeath) return;
            _markedForDeath = true;
            
            GameManager.Instance.CommandManager.UnRegisterEntity(this, true);
        }
        
        /// <summary>
        /// Client event letting us know that we have finished being unregistered and are dying.
        ///
        /// There might be other clients that need this to be around still.
        /// So instead of destroying this, just disallow interaction.
        /// </summary>
        public void OnUnregistered(bool showDeathAnimation) {
            DisallowInteraction();
            
            if (showDeathAnimation) {
                // When the view is done animating death, mark this client as ready to die so that the server knows when it can destroy this entity
                _view.KillAnimationFinishedEvent += DeathStatusHandler.SetLocalClientReady;
            } else {
                // Skip animation, immediately mark as ready to die
                DeathStatusHandler.SetLocalClientReady();
            }
            
            UnregisteredEvent?.Invoke(); 
            KilledEvent?.Invoke();
        }
        
        private void DisallowInteraction() {
            // Huh, actually I don't think there's anything to do here
            Interactable = false;
        }

        /// <summary>
        /// We have just detected that all clients are ready for this entity to be destroyed. Do that. 
        /// </summary>
        private void OnEntityReadyToDie() {
            GameManager.Instance.CommandManager.DestroyEntity(this);
        }
    }
}