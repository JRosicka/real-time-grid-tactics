using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using JetBrains.Annotations;
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
        

        private GridEntityView _view;
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

        public int CurrentHP { get; private set; }
        private void SetCurrentHP(int newHP, bool fromGameEffect) {
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

        [SyncVar(hook = nameof(OnCurrentResourcesChanged))] 
        private ResourceAmount _currentResources;
        public ResourceAmount CurrentResources {
            get => _currentResources;
            set {
                ResourceAmount oldValue = _currentResources;
                _currentResources = value;
                if (!NetworkClient.active) {
                    // SP, so syncvars won't work... Trigger manually.
                    OnCurrentResourcesChanged(oldValue, value);
                }
            }
        }
        private void OnCurrentResourcesChanged(ResourceAmount oldValue, ResourceAmount newValue) {
            ResourceAmountChangedEvent?.Invoke();
        }
        
        public List<AbilityCooldownTimer> ActiveTimers = new List<AbilityCooldownTimer>();
        
        /// <summary>
        /// This entity's current ability queue.
        /// TODO if this gets complicated then we should extract this out into a new AbilityQueue class. 
        /// </summary>
        public List<IAbility> QueuedAbilities = new List<IAbility>();

        public bool Interactable { get; private set; }

        public GridEntity LastAttackedEntity;

        /// <summary>
        /// Initialization method ran only on the server, before <see cref="ClientInitialize"/>.
        /// </summary>
        public void ServerInitialize(EntityData data, Team team, Vector2Int spawnLocation) {
            EntityData = data;
            MyTeam = team;
            
            // We call this later on the client, but we need the stats set up immediately on the server too (at least for rallying) 
            SetupStats();

            // Syncvar stats
            SetCurrentHP(EntityData.HP, false);
            CurrentResources = new ResourceAmount(EntityData.StartingResourceSet);
            
            // Target logic
            TargetLocationLogic = new TargetLocationLogic(EntityData.CanRally, spawnLocation, null);
            TargetLocationLogicChangedEvent += TargetLocationLogicChanged;
            SyncTargetLocationLogic();
        }
        
        [ClientRpc]
        public void RpcInitialize(EntityData data, Team team) {
            transform.parent = GameManager.Instance.CommandManager.SpawnBucket;
            ClientInitialize(data, team);
        }

        /// <summary>
        /// Initialization that runs on each client
        /// </summary>
        public void ClientInitialize(EntityData data, Team team) {
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
            Debug.Log("Entity initialized across all clients");
        }

        public bool CanTargetThings => Range > 0;
        public bool CanMove => MoveTime > 0;
        /// <summary>
        /// Null if the entity is unregistered (or not yet registered)
        /// </summary>
        public Vector2Int? Location => GameManager.Instance.GetLocationForEntity(this);
        
        [CanBeNull]
        private GameplayTile CurrentTileType {
            get {
                Vector2Int? location = Location;
                return location == null ? null : GameManager.Instance.GridController.GridData.GetCell(location.Value).Tile;
            }
        }

        public event Action<IAbility, AbilityCooldownTimer> AbilityPerformedEvent;
        public event Action<IAbility, AbilityCooldownTimer> CooldownTimerStartedEvent;
        public event Action<IAbility, AbilityCooldownTimer> CooldownTimerExpiredEvent;
        public event Action<IAbility, AbilityCooldownTimer> PerformAnimationEvent;
        public event Action SelectedEvent;
        public event Action HPChangedEvent;
        public event Action ResourceAmountChangedEvent;
        public event Action AttackedEvent;
        public event Action HealedEvent;
        public event Action KilledEvent;
        public event Action UnregisteredEvent;
        /// <summary>
        /// Only triggered on server
        /// </summary>
        public event Action EntityMovedEvent;

        #region Target Location
        
        public event Action<Vector2Int> TargetLocationLogicChangedEvent;
        [SyncVar(hook = nameof(OnTargetLocationLogicChanged))]
        public TargetLocationLogic TargetLocationLogic;
        private void OnTargetLocationLogicChanged(TargetLocationLogic oldValue, TargetLocationLogic newValue) {
            TargetLocationLogicChangedEvent?.Invoke(newValue.CurrentTarget);
        }
        /// <summary>
        /// Reset the reference for <see cref="TargetLocationLogic"/> to force a sync across clients. Just updating fields in the class
        /// is not enough to get the sync to occur. 
        /// </summary>
        private void SyncTargetLocationLogic() {
            TargetLocationLogic newTargetLocationLogic = new TargetLocationLogic(TargetLocationLogic.CanRally, TargetLocationLogic.CurrentTarget, TargetLocationLogic.TargetEntity);
            if (!NetworkServer.active && NetworkClient.active) {
                // MP client, so we need to network the change to the syncvar
                CmdSyncTargetLocationLogic(newTargetLocationLogic);
                return;
            }

            // Otherwise we are the MP server or in SP, so directly modify the field here 
            TargetLocationLogic = newTargetLocationLogic;
            if (!NetworkClient.active) {
                // SP, so syncvars won't work
                TargetLocationLogicChangedEvent?.Invoke(TargetLocationLogic.CurrentTarget);
            }
        }
        [Command(requiresAuthority = false)]
        private void CmdSyncTargetLocationLogic(TargetLocationLogic newTargetLocationLogic) {
            TargetLocationLogic = newTargetLocationLogic;
        }
        private void TargetLocationLogicChanged(Vector2Int newLocation) {
            if (TargetLocationLogic.TargetEntity != null) { // TODO this might cause memory leaks, since I don't know of a good way to unregister these events since we are not guaranteed to call SyncTargetLocationLogic from the server. 
                TargetLocationLogic.TargetEntity.EntityMovedEvent += TargetEntityUpdated;
                TargetLocationLogic.TargetEntity.UnregisteredEvent += TargetEntityUpdated;
            }
        }
        private void TargetEntityUpdated() {
            Vector2Int? newLocation = TargetLocationLogic.TargetEntity == null ? null : TargetLocationLogic.TargetEntity.Location;
            if (newLocation == null) {
                newLocation = Location;
                if (newLocation != null) {  // The location might be null if the entity is being destroyed 
                    SetTargetLocation(newLocation.Value, null);
                }
            } else {
                SetTargetLocation(newLocation.Value, TargetLocationLogic.TargetEntity);
            }
        }
        public void SetTargetLocation(Vector2Int newTargetLocation, GridEntity targetEntity) {
            TargetLocationLogic.CurrentTarget = newTargetLocation;
            TargetLocationLogic.TargetEntity = targetEntity;
            SyncTargetLocationLogic();
        }
        
        #endregion
        
        public void Select() {
            if (!Interactable) return;
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
            if (!CanMove) return false;

            MoveAbilityData data = (MoveAbilityData) EntityData.Abilities.First(a => a.Content.GetType() == typeof(MoveAbilityData)).Content;
            if (PerformAbility(data, new MoveAbilityParameters {
                        Destination = targetCell, 
                        NextMoveCell = targetCell, 
                        SelectorTeam = MyTeam
                    }, true)) {
                SetTargetLocation(targetCell, null);
            }
            return true;
        }

        public bool TryAttackMoveToCell(Vector2Int targetCell) {
            if (!CanMove) return false;

            AttackAbilityData data = (AttackAbilityData) EntityData.Abilities.First(a => a.Content.GetType() == typeof(AttackAbilityData)).Content;
            if (PerformAbility(data, new AttackAbilityParameters {
                        Destination = targetCell, 
                        TargetFire = false
                    },
                    true)) {
                SetTargetLocation(targetCell, null);
            }
            return true;
        }

        // Triggered on server
        public void EntityMoved() {
            EntityMovedEvent?.Invoke();
        }

        public void TryTargetEntity(GridEntity targetEntity, Vector2Int targetCell) {
            TargetType targetType = GetTargetType(this, targetEntity);

            if (targetType != TargetType.Enemy) return;
            AttackAbilityData data = (AttackAbilityData) EntityData.Abilities
                .First(a => a.Content is AttackAbilityData).Content;
            if (PerformAbility(data, new AttackAbilityParameters {
                        Target = targetEntity, 
                        TargetFire = true,
                        Destination = targetCell
                    }, true)) {
                SetTargetLocation(targetCell, targetEntity);
            }
        }

        public Vector2Int CurrentTargetLocation() {
            return TargetLocationLogic.CurrentTarget;
        }

        private static TargetType GetTargetType(GridEntity originEntity, GridEntity targetEntity) {
            if (targetEntity.MyTeam == Team.Neutral || originEntity.MyTeam == Team.Neutral) {
                return TargetType.Neutral;
            }

            return originEntity.MyTeam == targetEntity.MyTeam ? TargetType.Ally : TargetType.Enemy;
        }

        public bool CanUseAbility(IAbilityData data, bool ignoreBlockingTimers) {
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
            if (!ignoreBlockingTimers && ActiveTimers.Any(t => t.ChannelBlockers.Contains(data.Channel) && !t.Expired)) {
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

        public void ExpireTimerForAbility(IAbility ability, bool canceled) {
            // Find the timer with the indicated ability. The timers themselves are not synchronized, but since their abilities are we can use those. 
            AbilityCooldownTimer cooldownTimer = ActiveTimers.FirstOrDefault(t => t.Ability.UID == ability.UID);
            if (cooldownTimer == null) {
                // The ability timer must have already expired
                return;
            }

            cooldownTimer.Expire();
            ActiveTimers.Remove(cooldownTimer);
            CooldownTimerExpiredEvent?.Invoke(ability, cooldownTimer);
            if (!canceled && ability.AbilityData.AnimateWhenCooldownComplete) {
                PerformAnimationEvent?.Invoke(ability, cooldownTimer);
            }
        }

        /// <summary>
        /// Add time to the movement cooldown timer due to another ability being performed.
        /// If there is an active cooldown timer, then this amount is added to that timer.
        /// Otherwise, a new cooldown timer is added with this amount.
        /// </summary>
        public void AddMovementTime(float timeToAdd) {
            AbilityDataScriptableObject moveAbilityScriptable = Abilities.FirstOrDefault(a => a.Content is MoveAbilityData);
            if (moveAbilityScriptable == null) return;
            Vector2Int? location = Location;
            if (location == null) return;
            
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
                Destination = location.Value,
                NextMoveCell = location.Value,
                SelectorTeam = MyTeam
            }, this), timeToAdd);
        }

        private void Update() {
            List<AbilityCooldownTimer> activeTimersCopy = new List<AbilityCooldownTimer>(ActiveTimers);
            activeTimersCopy.ForEach(t => t.UpdateTimer(Time.deltaTime));
        }

        public bool PerformAbility(IAbilityData abilityData, IAbilityParameters parameters, bool queueIfNotLegal, bool clearQueueFirst = true) {
            if (!abilityData.AbilityLegal(parameters, this)) {
                if (queueIfNotLegal) {
                    // We specified to perform the ability now, but we can't legally do that. So queue it. 
                    QueueAbility(abilityData, parameters, true, true, false);
                    if (abilityData is not AttackAbilityData) {
                        // Clear the targeted entity since we are telling this entity to do something else
                        LastAttackedEntity = null;
                    }
                    return true;
                } else {
                    AbilityFailed(abilityData);
                    return false;
                }
            }
            
            if (abilityData is not AttackAbilityData) {
                // Clear the targeted entity since we are telling this entity to do something else
                LastAttackedEntity = null;
            }
            
            IAbility abilityInstance = abilityData.CreateAbility(parameters, this);
            abilityInstance.WaitUntilLegal = queueIfNotLegal;
            GameManager.Instance.CommandManager.PerformAbility(abilityInstance, clearQueueFirst);
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
        /// Auto-select any abilities that we have configured as auto-selectable.
        /// This probably won't behave well if this entity has multiple abilities configured as auto-selectable... 
        /// </summary>
        public void PerformAutoSelection() {
            foreach (IAbilityData abilityData in Abilities.Select(a => a.Content).Where(a => a.AutoSelect)) {
                abilityData.SelectAbility(this);
            }
        }
        
        public void PerformOnStartAbilities() {
            foreach (IAbilityData abilityData in Abilities.Select(a => a.Content).Where(a => a.PerformOnStart)) {
                PerformAbility(abilityData, new NullAbilityParameters(), abilityData.RepeatForeverAfterStartEvenWhenFailed);
            }
        }

        public void PerformDefaultAbility() {
            if (!EntityData.AttackByDefault) return;
            Vector2Int? location = Location;
            if (location == null) return;
            
            AttackAbilityData data = (AttackAbilityData) EntityData.Abilities
                .FirstOrDefault(a => a.Content is AttackAbilityData)?.Content;
            if (data == null) return;
            
            PerformAbility(data, new AttackAbilityParameters {
                TargetFire = false,
                Destination = location.Value
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

            if (SlowTiles.Contains(tile)) {
                return EntityData.SlowMoveTime;
            }

            return EntityData.NormalMoveTime;
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
            sourceEntity.LastAttackedEntity = this;
            if (Location == null) {
                Debug.LogWarning("Entity received attack but it is not registered or unregistered");
                return;
            }
            
            // TODO Could consider attack-moving to the target location if no abilities are queued and configured to attack by default.
            // Necessary so that the entity doesn't just sit there if attacked by something outside of its range. 

            // Get base damage
            float damage = sourceEntity.Damage;
            
            // Apply any additive attack modifiers based on tags
            damage += sourceEntity.EntityData.TagsToApplyBonusDamageTo.Any(t => EntityData.Tags.Contains(t))
                ? sourceEntity.EntityData.BonusDamage
                : 0;
            
            // Apply any multiplicative defense modifiers from terrain
            damage *= CurrentTileType!.GetDefenseModifier(EntityData);

            // Apply any multiplicative defense modifiers from structures (as long as this is not a structure)
            if (!EntityData.IsStructure) {
                List<GridEntity> structuresAtLocation = GameManager.Instance.CommandManager.EntitiesOnGrid.EntitiesAtLocation(Location.Value)?.Entities
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
            if (CurrentHP == MaxHP) return;

            int newHP = CurrentHP + healAmount;
            newHP = Mathf.Min(newHP, MaxHP);
            SetCurrentHP(newHP, true);
        }

        // Server flag?
        private bool _markedForDeath;
        // Client flag
        private bool _unregistered;
        public bool DeadOrDying() {
            return this == null || _markedForDeath || _unregistered;
        }
        
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

            _unregistered = true;
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