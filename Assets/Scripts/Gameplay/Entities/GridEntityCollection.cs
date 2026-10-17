using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using JetBrains.Annotations;
using Mirror;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// A collection of <see cref="GridEntity"/> instances and their locations on a grid.
    /// </summary>
    [Serializable]
    public class GridEntityCollection {
        /// <summary>
        /// Represents a <see cref="GridEntity"/> and its order (relative to other entities at the same location)
        /// </summary>
        [Serializable]
        public class OrderedGridEntity {
            public GridEntity Entity;
            public int Order;
        }
        
        /// <summary>
        /// Represents a set of <see cref="GridEntity"/>s at a particular location.
        /// </summary>
        [Serializable]
        public class PositionedGridEntityCollection {
            public List<OrderedGridEntity> Entities;
            public Vector2Int Location;

            public OrderedGridEntity GetTopEntity() {
                return Entities.IsNullOrEmpty() ? null : Entities.OrderByDescending(o => o.Order).ToArray()[0];
            }

            [CanBeNull]
            public OrderedGridEntity GetEntityAfter(OrderedGridEntity previousEntity) {
                if (Entities.IsNullOrEmpty()) return null;
                List<OrderedGridEntity> orderedList = Entities.OrderByDescending(o => o.Order).ToList();
                OrderedGridEntity nextEntity = orderedList.FirstOrDefault(o => o.Order < previousEntity.Order);
                if (nextEntity == null) {
                    // We must have gone through the whole entity stack, so loop back around to the top
                    return orderedList[0];
                }
                return nextEntity;
            }
        }

        public readonly List<PositionedGridEntityCollection> Entities = new List<PositionedGridEntityCollection>();
        
        // Entities that are not actually on the grid, but still registered (i.e. being carried by something)
        private readonly List<GridEntity> _entitiesOffGrid = new List<GridEntity>();

        /// <summary>
        /// Event that triggers when an entity is registered or unregistered at a particular location.
        /// </summary>
        public event Action<Vector2Int, GridEntity> EntityUpdatedEvent;
        
        #region Entity Operations
        
        public void RegisterEntity(GridEntity entity, Vector2Int location, int order, GridEntity entityToIgnore = null, bool checkLegality = true) {
            // Check to see if the entity is already registered
            if (Entities.SelectMany(c => c.Entities.Select(o => o.Entity)).Contains(entity)) return;

            PositionedGridEntityCollection collectionAtLocation = EntitiesAtLocation(location);
            List<OrderedGridEntity> currentEntitiesAtLocation = collectionAtLocation?.Entities;
            if (currentEntitiesAtLocation == null) {
                // Make a new list since there are currently no entities at the location
                Entities.Add(new PositionedGridEntityCollection {
                    Entities = new List<OrderedGridEntity> {
                        new OrderedGridEntity {
                            Entity = entity,
                            Order = order
                        }
                    },
                    Location = location
                });
                ClearLocationsWithFriendlyEntitiesCache();
                EntityUpdatedEvent?.Invoke(location, entity);
            } else if (!checkLegality || CanEntityShareLocation(entity, collectionAtLocation, entityToIgnore)) {
                if (currentEntitiesAtLocation.Any(o => o.Order == order)) {
                    Debug.LogWarning("I see that you're registering an entity in a location that contains another entity with the same order value. Hmmmm this might not behave super well you know, be careful out there!");
                }
                currentEntitiesAtLocation.Add(new OrderedGridEntity {
                    Entity = entity,
                    Order = order
                });
                ClearLocationsWithFriendlyEntitiesCache();
                EntityUpdatedEvent?.Invoke(location, entity);
            } else {
                throw new IllegalEntityPlacementException(location, entity, currentEntitiesAtLocation);
            }
        }

        public void UnRegisterEntity(GridEntity entity) {
            PositionedGridEntityCollection collection = Entities.FirstOrDefault(c => c.Entities.Any(o => o.Entity == entity));
            if (collection == null) {
                Debug.LogError("Attempted to unregister an entity that is not registered");
                return;
            }

            OrderedGridEntity orderedEntity = collection.Entities.FirstOrDefault(c => c.Entity == entity);
            collection.Entities.Remove(orderedEntity);
            
            // If this was the last entity at its location, remove the positioned collection from the list
            if (collection.Entities.Count == 0) {
                Entities.Remove(collection);
            }
            
            // Send the event with whatever is left over here, if anything
            ClearLocationsWithFriendlyEntitiesCache();
            EntityUpdatedEvent?.Invoke(collection.Location, collection.Entities.Count == 0 ? null : collection.Entities[0].Entity);
        }

        public void MoveEntity(GridEntity entity, Vector2Int newLocation) {
            int order = Entities.SelectMany(c => c.Entities).First(o => o.Entity == entity).Order;
            PositionedGridEntityCollection destinationCollection = Entities.FirstOrDefault(c => c.Location == newLocation);

            // Check to see if we can actually do this move
            if (!CanEntityShareLocation(entity, destinationCollection)) {
                throw new IllegalEntityPlacementException(newLocation, entity, destinationCollection?.Entities);
            }
            
            // Unregister and re-register
            UnRegisterEntity(entity);
            RegisterEntity(entity, newLocation, order);
        }

        public void RemoveEntityFromGrid(GridEntity entity) {
            if (entity == null || entity.DeadOrDying) return;
            if (_entitiesOffGrid.Contains(entity)) {
                Debug.LogError("Attempted to remove an entity that is already off the grid");
                return;
            }
            if (!IsEntityOnGrid(entity)) {
                Debug.LogError("Attempted to remove an entity that is not on the grid");
                return;
            }
            
            // Remove
            PositionedGridEntityCollection collection = Entities.FirstOrDefault(c => c.Entities.Any(o => o.Entity == entity));
            OrderedGridEntity orderedEntity = collection!.Entities.FirstOrDefault(c => c.Entity == entity);
            collection.Entities.Remove(orderedEntity);
            
            // If this was the last entity at its location, remove the positioned collection from the list
            if (collection.Entities.Count == 0) {
                Entities.Remove(collection);
            }
            
            // Add to off-grid collection
            _entitiesOffGrid.Add(entity);
            
            // Send the event with whatever is left over here, if anything
            ClearLocationsWithFriendlyEntitiesCache();
            EntityUpdatedEvent?.Invoke(collection.Location, collection.Entities.Count == 0 ? null : collection.Entities[0].Entity);
        }

        /// <summary>
        /// Does not check for placement legality!
        /// </summary>
        public void PutEntityBackOnGrid(GridEntity entity, Vector2Int location) {
            if (entity == null || entity.DeadOrDying) return;
            if (!_entitiesOffGrid.Contains(entity)) {
                Debug.LogError("Attempted to place an entity that is not off the grid");
                return;
            }
            if (IsEntityOnGrid(entity)) {
                Debug.LogError("Attempted to place an entity that is already on the grid");
                return;
            }

            int order = entity.EntityData.GetStackOrder();
            
            // Remove
            _entitiesOffGrid.Remove(entity);

            // Place
            PositionedGridEntityCollection collectionAtLocation = EntitiesAtLocation(location);
            List<OrderedGridEntity> currentEntitiesAtLocation = collectionAtLocation?.Entities;
            if (currentEntitiesAtLocation == null) {
                // Make a new list since there are currently no entities at the location
                Entities.Add(new PositionedGridEntityCollection {
                    Entities = new List<OrderedGridEntity> {
                        new OrderedGridEntity {
                            Entity = entity,
                            Order = order
                        }
                    },
                    Location = location
                });
                ClearLocationsWithFriendlyEntitiesCache();
                EntityUpdatedEvent?.Invoke(location, entity);
            } else {
                if (currentEntitiesAtLocation.Any(o => o.Order == order)) {
                    Debug.LogWarning("I see that you're registering an entity in a location that contains another entity with the same order value. Hmmmm this might not behave super well you know, be careful out there!");
                }

                currentEntitiesAtLocation.Add(new OrderedGridEntity {
                    Entity = entity,
                    Order = order
                });
                ClearLocationsWithFriendlyEntitiesCache();
                EntityUpdatedEvent?.Invoke(location, entity);
            }
        }
        
        #endregion
        
        public void ApplyEntityUpdate(GridEntity entity, GridEntityCollectionUpdate updateType, Vector2Int previousLocation, Vector2Int newLocation) {
            switch (updateType) {
                case GridEntityCollectionUpdate.Register:
                    RegisterEntity(entity, newLocation, entity.EntityData.GetStackOrder(), null, false);
                    break;
                case GridEntityCollectionUpdate.Unregister:
                    UnRegisterEntity(entity);
                    break;
                case GridEntityCollectionUpdate.Move:
                    MoveEntity(entity, newLocation);
                    break;
                case GridEntityCollectionUpdate.RemoveFromGrid:
                    RemoveEntityFromGrid(entity);
                    break;
                case GridEntityCollectionUpdate.PutBackOnGrid:
                    PutEntityBackOnGrid(entity, newLocation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateType), updateType, null);
            }
        }

        /// <summary>
        /// The <see cref="GridEntity"/>s at the given location, or null if no entity exists there.
        /// </summary>
        [CanBeNull]
        public PositionedGridEntityCollection EntitiesAtLocation(Vector2Int location) {
            return Entities.FirstOrDefault(e => e.Location == location);
        }

        /// <summary>
        /// Gets the current location of the given <see cref="GridEntity"/>
        /// </summary>
        /// <exception cref="GridEntityNotPresentException">Thrown if the entity was not found in this collection</exception>
        public Vector2Int? LocationOfEntity(GridEntity entity) {
            PositionedGridEntityCollection entry = Entities.FirstOrDefault(e => e.Entities.Any(o => o.Entity == entity));
            return entry?.Location;
        }

        public List<GridEntity> ActiveEntitiesForTeam(GameTeam team, bool justTop = false) {
            return AllEntities(justTop).Where(e => e.Team == team).ToList();
        }

        public List<GridEntity> AllEntities(bool justTop = false) {
            if (justTop) {
                return Entities.Select(c => c.GetTopEntity()?.Entity).NotNull().ToList();
            }
            return Entities.SelectMany(c => c.Entities)
                .Select(o => o.Entity)
                .ToList();
        }

        public bool IsEntityOnGrid(GridEntity entity) {
            return AllEntities().Contains(entity);
        }

        public GridEntity GetEntityByID(long entityID) {
            return AllEntities().FirstOrDefault(e => e.UID == entityID);
        }

        private Dictionary<GameTeam, List<Vector2Int>> _locationsWithFriendlyEntities = new Dictionary<GameTeam, List<Vector2Int>>();
        public List<Vector2Int> LocationsWithFriendlyEntities(GameTeam team) {
            if (_locationsWithFriendlyEntities.TryGetValue(team, out var entities)) {
                return entities;
            }
            
            List<Vector2Int> locationsWithFriendlyEntities = GameManager.Instance.CommandManager.EntitiesOnGrid
                .ActiveEntitiesForTeam(team)
                .Select(e => e.Location)
                .NotNull()
                .Select(l => l!.Value)
                .ToList();
            _locationsWithFriendlyEntities[team] = locationsWithFriendlyEntities;
            return locationsWithFriendlyEntities;
        }

        private void ClearLocationsWithFriendlyEntitiesCache() {
            foreach (List<Vector2Int> locations in _locationsWithFriendlyEntities.Values) {
                locations.Clear();
            }
            _locationsWithFriendlyEntities.Clear();
        }

        private bool CanEntityShareLocation(GridEntity entity, PositionedGridEntityCollection otherEntities, GridEntity entityToIgnore = null) {
            if (otherEntities == null) return true;
            if (otherEntities.Entities.IsNullOrEmpty()) return true;
            List<GridEntity> entitiesToIgnore = entityToIgnore != null ? new List<GridEntity> {entityToIgnore} : null;
            if (PathfinderService.CanEntityEnterCell(otherEntities.Location, entity.EntityData, entity.Team, entitiesToIgnore)) return true;
            return false;
        }
        
        private class GridEntityNotPresentException : Exception {
            public GridEntityNotPresentException(GridEntity entity) : base($"{nameof(GridEntity)} {entity.UnitName}: Entity not found in collection") { }
        }

        private class IllegalEntityPlacementException : Exception {
            public IllegalEntityPlacementException(Vector2Int location, 
                GridEntity moveAttemptEntity, 
                IEnumerable<OrderedGridEntity> entitiesAtLocation) 
                : base($"Failed to place {nameof(GridEntity)} ({moveAttemptEntity.UnitName}) at location {location}"
                       + $" because other {nameof(GridEntity)}s at that location conflict with this one. Other entities: {entitiesAtLocation.Aggregate("", (current, o) => current + o.Entity.DisplayName + ", ")}") { }
        }
    }
}

public static class PositionedGridEntityCollectionExtensions {
    public static GridEntityCollection.PositionedGridEntityCollection Clone(this GridEntityCollection.PositionedGridEntityCollection other) {
        List<GridEntityCollection.OrderedGridEntity> orderedEntities = other.Entities.Select(clonedEntity => new GridEntityCollection.OrderedGridEntity {
            Entity = clonedEntity.Entity, Order = clonedEntity.Order,
        }).ToList();
        return new GridEntityCollection.PositionedGridEntityCollection {
            Entities = orderedEntities,
            Location = other.Location
        };
    }
}
