using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.BuildQueue;
using Gameplay.Entities.Upgrades;
using Gameplay.Managers;
using Gameplay.UI;
using JetBrains.Annotations;
using Mirror;
using Scenes;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Represents an entity that exists at a specific position on the gameplay grid.
    /// Has an <see cref="IInteractBehavior"/> field to handle player input.
    /// </summary>
    public class GridEntity : NetworkBehaviour {
        // GameManager getters
        private static ICommandManager CommandManager => GameManager.Instance?.CommandManager;
        private static AbilityAssignmentManager AbilityAssignmentManager => GameManager.Instance.AbilityAssignmentManager;
        private static InGameTimer InGameTimer => GameManager.Instance.InGameTimer;

        #region Fields
        
        [Header("References")]
        public Canvas ViewCanvas;
        public ClientsStatusHandler InitializationStatusHandler;
        public ClientsStatusHandler DeathStatusHandler;
        public GridEntityHPHandler HPHandler;

        [Header("Config")] 
        public string UnitName;
        public GameTeam Team;
        public long UID;
        public string DisplayName => EntityData.ID;
        public List<EntityTag> Tags => EntityData.Tags;
        public IEnumerable<IAbilityData> Abilities => EntityData.Abilities.Select(a => a.Content);

        [Header("Stats")]
        public int MaxHP;
        public float MoveTime;
        public int Range;
        public int Damage;

        // NetworkableFields and composition objects
        public NetworkableField CurrentResources;
        public ResourceAmount CurrentResourcesValue => (ResourceAmount)CurrentResources.Value;
        public NetworkableField TargetLocationLogic;
        public TargetLocationLogic TargetLocationLogicValue => (TargetLocationLogic)TargetLocationLogic.Value;
        private NetworkableField _location;
        public IBuildQueue BuildQueue;
        [CanBeNull] // If not yet initialized on the client
        public IInteractBehavior InteractBehavior;
        private bool _normallyWouldBeAnEnemyButIsControllableDueToCheats;
        private NetworkableField _killCountField;
        public int KillCount => ((NetworkableIntegerValue)_killCountField?.Value)?.Value ?? 0;
        private NetworkableField _incomeRateField;
        public int IncomeRate => ((NetworkableIntegerValue)_incomeRateField?.Value)?.Value ?? 0;
        private NetworkableField _timerSpeedMultiplierField;
        public float TimerSpeedMultiplier => ((NetworkableFloatValue)_timerSpeedMultiplierField?.Value)?.Value ?? 0;
        private NetworkableField _slowMoveSpeedMultiplierField;
        public float SlowMoveSpeedMultiplier => ((NetworkableFloatValue)_slowMoveSpeedMultiplierField?.Value)?.Value ?? 0;
        private NetworkableField _additionalMovementTimeFromAttackingField;
        public float AdditionalMovementTimeFromAttacking => ((NetworkableFloatValue)_additionalMovementTimeFromAttackingField?.Value)?.Value ?? 0;

        // Abilities
        /// <summary>
        /// This entity's active abilities.
        /// </summary>
        public List<AbilityTimer> ActiveTimers = new();
        /// <summary>
        /// This entity's abilities that we have started to perform and have not yet completed
        /// </summary>
        public List<IAbility> InProgressAbilities = new();
        /// <summary>
        /// This entity's queued abilities. Each ability stores the UID of the ability it depends upon. Only updated on server. 
        /// </summary>
        public List<IAbility> QueuedAbilities = new();
        /// <summary>
        /// Updated client-side. Tracks the number of instances of each ability type that this entity has performed
        /// (i.e. put in progress) on a per-ability-type basis.  
        /// </summary>
        public Dictionary<string, int> AbilityInstanceCount = new();

        // Misc fields and properties
        [HideInInspector] 
        public bool Registered;
        [HideInInspector] 
        public EntityData EntityData;
        public NetworkableField LastAttackedEntity;
        public GridEntity LastAttackedEntityValue => ((NetworkableGridEntityValue)LastAttackedEntity.Value)?.Value;
        public bool Interactable { get; private set; }
        public bool CanTargetThings => Range > 0;
        public bool CanMoveOrRally => MoveTime > 0;
        public bool CanMove => CanMoveOrRally && !EntityData.IsStructure;
        public bool HoldingPosition { get; private set; }
        /// <summary>
        /// Null if the entity is unregistered (or not yet registered)
        /// </summary>
        public Vector2Int? Location {
            get {
                if (_location == null || _location.Value == null) {
                    Debug.LogWarning("Location is null!!");
                }
                return ((NetworkableVector2IntegerValue)_location?.Value)?.Value;
            }
        }

        [CanBeNull] public GameplayTile CurrentTileType {
            get {
                Vector2Int? location = Location;
                return location == null ? null : GameManager.Instance.GridController.GridData.GetCell(location.Value).Tile;
            }
        }
        public bool DeadOrDying => this == null || HPHandler.MarkedForDeath || _unregistered;

        // Client flag
        private bool _unregistered;
        private GridEntityView _view;
        
        // Events
        public event Action<IAbility, AbilityTimer> AbilityPerformedEvent;
        public event Action<IAbility, AbilityTimer> AbilityTimerStartedEvent;
        public event Action<IAbility, AbilityTimer> AbilityTimerExpiredEvent;
        public event Action<IAbility, AbilityTimer> PerformAnimationEvent;
        public event Action<IUpgrade> UpgradeAppliedEvent;
        public event Action<IUpgrade> UpgradeRemovedEvent;
        public event Action SelectedEvent;
        public event Action DeselectedEvent;
        public event Action<bool> TargetedEvent;
        public event Action UnregisteredEvent;
        // Needs to happen right after UnregisteredEvent, probably. Keep separate. 
        public event Action KilledEvent;
        public event Action<int> KillCountChanged;
        public event Action<int> IncomeRateChanged;
        public event Action<List<IAbility>> InProgressAbilitiesUpdatedEvent;
        /// <summary>
        /// Only triggered on server
        /// </summary>
        public event Action EntityMovedEvent;

        public event Action<GridEntity, GridEntity> AttackTargetUpdated;
        
        #endregion
        #region Initialization
        
        private void Awake() {
            CurrentResources = new NetworkableField(this, nameof(CurrentResources), new ResourceAmount());
            TargetLocationLogic = new NetworkableField(this, nameof(TargetLocationLogic), new TargetLocationLogic());
            _location = new NetworkableField(this, nameof(_location), new NetworkableVector2IntegerValue(new Vector2Int(0, 0)));
            LastAttackedEntity = new NetworkableField(this, nameof(LastAttackedEntity), new NetworkableGridEntityValue(null));
            _killCountField = new NetworkableField(this, nameof(_killCountField), new NetworkableIntegerValue(0));
            _incomeRateField = new NetworkableField(this, nameof(_incomeRateField), new NetworkableIntegerValue(0));
            _timerSpeedMultiplierField = new NetworkableField(this, nameof(_timerSpeedMultiplierField), new NetworkableFloatValue(1f));
            _slowMoveSpeedMultiplierField = new NetworkableField(this, nameof(_slowMoveSpeedMultiplierField), new NetworkableFloatValue(1f));
            _additionalMovementTimeFromAttackingField = new NetworkableField(this, nameof(_additionalMovementTimeFromAttackingField), new NetworkableFloatValue(0));
        }

        /// <summary>
        /// Initialization method ran only on the server, before <see cref="ClientInitialize"/>.
        /// </summary>
        public void ServerInitialize(EntityData data, GameTeam team, Vector2Int spawnLocation) {
            EntityData = data;
            Team = team;
            
            // We call this later on the client, but we need the stats set up immediately on the server too (at least for rallying) 
            SetupStats();

            // NetworkableFields
            HPHandler.SetCurrentHP(EntityData.HP, false);
            CurrentResources.UpdateValue(EntityData.StartingResourceSet);
            _location.UpdateValue(new NetworkableVector2IntegerValue(spawnLocation));

            int startingIncomeRate = GetAbilityData<IncomeAbilityData>()?.ResourceAmountIncome.Amount ?? 0;
            if (startingIncomeRate != 0) {
                SetIncomeRate(startingIncomeRate);
            }
            
            TargetLocationLogic.ValueChanged += TargetLocationLogicChanged;
            TargetLocationLogic.UpdateValue(new TargetLocationLogic(EntityData.CanRally, spawnLocation, null, false, false));
            LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
        }
        
        [ClientRpc]
        public void RpcInitialize(EntityData data, GameTeam team, bool built, long entityUID) {
            transform.parent = CommandManager.SpawnBucket;
            ClientInitialize(data, team, built, entityUID);
        }

        /// <summary>
        /// Initialization that runs on each client
        /// </summary>
        public void ClientInitialize(EntityData data, GameTeam team, bool built, long entityUID) {
            EntityData = data;
            Team = team;
            UID = entityUID;
            GameTeam localPlayerTeam = GameManager.Instance.LocalTeam;
            
            foreach (AbilityDataScriptableObject abilityData in data.Abilities) {
                AbilityInstanceCount[abilityData.Content.ContentResourceID] = 0;
            }

            if (localPlayerTeam == GameTeam.Spectator) {
                InteractBehavior = new UnownedInteractBehavior();
            } else if (Team == GameTeam.Neutral) {
                if (data.ControllableByAllPlayers) {
                    InteractBehavior = new OwnerInteractBehavior(this);
                } else {
                    InteractBehavior = new UnownedInteractBehavior();
                }
            } else if (Team == localPlayerTeam) {
                InteractBehavior = new OwnerInteractBehavior(this);
                if (built) {
                    // Play the sound effect
                    GameManager.Instance.GameAudio.EntityFinishedBuildingSound(EntityData);
                }
            } else if (GameManager.Instance.Cheats.ControlAllPlayers) {
                _normallyWouldBeAnEnemyButIsControllableDueToCheats = true;
                InteractBehavior = new OwnerInteractBehavior(this);
            } else {
                InteractBehavior = new EnemyInteractBehavior();
            }

            BuildQueue = data.CanBuild 
                ? data.ControllableByAllPlayers 
                    ? new SharedBuildableBuildQueue(this, data.BuildQueueSize)
                    : new BuildableBuildQueue(this, data.BuildQueueSize) 
                : new NullBuildQueue();

            SetupStats();
            SetupView();
            
            // Set up view portion of any in-progress upgrades
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(team);
            player?.OwnedPurchasablesController.Upgrades.ApplyUpgradeAnimations(this);
            
            InitializationStatusHandler.Initialize(null, nameof(InitializationStatusHandler));
            InitializationStatusHandler.SetLocalClientReady();
            DeathStatusHandler.Initialize(OnEntityReadyToDie, nameof(DeathStatusHandler));
            LastAttackedEntity.ValueChanged += UpdateAttackTarget;
            TargetLocationLogic.ValueChanged += UpdateAttackTarget;
            _killCountField.ValueChanged += (_, _, _) => KillCountChanged?.Invoke(KillCount);
            _incomeRateField.ValueChanged += (_, _, _) => IncomeRateChanged?.Invoke(IncomeRate);
            
            Interactable = true;
        } 
        
        private void SetupStats() {
            MaxHP = EntityData.HP;
            MoveTime = EntityData.NormalMoveTime;
            Range = EntityData.Range;
            Damage = EntityData.Damage;
        }
        
        private void Update() {
            List<AbilityTimer> activeTimersCopy = new List<AbilityTimer>(ActiveTimers);
            activeTimersCopy.ForEach(t => t.UpdateTimer(Time.deltaTime * TimerSpeedMultiplier));
        }

        #endregion
        #region View

        private void SetupView() {
            int stackOrder = EntityData.GetStackOrder();
            ViewCanvas.sortingOrder = stackOrder;
            Debug.Log($"[{InGameTimer.MatchLengthString}] Instantiating view for {EntityData.ID}");
            _view = Instantiate(EntityData.ViewPrefab, ViewCanvas.transform);
            _view.Initialize(this, stackOrder);
        }

        public void ToggleView(bool show) {
            _view.ToggleView(show);
        }
        
        #endregion
        #region Target Location
        
        private void TargetLocationLogicChanged(INetworkableFieldValue oldValueBase, INetworkableFieldValue newValueBase, object metadata) {
            if (oldValueBase == null && newValueBase == null) return;
            TargetLocationLogic oldValue = (TargetLocationLogic)oldValueBase;
            TargetLocationLogic newValue = (TargetLocationLogic)newValueBase;

            void TryUnSubscribe() {
                if (oldValue.TargetEntity == null) return;
                oldValue.TargetEntity.EntityMovedEvent -= TargetEntityUpdated;
                oldValue.TargetEntity.UnregisteredEvent -= TargetEntityUpdated;
            }
            void TrySubscribe() {
                if (newValue.TargetEntity == null) return;
                // TODO this might cause memory leaks, since I don't know of a good way to unregister these events since we are not guaranteed to call SyncTargetLocationLogic from the server. 
                newValue.TargetEntity.EntityMovedEvent += TargetEntityUpdated;
                newValue.TargetEntity.UnregisteredEvent += TargetEntityUpdated;
            }
            
            // This sort of weird branching is meant to ensure that listeners get properly unsubscribed and that we 
            // only subscribe the new target entity if it is actually new
            if (newValue == null) {
                TryUnSubscribe();
            } else if (oldValue == null) {
                TrySubscribe();
            } else if (oldValue.TargetEntity != newValue.TargetEntity) {
                TryUnSubscribe();
                TrySubscribe();
            }
        }
        private void TargetEntityUpdated() {
            Vector2Int? newLocation = TargetLocationLogicValue.TargetEntity == null ? null : TargetLocationLogicValue.TargetEntity.Location;
            if (newLocation == null) {
                SetTargetLocation(TargetLocationLogicValue.CurrentTarget, null, TargetLocationLogicValue.Attacking);
            } else {
                SetTargetLocation(newLocation.Value, TargetLocationLogicValue.TargetEntity, TargetLocationLogicValue.Attacking);
            }
        }
        public void SetTargetLocation(Vector2Int newTargetLocation, GridEntity targetEntity, bool attacking, bool hidePathDestination = false) {
            TargetLocationLogic.UpdateValue(new TargetLocationLogic(TargetLocationLogicValue.CanRally, newTargetLocation, targetEntity, attacking, hidePathDestination));
        }

        private void UpdateAttackTarget(INetworkableFieldValue oldValue, INetworkableFieldValue newValue, string metadata) {
            AttackTargetUpdated?.Invoke(this, GetAttackTarget());
        }

        public GridEntity GetAttackTarget() {
            if (TargetLocationLogicValue.TargetEntity != null && TargetLocationLogicValue.TargetEntity.Team != GameTeam.Neutral) {
                return TargetLocationLogicValue.TargetEntity;
            }

            if (LastAttackedEntityValue != null) {
                return LastAttackedEntityValue;
            }

            return null;
        }
        
        #endregion
        #region Selection and Inputs
        
        public void Select() {
            if (!Interactable) return;
            if (InteractBehavior != null) {
                InteractBehavior.Select(this);
                SelectedEvent?.Invoke();
            }
        }

        public void Deselect() {
            if (!Interactable) return;
            if (InteractBehavior != null) {
                DeselectedEvent?.Invoke();
            }
        }

        public void DisplayTargeted(bool show) {
            TargetedEvent?.Invoke(show);
        }

        /// <summary>
        /// Try to move or use an ability on the indicated location
        /// </summary>
        public void InteractWithCell(Vector2Int location) {
            if (!Interactable) return;
            InteractBehavior?.TargetCellWithUnit(this, location); 
        }

        public void ToggleControlFromCheat(bool controlEverything) {
            if (InteractBehavior == null) {
                Debug.LogWarning("Unset interact behavior, cheat will not work properly");
                return;
            }
            if (controlEverything && InteractBehavior is EnemyInteractBehavior) {
                _normallyWouldBeAnEnemyButIsControllableDueToCheats = true;
                InteractBehavior = new OwnerInteractBehavior(this);
            } else if (!controlEverything && _normallyWouldBeAnEnemyButIsControllableDueToCheats) {
                InteractBehavior = new EnemyInteractBehavior();
            }
        }

        #endregion
        #region AbilityTimers

        public void CreateAbilityTimer(IAbility ability, float overrideCooldownDuration = -1) {
            if (!GameTypeTracker.Instance.GameIsNetworked) {
                // SP
                DoCreateAbilityTimer(ability, overrideCooldownDuration);
            } else if (GameTypeTracker.Instance.HostForNetworkedGame) {
                // MP server. Make the timer immediately locally, and also make the RPC call to do it remotely. 
                DoCreateAbilityTimer(ability, overrideCooldownDuration); 
                RpcCreateAbilityTimer(ability, overrideCooldownDuration);
            }
            // Else MP client, do nothing
        }

        [ClientRpc]
        private void RpcCreateAbilityTimer(IAbility ability, float overrideCooldownDuration) {
            if (GameTypeTracker.Instance.HostForNetworkedGame) return;   // Don't make the timer on the server since it was already created locally there. 
            DoCreateAbilityTimer(ability, overrideCooldownDuration);
        }

        private void DoCreateAbilityTimer(IAbility ability, float overrideCooldownDuration) {
            AbilityTimer newTimer = new AbilityTimer(ability, overrideCooldownDuration);
            ActiveTimers.Add(newTimer);
            AbilityTimerStartedEvent?.Invoke(ability, newTimer);
        }

        public void AddTimeToAbilityTimer(IAbility ability, float timeToAdd) {
            if (!GameTypeTracker.Instance.GameIsNetworked) {
                // SP
                DoAddTimeToAbilityTimer(ability, timeToAdd);
            } else if (GameTypeTracker.Instance.HostForNetworkedGame) {
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
            List<AbilityTimer> activeTimersCopy = new List<AbilityTimer>(ActiveTimers);
            AbilityTimer timer = activeTimersCopy.FirstOrDefault(t => t.Ability.AbilityData == ability.AbilityData);
            if (timer == null) {
                Debug.Log("Tried to add time for an ability that does not currently have an active timer");
                return;
            }

            timer.AddTime(timeToAdd);
        }
        
        #endregion
        #region Abilities
        
        [CanBeNull]
        public TAbilityData GetAbilityData<TAbilityData>() {
            return (TAbilityData)EntityData.Abilities.FirstOrDefault(a => a.Content.GetType() == typeof(TAbilityData))?.Content;
        }

        public void TriggerAbilityCooldownExpired(IAbility ability, AbilityTimer abilityTimer, bool canceled) {
            AbilityTimerExpiredEvent?.Invoke(ability, abilityTimer);
            if (!canceled && ability.AbilityData.AnimateWhenCooldownComplete) {
                PerformAnimationEvent?.Invoke(ability, abilityTimer);
            }
        }
        
        public List<IAbility> GetCancelableAbilities(bool fromInput = true) {
            List<IAbility> abilities = new List<IAbility>();
            
            // Get cancelable ability timers
            abilities.AddRange(ActiveTimers
                .Select(t => t.Ability)
                .Where(a => a.AbilityData.CancelableWhileOnCooldown));
            
            // Get cancelable in-progress abilities
            abilities.AddRange(InProgressAbilities.Where(a => a.AbilityData.CancelableWhileInProgress));

            if (fromInput) {
                // Remove any abilities that can not be canceled manually
                abilities.RemoveAll(a => !a.Cancelable);
            }
            
            return abilities;
        }

        public void CancelAllAbilities() {
            List<IAbility> cancelableAbilities = GetCancelableAbilities();
            if (cancelableAbilities.Count > 0) {
                cancelableAbilities.ForEach(a => CommandManager.CancelAbility(a, true));
                    
                // Update the rally point
                var currentLocation = Location;
                // The location might be null if the entity is being destroyed 
                if (currentLocation != null) {
                    SetTargetLocation(currentLocation.Value, null, false);
                }
            }
            
            // Also stop holding position
            ToggleHoldPosition(false, true);
        }
        
        public void UpdateInProgressAbilities(List<IAbility> newInProgressAbilitiesSet) {
            InProgressAbilities = newInProgressAbilitiesSet;
            GameManager.Instance.QueuedStructureBuildsManager.UpdateQueuedBuildsForEntity(this);
            InProgressAbilitiesUpdatedEvent?.Invoke(newInProgressAbilitiesSet);
        }
        
        /// <summary>
        /// Responds with any client-specific user-facing events for an ability being performed
        /// </summary>
        public void AbilityPerformed(IAbility abilityInstance) {
            AbilityTimer abilityTimer = ActiveTimers.FirstOrDefault(t => t.Ability.UID == abilityInstance.UID);
            if (abilityTimer == null) {
                return;
            }

            if (abilityInstance is BuildAbility) {
                GameManager.Instance.QueuedStructureBuildsManager.UpdateQueuedBuildsForEntity(this); 
            }

            AbilityPerformedEvent?.Invoke(abilityInstance, abilityTimer);
            if (!abilityInstance.AbilityData.AnimateWhenCooldownComplete) {
                PerformAnimationEvent?.Invoke(abilityInstance, abilityTimer);
            }
        }

        /// <summary>
        /// Responds with any client-specific user-facing events for an ability failing to be performed. Occurs if
        /// ability validation check failed. 
        /// </summary>
        public void AbilityFailed(IAbilityData ability) {
            // TODO
        }
        
        #endregion
        #region Upgrades
        /// <summary>
        /// Responds with any client-specific user-facing events for an upgrade being applied
        /// </summary>
        public void UpgradeApplied(IUpgrade upgrade) {
            UpgradeAppliedEvent?.Invoke(upgrade);
        }
        public void UpgradeRemoved(IUpgrade upgrade) {
            UpgradeRemovedEvent?.Invoke(upgrade);
        }
        #endregion
        #region Moving
        
        public bool TryMoveToCell(Vector2Int targetCell, bool fromInput, bool recordForReplay) {
            if (!CanMoveOrRally) return false;

            MoveAbilityData data = GetAbilityData<MoveAbilityData>();
            if (AbilityAssignmentManager.StartPerformingAbility(this, data, new MoveAbilityParameters {
                    Destination = targetCell, 
                    NextMoveCell = targetCell, 
                    BlockedByOccupation = true
                }, fromInput, true, true, recordForReplay)) {
                SetTargetLocation(targetCell, null, false);
            }
            return true;
        }
        
        // Triggered on server
        public void UpdateEntityLocation(Vector2Int newLocation) {
            _location.UpdateValue(new NetworkableVector2IntegerValue(newLocation));
        }
        
        // Triggered on server
        public void TriggerEntityMovedEvent() {
            EntityMovedEvent?.Invoke();
        }

        /// <summary>
        /// Whether this entity can move to (or rally to) a cell with the given tile, assuming it starts adjacent to it.
        /// </summary>
        public bool CanPathFindToTile(GameplayTile tile) {
            return !EntityDataForPathfinding().InaccessibleTiles.Contains(tile);
        }

        public EntityData EntityDataForPathfinding() {
            return EntityData.OverrideEntityDataForPathfinding ?? EntityData;
        }
        
        /// <summary>
        /// The total amount of time it takes for this entity to move to a cell with the given tile, assuming it starts
        /// adjacent to it.
        /// </summary>
        public float MoveTimeToTile(GameplayTile tile) {
            if (!CanPathFindToTile(tile)) {
                return -1;
            }

            EntityData entityData = EntityDataForPathfinding();
            float normalMoveTime = entityData.NormalMoveTime;
            float extraMoveTimeFromTile = tile.GetMoveModifier(entityData.Tags) - 1;
            if (tile.IsSlowed(entityData.Tags)) {
                extraMoveTimeFromTile *= SlowMoveSpeedMultiplier;
            }
            return normalMoveTime + normalMoveTime * extraMoveTimeFromTile;
        }

        public void ToggleHoldPosition(bool holdPosition, bool recordForReplay) {
            if (recordForReplay) {
                GameManager.Instance.ReplayManager.TryRecordHoldPosition(this, holdPosition);
            }
            
            HoldingPosition = holdPosition;

            if (!holdPosition) return;
            
            // Cancel any in-progress moves and attacks
            List<IAbility> abilities = InProgressAbilities.Where(a => a is MoveAbility or AttackAbility).ToList();
            abilities.ForEach(a => CommandManager.CancelAbility(a, false));

            // Update the rally point
            var currentLocation = Location;
            // The location might be null if the entity is being destroyed 
            if (currentLocation != null) {
                SetTargetLocation(currentLocation.Value, null, false);
            }
        }
        
        #endregion
        #region Attacking

        public void TryAttack(Vector2Int targetCell, GridEntity targetEntity) {
            if (!CanMoveOrRally) return;

            if (targetEntity == null) {
                // Attack move
                AttackAbilityData data = GetAbilityData<AttackAbilityData>();
                if (AbilityAssignmentManager.StartPerformingAbility(this, data, new AttackAbilityParameters {
                        Destination = targetCell
                    }, false, true, true, false)) {
                    SetTargetLocation(targetCell, null, true);
                }
            } else {
                // Target attack
                TargetAttackAbilityData data = GetAbilityData<TargetAttackAbilityData>();
                if (AbilityAssignmentManager.StartPerformingAbility(this, data, new TargetAttackAbilityParameters {
                        Target = targetEntity
                    }, false, true, true, false)) {
                    SetTargetLocation(targetCell, targetEntity, true);
                }
            }
        }

        public void IncrementKillCount() {
            _killCountField.UpdateValue(new NetworkableIntegerValue(KillCount + 1));
        }

        public void SetIncomeRate(int newIncomeRate) {
            _incomeRateField.UpdateValue(new NetworkableIntegerValue(newIncomeRate));
        }

        public void SetTimerMultiplier(float newTimerMultiplier) {
            _timerSpeedMultiplierField.UpdateValue(new NetworkableFloatValue(newTimerMultiplier));
        }

        public void SetSlowMoveSpeedMultiplier(float newSlowMoveSpeedMultiplier) {
            _slowMoveSpeedMultiplierField.UpdateValue(new NetworkableFloatValue(newSlowMoveSpeedMultiplier));
        }

        public void SetAdditionalMovementTimeFromAttacking(float newAdditionalMovementTime) {
            _additionalMovementTimeFromAttackingField.UpdateValue(new NetworkableFloatValue(newAdditionalMovementTime));
        }
        
        public float MovementTimeFromAttacking => EntityData.AddedMovementTimeFromAttacking + AdditionalMovementTimeFromAttacking;

        public enum TargetType {
            Enemy = 1,
            Ally = 2,
            Neutral = 3
        }

        public void TryTargetEntity(GridEntity targetEntity, Vector2Int targetCell) {
            TargetType targetType = GetTargetType(targetEntity);

            if (targetType != TargetType.Enemy) return;
            TargetAttackAbilityData data = GetAbilityData<TargetAttackAbilityData>();
            if (AbilityAssignmentManager.StartPerformingAbility(this, data, new TargetAttackAbilityParameters() {
                    Target = targetEntity, 
                }, true, true, true, true)) {
                SetTargetLocation(targetCell, targetEntity, true);
            }
        }

        public TargetType GetTargetType(GridEntity targetEntity) {
            if (targetEntity == null) return TargetType.Neutral;
            if (targetEntity.Team == GameTeam.Neutral || Team == GameTeam.Neutral) return TargetType.Neutral;
            return Team == targetEntity.Team ? TargetType.Ally : TargetType.Enemy;
        }
        
        public string GetAttackTooltipMessageFromAbilities() {
            string tooltipMessage = "";
            foreach (IAbilityData abilityData in Abilities) {
                string attackTooltipMessage = abilityData.GetAttackTooltipMessage(Team);
                if (string.IsNullOrEmpty(attackTooltipMessage)) continue;

                if (!string.IsNullOrEmpty(tooltipMessage)) {
                    tooltipMessage += "<br>";
                }
                tooltipMessage += attackTooltipMessage;
            }

            return tooltipMessage;
        }

        public string GetAttackTooltipMessageFromUpgrades(string currentTooltipMessage) {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(this);
            if (player == null) return currentTooltipMessage;
            foreach (IUpgrade upgrade in player.OwnedPurchasablesController.Upgrades.GetOwnedUpgrades()) {
                string attackTooltipMessage = upgrade.GetAttackTooltipMessage(this);
                if (string.IsNullOrEmpty(attackTooltipMessage)) continue;
                
                if (!string.IsNullOrEmpty(currentTooltipMessage)) {
                    currentTooltipMessage += "<br>";
                }

                currentTooltipMessage += attackTooltipMessage;
            }

            return currentTooltipMessage;
        }

        public string GetMoveTooltipMessageFromUpgrades() {
            string currentTooltipMessage = "";
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(this);
            if (player == null) return currentTooltipMessage;
            foreach (IUpgrade upgrade in player.OwnedPurchasablesController.Upgrades.GetOwnedUpgrades()) {
                string moveTooltipMessage = upgrade.GetMoveTooltipMessage(this);
                if (string.IsNullOrEmpty(moveTooltipMessage)) continue;

                if (!string.IsNullOrEmpty(moveTooltipMessage)) {
                    moveTooltipMessage += "<br>";
                }

                currentTooltipMessage += moveTooltipMessage;
            }
            
            return currentTooltipMessage;
        }
        
        public int GetStructureDefenseModifier() {
            if (EntityData.IsStructure) {
                return EntityData.SharedUnitArmorBonus;
            }

            List<GridEntity> structuresAtLocation = GameManager.Instance.CommandManager.EntitiesOnGrid.EntitiesAtLocation(Location!.Value)?.Entities
                ?.Select(e => e.Entity).Where(e => e.EntityData.IsStructure).ToList() ?? new List<GridEntity>();
            foreach (GridEntity structure in structuresAtLocation) {
                // Return the damage modifier of any structures whose modifier should be applied to this entity
                if (structure.EntityData.SharedUnitDamageTakenModifierTags.Count == 0
                    || structure.EntityData.SharedUnitDamageTakenModifierTags.Any(t => EntityData.Tags.Contains(t))) {
                    return structure.EntityData.SharedUnitArmorBonus;
                }
            }

            return 0;
        }

        public int GetTerrainDefenseModifier() {
            return CurrentTileType!.GetDefenseModifier(EntityData);
        }

        public void TryRespondToAttack(GridEntity sourceEntity) {
            // If we not an attacker, no response
            if (!EntityData.AttackByDefault) return;
            // If somehow no location (not registered), no response
            if (sourceEntity.Location == null) return;
            
            // Determine whether we should respond with an attack
            bool inProgressAbilitiesAllowResponse = InProgressAbilities.Count == 0;
            if (!inProgressAbilitiesAllowResponse) {
                if (HoldingPosition) {
                    // No response if we are holding position
                } else if (!InProgressAbilities.Any(a => a is AttackAbility)) {
                    // No response if there are no attack abilities in progress
                } else if (!InProgressAbilities.All(a => a is AttackAbility or MoveAbility)) {
                    // No response if there are any non-attack non-move abilities in progress
                } else if (InProgressAbilities.Where(a => a is AttackAbility).Cast<AttackAbility>()
                           .Any(a => a.AbilityParameters.Reaction)) {
                    // No response if any of the in-progress attack abilities are reactive attacks
                } else {
                    inProgressAbilitiesAllowResponse = true;
                }
            }
            if (!inProgressAbilitiesAllowResponse) return;
            
            // If the active attack is just targeting the current location, then it is a default attack rather than an 
            // attack move. So don't bother queueing it. 
            IAbility actualAttackMove = InProgressAbilities.FirstOrDefault(a => a is AttackAbility attackAbility && attackAbility.AbilityParameters.Destination != Location);
            
            // Attack-move to the target
            AbilityAssignmentManager.StartPerformingAbility(this, GetAbilityData<AttackAbilityData>(), new AttackAbilityParameters {
                Destination = sourceEntity.Location.Value,
                Reaction = true,
                ReactionTarget = sourceEntity
            }, false, true, true, false);
            
            if (actualAttackMove != null) {
                // Re-queue
                IAbility reactiveAttackAbility = InProgressAbilities.FirstOrDefault(a => a is AttackAbility attackAbility && attackAbility.AbilityParameters.Reaction);
                if (reactiveAttackAbility == null) {
                    Debug.LogWarning("We just created a targeted ability, but it is missing in the in-progress abilities list!");
                } else {
                    AbilityAssignmentManager.QueueAbility(this, actualAttackMove.AbilityData, actualAttackMove.BaseParameters, reactiveAttackAbility);
                }
            } else {
                // Since there will be no follow up attack queued, just set the target location to track this entity since that's all we will be doing
                SetTargetLocation(sourceEntity.Location.Value, sourceEntity, true);
            }
        }
        
        #endregion
        #region Death
        
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

            _unregistered = true;
            UnregisteredEvent?.Invoke();
            if (showDeathAnimation) {
                KilledEvent?.Invoke();
            }
        }
        
        private void DisallowInteraction() {
            Interactable = false;
        }

        /// <summary>
        /// We have just detected that all clients are ready for this entity to be destroyed. Do that. 
        /// </summary>
        private void OnEntityReadyToDie() {
            CommandManager?.DestroyEntity(this);
        }
        
        #endregion
    }
}