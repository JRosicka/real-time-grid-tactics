using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Grid;
using UnityEngine;

public class PathfinderService {
    public struct GridPath {
        public List<Vector2Int> Cells;
    }

    private GridController GridController => GameManager.Instance.GridController;

    public int RequiredMoves(GridEntity entity, Vector2Int origin, Vector2Int destination) {
        Vector2Int pathVector = destination - origin;
        return Mathf.Abs(pathVector.x) + Mathf.Abs(pathVector.y);
    }

    public float AngleBetweenCells(Vector2Int cell1, Vector2Int cell2) {
        Vector2 cell1WorldPos = GridController.GetWorldPosition(cell1);
        Vector2 cell2WorldPos = GridController.GetWorldPosition(cell2);
        return Vector2.SignedAngle(Vector2.right, new Vector2(cell2WorldPos.x - cell1WorldPos.x, 
                                                        cell2WorldPos.y - cell1WorldPos.y));
    }
    
    // TODO repurpose this a bit - we need to factor in types. We will want to consider a similar method for calculating movement penalties as well. 
    public static bool CanEntityEnterCell(Vector2Int cellPosition, EntityData entityData, GridEntity.Team entityTeam, List<GridEntity> entitiesToIgnore = null) {
        entitiesToIgnore ??= new List<GridEntity>();
        List<GridEntity> entitiesAtLocation = GameManager.Instance.GetEntitiesAtLocation(cellPosition)?.Entities
            .Select(o => o.Entity).ToList();
        if (entitiesAtLocation == null) {
            // No other entities are here
            // TODO Check to see if tile type allows for this entity
            return true;
        }
        if (entitiesAtLocation.Any(e => e.MyTeam != entityTeam && e.MyTeam != GridEntity.Team.Neutral)) {
            // There are enemies here
            return false;
        }
        if (entitiesAtLocation.Any(e => !e.EntityData.FriendlyUnitsCanShareCell && !entitiesToIgnore.Contains(e))) {
            // Can only enter a friendly entity's cell if they are specifically configured to allow for that
            // or if we are set to ignore that entity.
            // Note that this means that structures can not be built on cells that contain units! This is intentional. 
            return false;
        }
        // So the only entities here do indeed allow for non-structures to share space with them. Still need to check if this is a structure. Can't put a structure on a structure!
        if (entityData.IsStructure && entitiesAtLocation.Any(e => e.EntityData.IsStructure)) {
            return false;
        }
        
        // TODO Check to see if tile type allows for this entity

        return true;
    }
}