using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Grid;
using Gameplay.Pathfinding;
using UnityEngine;

/// <summary>
/// Service for finding paths, and also for finding movement costs and restrictions between tiles on the grid
/// </summary>
public class PathfinderService {
    private GridController GridController => GameManager.Instance.GridController;

    // TODO we need a failsafe in case a path is not possible
    /// <summary>
    /// Find the shortest path between an entity and a destination. Uses a basic A* algorithm. Factors in movement costs
    /// per tile.
    /// </summary>
    /// <param name="entity">The entity to traverse the path. Matters for determining movement costs per tile. Also uses
    /// its current location as the path start.</param>
    /// <param name="destination">The location to make a path to</param>
    /// <returns>A path of nodes from the entity's location to the destination</returns>
    /// <exception cref="Exception">If the generated path is too long</exception>
    public List<GridNode> FindPath(GridEntity entity, Vector2Int destination) {
        if (!entity.CanEnterTile(GridController.GridData.GetCell(destination).Tile)) {
            // Can't go to destination, so no path
            return null;
        }
        
        GridNode startNode = new GridNode(entity, GridController.GridData.GetCell(entity.Location));

        List<GridNode> toSearch = new List<GridNode> { startNode };
        List<GridNode> processed = new List<GridNode>();

        while (toSearch.Any()) {
            GridNode current = toSearch[0];
            
            // Try to add the node with the lowest F cost, or the lowest H cost in the case of ties
            // TODO Replace this with a hashmap
            foreach (GridNode node in toSearch) {
                if (node.F < current.F || Mathf.Approximately(node.F, current.F) && node.H < current.H) {
                    current = node;
                }
            }
            
            processed.Add(current);
            toSearch.Remove(current);
            if (current.Location == destination) {
                // We have reached the end, so now construct and return the path
                GridNode currentPathNode = current;
                List<GridNode> path = new List<GridNode>();
                while (currentPathNode != startNode) {
                    path.Add(currentPathNode);
                    currentPathNode = currentPathNode.Connection;

                    if (path.Count > 500) {
                        throw new Exception("Frig bro, the path is too long");
                    }
                }
                path.Add(startNode);
                
                // Reverse the path so that it is in the correct order
                path.Reverse();
                return path;
            }

            // Search through all the current node's neighbors
            foreach (GridNode neighbor in current.Neighbors.Where(n => n.Walkable && processed.All(node => node.Location != n.Location))) {
                bool inSearch = toSearch.Any(node => node.Location == neighbor.Location);    // TODO this and above search seem inefficient
                float costToNeighbor = current.G + neighbor.CostToEnter();

                if (!inSearch || costToNeighbor < neighbor.G) {
                    // This neighbor has not been processed yet or the current path to it is better than a previously found path
                    neighbor.SetG(costToNeighbor);
                    neighbor.SetConnection(current);

                    if (!inSearch) {
                        // This is the first time we have taken a look at this node, so do some basic one-time setup
                        neighbor.SetH(neighbor.GetDistance(destination));
                        toSearch.Add(neighbor);
                    }
                }
            }
        }
        
        // We ran out of nodes to search without finding a way to the destination, so no path exists
        return null;
    }

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