using System;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using UnityEngine;

/// <summary>
/// Utility class for getting resource entities at the same location as the matching collection structure
/// </summary>
public class ResourceEntityFinder : MonoBehaviour {
    [SerializeField] private EntityData _villageEntityData;
    [SerializeField] private EntityData _villageSiteEntityData;
    [SerializeField] private EntityData _amberMineEntityData;
    [SerializeField] private EntityData _amberDepositEntityData;
    
    public GridEntity GetMatchingResourceEntity(GridEntity entityAtLocation, EntityData resourceCollector) {
        if (entityAtLocation.Location == null) {
            Debug.LogWarning($"{entityAtLocation.EntityData.ID} location is null!");
            return null;
        }

        var entities = GameManager.Instance.GetEntitiesAtLocation(entityAtLocation.Location.Value);
        if (entities == null) {
            Debug.LogWarning($"No entities found at location ({entityAtLocation.Location.Value})!");
            return null;
        }

        EntityData matchingResourceData = GetMatchingResourceData(resourceCollector);
        if (matchingResourceData == null) {
            return null;
        }
        
        GridEntity resourceEntity = entities.Entities.Select(o => o.Entity)
            .FirstOrDefault(e => e.EntityData.ID == matchingResourceData.ID);
        if (resourceEntity == null) {
            Debug.LogWarning("Matching resource entity not found!");
            return null;
        }

        return resourceEntity;
    }

    private EntityData GetMatchingResourceData(EntityData resourceCollector) {
        if (resourceCollector == _villageEntityData) {
            return _villageSiteEntityData;
        }
        if (resourceCollector == _amberMineEntityData) {
            return _amberDepositEntityData;
        }

        return null;
    }
}
