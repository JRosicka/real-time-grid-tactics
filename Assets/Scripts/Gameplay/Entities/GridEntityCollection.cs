using System;
using System.Collections.Generic;
using System.Linq;
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

        public readonly List<PositionedGridEntityCollection> Entities;

        /// <summary>
        /// Event that triggers when an entity is register or unregistered at a particular location.
        /// Static since <see cref="GridEntityCollection"/> constantly gets recreated. 
        /// </summary>
        public static event Action<Vector2Int, GridEntity> EntityUpdatedEvent;

        public GridEntityCollection() : this(new List<PositionedGridEntityCollection>()) { }

        public GridEntityCollection(List<PositionedGridEntityCollection> entities) {
            Entities = entities;
        }
        
        public void RegisterEntity(GridEntity entity, Vector2Int location, int order, GridEntity entityToIgnore = null) {
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
            } else if (CanEntityShareLocation(entity, collectionAtLocation, entityToIgnore)) {
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
                Debug.LogWarning("Attempted to unregister an entity that is not registered");
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

        /// <summary>
        /// The <see cref="GridEntity"/>s at the given location, or null if no entity exists there.
        /// </summary>
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

    #region Serializers
    
    public static class GridEntityCollectionSerializer {
        public static void WriteGridEntityCollection(this NetworkWriter writer, GridEntityCollection collection) {
            writer.Write(collection.Entities);
        }

        public static GridEntityCollection ReadGridEntityCollection(this NetworkReader reader) {
            return new GridEntityCollection(reader.Read<List<GridEntityCollection.PositionedGridEntityCollection>>());
        }
    }

    public static class PositionedGridEntitySerializer {
        public static void WritePositionedGridEntity(this NetworkWriter writer,
            GridEntityCollection.PositionedGridEntityCollection entityCollection) {
            writer.Write(entityCollection.Entities);
            writer.Write(entityCollection.Location);
        }
        
        public static GridEntityCollection.PositionedGridEntityCollection ReadPositionedGridEntity(this NetworkReader reader) {
            return new GridEntityCollection.PositionedGridEntityCollection {
                Entities = reader.Read<List<GridEntityCollection.OrderedGridEntity>>(),
                Location = reader.Read<Vector2Int>()
            };
        }
    }

    public static class PositionedGridEntityListSerializer {
        public static void WritePositionedGridEntityList(this NetworkWriter writer,
            List<GridEntityCollection.PositionedGridEntityCollection> list) {
            writer.Write(list.Count);
            foreach (GridEntityCollection.PositionedGridEntityCollection entity in list) {
                writer.Write(entity);
            }
        }

        public static List<GridEntityCollection.PositionedGridEntityCollection> ReadPositionedGridEntityList(this NetworkReader reader) {
            List<GridEntityCollection.PositionedGridEntityCollection> ret = new List<GridEntityCollection.PositionedGridEntityCollection>();
            int collectionSize = reader.ReadInt();
            for (int i = 0; i < collectionSize; i++) {
                ret.Add(reader.Read<GridEntityCollection.PositionedGridEntityCollection>());
            }

            return ret;
        }
    }

    public static class OrderedGridEntityListSerializer {
        public static void WriteOrderedGridEntityList(this NetworkWriter writer,
            List<GridEntityCollection.OrderedGridEntity> list) {
            writer.Write(list.Count); 
            foreach (GridEntityCollection.OrderedGridEntity entity in list) {
                writer.Write(entity);
            }
        }

        public static List<GridEntityCollection.OrderedGridEntity> ReadOrderedGridEntityList(this NetworkReader reader) {
            List<GridEntityCollection.OrderedGridEntity> ret = new List<GridEntityCollection.OrderedGridEntity>();
            int collectionSize = reader.ReadInt();
            for (int i = 0; i < collectionSize; i++) {
                ret.Add(reader.Read<GridEntityCollection.OrderedGridEntity>());
            }

            return ret;
        }
    }

    public static class OrderedGridEntitySerializer {
        public static void WriteOrderedGridEntity(this NetworkWriter writer, GridEntityCollection.OrderedGridEntity orderedEntity) {
            writer.Write(orderedEntity.Entity);
            writer.Write(orderedEntity.Order);
        }

        public static GridEntityCollection.OrderedGridEntity ReadOrderedGridEntity(this NetworkReader reader) {
            return new GridEntityCollection.OrderedGridEntity {
                Entity = reader.Read<GridEntity>(),
                Order = reader.ReadInt()
            };
        }
    }
    
    #endregion
}