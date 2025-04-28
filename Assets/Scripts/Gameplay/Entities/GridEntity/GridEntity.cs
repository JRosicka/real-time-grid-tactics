using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.BuildQueue;
using Gameplay.Managers;
using Gameplay.UI;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Represents an entity that exists at a specific position on the gameplay grid.
    /// Has an <see cref="IInteractBehavior"/> field to handle player input.
    /// </summary>
    public class GridEntity : NetworkBehaviour {
        // GameManager getters
        private static ICommandManager CommandManager => GameManager.Instance.CommandManager;
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
        public string DisplayName => EntityData.ID;
        public List<EntityTag> Tags => EntityData.Tags;
        public IEnumerable<IAbilityData> Abilities => EntityData.Abilities.Select(a => a.Content);
        public List<GameplayTile> SlowTiles;
        public List<GameplayTile> InaccessibleTiles;

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
        private NetworkableField _killCountField;
        public int KillCount => ((NetworkableIntegerValue)_killCountField?.Value)?.Value ?? 0;

        // Abilities
        /// <summary>
        /// This entity's active abilities.
        /// </summary>
        public List<AbilityCooldownTimer> ActiveTimers = new();
        /// <summary>
        /// This entity's current ability queue.
        /// </summary>
        public List<IAbility> QueuedAbilities = new();

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
        public event Action<IAbility, AbilityCooldownTimer> AbilityPerformedEvent;
        public event Action<IAbility, AbilityCooldownTimer> CooldownTimerStartedEvent;
        public event Action<IAbility, AbilityCooldownTimer> CooldownTimerExpiredEvent;
        public event Action<IAbility, AbilityCooldownTimer> PerformAnimationEvent;
        public event Action SelectedEvent;
        public event Action UnregisteredEvent;
        // Needs to happen right after UnregisteredEvent, probably. Keep separate. 
        public event Action KilledEvent;
        public event Action<int> KillCountChanged;
        public event Action<List<IAbility>> AbilityQueueUpdatedEvent;
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
            TargetLocationLogic.ValueChanged += TargetLocationLogicChanged;
            TargetLocationLogic.UpdateValue(new TargetLocationLogic(EntityData.CanRally, spawnLocation, null, false, false));
            LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
        }
        
        [ClientRpc]
        public void RpcInitialize(EntityData data, GameTeam team) {
            transform.parent = CommandManager.SpawnBucket;
            ClientInitialize(data, team);
        }

        /// <summary>
        /// Initialization that runs on each client
        /// </summary>
        public void ClientInitialize(EntityData data, GameTeam team) {
            EntityData = data;
            Team = team;
            GameTeam localPlayerTeam = GameManager.Instance.LocalTeam;

            if (localPlayerTeam == GameTeam.Spectator) {
                InteractBehavior = new UnownedInteractBehavior();
            } else if (Team == GameTeam.Neutral) {
                InteractBehavior = new UnownedInteractBehavior();
            } else if (Team == localPlayerTeam) {
                InteractBehavior = new OwnerInteractBehavior();
            } else {
                InteractBehavior = new EnemyInteractBehavior();
            }

            BuildQueue = data.CanBuild 
                ? new BuildableBuildQueue(this, data.BuildQueueSize) 
                : new NullBuildQueue();

            SetupStats();
            SetupView();
            
            InitializationStatusHandler.Initialize(null);
            InitializationStatusHandler.SetLocalClientReady();
            DeathStatusHandler.Initialize(OnEntityReadyToDie);
            LastAttackedEntity.ValueChanged += UpdateAttackTarget;
            TargetLocationLogic.ValueChanged += UpdateAttackTarget;
            _killCountField.ValueChanged += (_, _, _) => KillCountChanged?.Invoke(KillCount);
            
            Interactable = true;
        } 
        
        private void SetupStats() {
            MaxHP = EntityData.HP;
            MoveTime = EntityData.NormalMoveTime;
            Range = EntityData.Range;
            Damage = EntityData.Damage;

            List<GameplayTile> tiles = GameManager.Instance.Configuration.Tiles;
            // Add any tiles that have at least one of our tags in its inaccessible tags list
            InaccessibleTiles = tiles.Where(t => t.InaccessibleTags.Intersect(Tags).Any()).ToList();
            // Add any tiles that have at least one of our tags in its slow tags list, and is not an inaccessible tile
            SlowTiles = tiles.Where(t => t.SlowTags.Select(s => s.Tag).Intersect(Tags).Any())
                .Where(t => !InaccessibleTiles.Contains(t))
                .ToList();
        }
        
        private void Update() {
            List<AbilityCooldownTimer> activeTimersCopy = new List<AbilityCooldownTimer>(ActiveTimers);
            activeTimersCopy.ForEach(t => t.UpdateTimer(Time.deltaTime));
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

        /// <summary>
        /// Try to move or use an ability on the indicated location
        /// </summary>
        public void InteractWithCell(Vector2Int location) {
            if (!Interactable) return;
            InteractBehavior?.TargetCellWithUnit(this, location); 
        }

        #endregion
        #region AbilityTimers

        public void CreateAbilityTimer(IAbility ability, float overrideCooldownDuration = -1) {
            if (!NetworkClient.active) {
                // SP
                DoCreateAbilityTimer(ability, overrideCooldownDuration);
            } else if (NetworkServer.active) {
                // MP server. Make the timer immediately locally, and also make the RPC call to do it remotely. 
                DoCreateAbilityTimer(ability, overrideCooldownDuration); 
                RpcCreateAbilityTimer(ability, overrideCooldownDuration);
            }
            // Else MP client, do nothing
        }

        [ClientRpc]
        private void RpcCreateAbilityTimer(IAbility ability, float overrideCooldownDuration) {
            if (NetworkServer.active) return;   // Don't make the timer on the server since it was already created locally there. 
            DoCreateAbilityTimer(ability, overrideCooldownDuration);
        }

        private void DoCreateAbilityTimer(IAbility ability, float overrideCooldownDuration) {
            AbilityCooldownTimer newCooldownTimer = new AbilityCooldownTimer(ability, overrideCooldownDuration);
            ActiveTimers.Add(newCooldownTimer);
            CooldownTimerStartedEvent?.Invoke(ability, newCooldownTimer);
        }

        public void AddTimeToAbilityTimer(IAbility ability, float timeToAdd) {
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
        
        #endregion
        #region Abilities
        
        [CanBeNull]
        public TAbilityData GetAbilityData<TAbilityData>() {
            return (TAbilityData)EntityData.Abilities.FirstOrDefault(a => a.Content.GetType() == typeof(TAbilityData))?.Content;
        }

        public void TriggerAbilityCooldownExpired(IAbility ability, AbilityCooldownTimer cooldownTimer, bool canceled) {
            CooldownTimerExpiredEvent?.Invoke(ability, cooldownTimer);
            if (!canceled && ability.AbilityData.AnimateWhenCooldownComplete) {
                PerformAnimationEvent?.Invoke(ability, cooldownTimer);
            }
        }
        
        public List<IAbility> GetCancelableAbilities() {
            List<IAbility> abilities = new List<IAbility>();
            
            // Get cancelable active abilities
            abilities.AddRange(ActiveTimers
                .Select(t => t.Ability)
                .Where(a => a.AbilityData.CancelableWhileActive));
            
            // Get cancelable queued abilities
            abilities.AddRange(QueuedAbilities.Where(a => a.AbilityData.CancelableWhileQueued));

            return abilities;
        }

        public void CancelAllAbilities() {
            List<IAbility> cancelableAbilities = GetCancelableAbilities();
            if (cancelableAbilities.Count > 0) {
                cancelableAbilities.ForEach(a => CommandManager.CancelAbility(a));
                    
                // Update the rally point
                var currentLocation = Location;
                // The location might be null if the entity is being destroyed 
                if (currentLocation != null) {
                    SetTargetLocation(currentLocation.Value, null, false);
                }
            }
            
            // Also stop holding position
            ToggleHoldPosition(false);
        }
        
        public void UpdateAbilityQueue(List<IAbility> newAbilityQueue) {
            QueuedAbilities = newAbilityQueue;
            GameManager.Instance.QueuedStructureBuildsManager.UpdateQueuedBuildsForEntity(this);
            AbilityQueueUpdatedEvent?.Invoke(newAbilityQueue);
        }
        
        /// <summary>
        /// Responds with any client-specific user-facing events for an ability being performed
        /// </summary>
        public void AbilityPerformed(IAbility abilityInstance) {
            AbilityCooldownTimer cooldownTimer = ActiveTimers.FirstOrDefault(t => t.Ability.UID == abilityInstance.UID);
            if (cooldownTimer == null) {
                return;
            }

            if (abilityInstance is BuildAbility) {
                GameManager.Instance.QueuedStructureBuildsManager.UpdateQueuedBuildsForEntity(this); 
            }

            AbilityPerformedEvent?.Invoke(abilityInstance, cooldownTimer);
            if (!abilityInstance.AbilityData.AnimateWhenCooldownComplete) {
                PerformAnimationEvent?.Invoke(abilityInstance, cooldownTimer);
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
        #region Moving
        
        public bool TryMoveToCell(Vector2Int targetCell, bool blockedByOccupation) {
            if (!CanMoveOrRally) return false;

            MoveAbilityData data = GetAbilityData<MoveAbilityData>();
            if (AbilityAssignmentManager.PerformAbility(this, data, new MoveAbilityParameters {
                    Destination = targetCell, 
                    NextMoveCell = targetCell, 
                    SelectorTeam = Team,
                    BlockedByOccupation = blockedByOccupation
                }, true, true)) {
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
            return !InaccessibleTiles.Contains(tile);
        }
        
        /// <summary>
        /// The total amount of time it takes for this entity to move to a cell with the given tile, assuming it starts
        /// adjacent to it.
        /// </summary>
        public float MoveTimeToTile(GameplayTile tile) {
            if (!CanPathFindToTile(tile)) {
                return -1;
            }

            return EntityData.NormalMoveTime * tile.GetMoveModifier(Tags);
        }

        public void ToggleHoldPosition(bool holdPosition) {
            HoldingPosition = holdPosition;

            if (!holdPosition) return;
            
            // Cancel any queued moves and attacks
            List<IAbility> abilities = QueuedAbilities.Where(a => a is MoveAbility or AttackAbility).ToList();
            abilities.ForEach(a => CommandManager.CancelAbility(a));

            // Update the rally point
            var currentLocation = Location;
            // The location might be null if the entity is being destroyed 
            if (currentLocation != null) {
                SetTargetLocation(currentLocation.Value, null, false);
            }
        }
        
        #endregion
        #region Attacking

        public void TryAttackMoveToCell(Vector2Int targetCell) {
            if (!CanMoveOrRally) return;

            AttackAbilityData data = GetAbilityData<AttackAbilityData>();
            if (AbilityAssignmentManager.PerformAbility(this, data, new AttackAbilityParameters {
                        Destination = targetCell, 
                        TargetFire = false
                    }, true, true)) {
                SetTargetLocation(targetCell, null, true);
            }
        }

        private void IncrementKillCount() {
            _killCountField.UpdateValue(new NetworkableIntegerValue(KillCount + 1));
        }
        
        public enum TargetType {
            Enemy = 1,
            Ally = 2,
            Neutral = 3
        }

        public void TryTargetEntity(GridEntity targetEntity, Vector2Int targetCell) {
            TargetType targetType = GetTargetType(targetEntity);

            if (targetType != TargetType.Enemy) return;
            AttackAbilityData data = GetAbilityData<AttackAbilityData>();
            if (AbilityAssignmentManager.PerformAbility(this, data, new AttackAbilityParameters {
                    Target = targetEntity, 
                    TargetFire = true,
                    Destination = targetCell
                }, true, true)) {
                SetTargetLocation(targetCell, targetEntity, true);
            }
        }

        public TargetType GetTargetType(GridEntity targetEntity) {
            if (targetEntity == null) return TargetType.Neutral;
            if (targetEntity.Team == GameTeam.Neutral || Team == GameTeam.Neutral) return TargetType.Neutral;
            return Team == targetEntity.Team ? TargetType.Ally : TargetType.Enemy;
        }

        public void ReceiveAttackFromEntity(GridEntity sourceEntity, int bonusDamage) {
            sourceEntity.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(this));
            if (Location == null) {
                Debug.LogWarning("Entity received attack but it is not registered or unregistered");
                return;
            }
            
            bool killed = HPHandler.ReceiveAttackFromEntity(sourceEntity, bonusDamage);
            TryRespondToAttack(sourceEntity);

            // TODO would be better to put this in a more central attack module. It should gather a total amount of kills in the given instant in order to account for multiple kills at once (splash damage)
            if (killed) {
                sourceEntity.IncrementKillCount();
            }
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

        private void TryRespondToAttack(GridEntity sourceEntity) {
            // If we not an attacker, no response
            if (!EntityData.AttackByDefault) return;
            // If somehow no location (not registered), no response
            if (sourceEntity.Location == null) return;
            
            // Determine whether we should respond with an attack
            bool queuedAbilitiesAllowResponse = QueuedAbilities.Count == 0 || QueuedAbilities.All(a =>
                a is AttackAbility attackAbility // No response if there are any queued non-attack abilities
                && !attackAbility.AbilityParameters.TargetFire      // Or if any of those queued attacks are target-fire
                && !attackAbility.AbilityParameters.Reaction);      // Or if any are reactions
            bool hasAttackMoveTargetLocation = QueuedAbilities.Count > 0;
            if (!queuedAbilitiesAllowResponse) return;
            
            // Attack-move to the target
            AbilityAssignmentManager.QueueAbility(this, GetAbilityData<AttackAbilityData>(), new AttackAbilityParameters {
                TargetFire = false,
                Destination = sourceEntity.Location.Value,
                Reaction = true,
                ReactionTarget = sourceEntity
            }, true, false, true, false);
            if (!hasAttackMoveTargetLocation && !HoldingPosition) {
                SetTargetLocation(sourceEntity.Location.Value, null, true);
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
            // Huh, actually I don't think there's anything to do here
            Interactable = false;
        }

        /// <summary>
        /// We have just detected that all clients are ready for this entity to be destroyed. Do that. 
        /// </summary>
        private void OnEntityReadyToDie() {
            CommandManager.DestroyEntity(this);
        }
        
        #endregion
    }
}