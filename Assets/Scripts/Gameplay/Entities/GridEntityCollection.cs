using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// A collection of <see cref="GridEntity"/> instances and their locations on a grid.
    /// </summary>
    [Serializable]
    public class GridEntityCollection {
        /// <summary>
        /// Represents a <see cref="GridEntity"/> and its location.
        /// </summary>
        [Serializable]
        public class PositionedGridEntity {
            public GridEntity Entity;
            public Vector2Int Location;
        }

        public readonly List<PositionedGridEntity> Entities;

        public GridEntityCollection() : this(new List<PositionedGridEntity>()) { }

        public GridEntityCollection(List<PositionedGridEntity> entities) {
            Entities = entities;
        }

        public void RegisterEntity(GridEntity entity, Vector2Int location) {
            if (Entities.Any(e => e.Entity == entity)) return;
            GridEntity currentEntityAtLocation = EntityAtLocation(location);
            if (currentEntityAtLocation != null) {
                throw new IllegalEntityPlacementException(location, entity, currentEntityAtLocation);
            }
            Entities.Add(new PositionedGridEntity {
                Entity = entity,
                Location = location
            });
        }

        public void UnRegisterEntity(GridEntity entity) {
            PositionedGridEntity entry = Entities.FirstOrDefault(e => e.Entity == entity);
            if (entry == null) {
                Debug.LogWarning("Attempted to unregister an entity that is not registered");
                return;
            }
            Entities.Remove(entry);
        }

        public void MoveEntity(GridEntity entity, Vector2Int newLocation) {
            GridEntity entityAtLocation = EntityAtLocation(newLocation);
            if (entityAtLocation != null) {
                throw new IllegalEntityPlacementException(newLocation, entity, entityAtLocation);
            }
            PositionedGridEntity entry = Entities.FirstOrDefault(e => e.Entity == entity);
            if (entry == null) {
                throw new GridEntityNotPresentException(entity);
            }

            entry.Location = newLocation;
            
        }

        /// <summary>
        /// The <see cref="GridEntity"/> at the given location, or null if no entity exists there.
        /// </summary>
        public GridEntity EntityAtLocation(Vector2Int location) {
            return Entities.FirstOrDefault(e => e.Location == location)?.Entity;
        }

        /// <summary>
        /// Gets the current location of the given <see cref="GridEntity"/>
        /// </summary>
        /// <exception cref="GridEntityNotPresentException">Thrown if the entity was not found in this collection</exception>
        public Vector2Int LocationOfEntity(GridEntity entity) {
            PositionedGridEntity entry = Entities.FirstOrDefault(e => e.Entity == entity);
            if (entry == null) {
                throw new GridEntityNotPresentException(entity);
            }
            
            return entry.Location;
        }
        
        private class GridEntityNotPresentException : Exception {
            public GridEntityNotPresentException(GridEntity entity) : base($"{nameof(GridEntity)} {entity.UnitName}: Entity not found in collection") { }
        }

        private class IllegalEntityPlacementException : Exception {
            public IllegalEntityPlacementException(Vector2Int location, 
                GridEntity moveAttemptEntity, 
                GridEntity entityAtLocation) 
                : base($"Failed to place {nameof(GridEntity)} ({moveAttemptEntity.UnitName}) at location {location}"
                       + $" because another {nameof(GridEntity)} ({entityAtLocation.UnitName}) already exists there") { }
        }
    }

    public static class GridEntityCollectionSerializer {
        public static void WriteGridEntityCollection(this NetworkWriter writer, GridEntityCollection collection) {
            writer.Write(collection.Entities);
        }

        public static GridEntityCollection ReadGridEntityCollection(this NetworkReader reader) {
            return new GridEntityCollection(reader.Read<List<GridEntityCollection.PositionedGridEntity>>());
        }
    }

    public static class PositionedGridEntitySerializer {
        public static void WritePositionedGridEntity(this NetworkWriter writer,
            GridEntityCollection.PositionedGridEntity entity) {
            writer.Write(entity.Entity);
            writer.Write(entity.Location);
        }
        
        public static GridEntityCollection.PositionedGridEntity ReadPositionedGridEntity(this NetworkReader reader) {
            return new GridEntityCollection.PositionedGridEntity {
                Entity = reader.Read<GridEntity>(),
                Location = reader.Read<Vector2Int>()
            };
        }
    }

    public static class PositionedGridEntityListSerializer {
        public static void WritePositionedGridEntityList(this NetworkWriter writer,
            List<GridEntityCollection.PositionedGridEntity> list) {
            foreach (GridEntityCollection.PositionedGridEntity entity in list) {
                writer.Write(entity);
            }
        }

        public static List<GridEntityCollection.PositionedGridEntity> ReadPositionedGridEntityList(this NetworkReader reader) {
            List<GridEntityCollection.PositionedGridEntity> ret = new List<GridEntityCollection.PositionedGridEntity>();
            while (reader.Remaining > 0) {
                ret.Add(reader.Read<GridEntityCollection.PositionedGridEntity>());
            }

            return ret;
        }
    }
}