using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Grid;
using Gameplay.Pathfinding;
using UnityEngine;
using Util;

/// <summary>
/// Service for finding paths, and also for finding movement costs and restrictions between tiles on the grid
/// </summary>
public class PathfinderService {
    // I would really hope that my maps aren't so big that a viable path can be found after searching through this many...
    private const int MaxCellsToSearch = 5000;
    private const int MaxCellsToSearchWhenWeKnowNoPathExists = 200;
    
    private GridController GridController => GameManager.Instance.GridController;

    /// <summary>
    /// Represents a path of nodes found by the pathfinder. All nodes in the path are guaranteed to be adjacent to their
    /// neighbors. 
    /// </summary>
    public struct Path {
        public List<GridNode> Nodes;
        /// <summary>
        /// Whether the destination node is the one that was actually requested when finding the path. If false, then
        /// the destination node is the best alternative we could find. 
        /// </summary>
        public bool ContainsRequestedDestination;
    }

    /// <summary>
    /// Find the shortest path between an entity and a destination. Uses a basic A* algorithm. Factors in movement costs
    /// per tile.
    ///
    /// If the destination can not be reached, then we return a path to the best legal alternative location. This is
    /// prioritized by lowest H-cost then by lowest G-cost. 
    /// </summary>
    /// <param name="entity">The entity to traverse the path. Matters for determining movement costs per tile. Also uses
    /// its current location as the path start.</param>
    /// <param name="destination">The location to make a path to</param>
    /// <returns>A path of nodes from the entity's location to the destination</returns>
    /// <exception cref="Exception">If the generated path is too long</exception>
    public Path FindPath(GridEntity entity, Vector2Int destination) {
        int maxSearch = MaxCellsToSearch;
        if (!entity.CanPathFindToTile(GridController.GridData.GetCell(destination).Tile) 
                || !CanEntityEnterCell(destination, entity.EntityData, entity.MyTeam)) {
            // Can't go to destination, so let's not overdo the search since we're just gonna pick the best available choice anyway
            maxSearch = MaxCellsToSearchWhenWeKnowNoPathExists;
        }
        
        GridNode startNode = new GridNode(entity, GridController.GridData.GetCell(entity.Location));
        startNode.SetH(startNode.GetDistance(destination));
        
        List<GridNode> toSearch = new List<GridNode> { startNode };
        List<GridNode> processed = new List<GridNode>();

        while (toSearch.Any()) {
            // We always add new items sorted by F cost then H cost, so the first element in the list will be the best 
            // choice for what to search next
            GridNode current = toSearch[0];
            
            processed.Add(current);
            toSearch.Remove(current);
            
            if (current.Location == destination) {
                // We have reached the end
                return ConstructPath(startNode, current, true);
            }
            
            if (processed.Count > maxSearch) {
                // We have not yet found a path after searching for a while, and we have not exhausted all of the tiles 
                // to search. Pick the best possible alternative destination out of those we have searched.
                return ConstructBestAlternativePath(processed, startNode);
            }

            // Search through all the current node's neighbors
            foreach (GridNode neighbor in current.Neighbors.Where(n => n.Walkable && processed.All(node => node.Location != n.Location))) {
                GridNode existingNeighbor = toSearch.FirstOrDefault(s => s.Location == neighbor.Location);
                bool inSearch = existingNeighbor != null;
                GridNode neighborBeingSearched = inSearch ? existingNeighbor : neighbor;
                float costToNeighbor = current.G + neighborBeingSearched.CostToEnter();

                if (!inSearch || costToNeighbor < existingNeighbor.G) {
                    // This neighbor has not been processed yet or the current path to it is better than a previously found path
                    neighborBeingSearched.SetG(costToNeighbor);
                    neighborBeingSearched.SetConnection(current);

                    if (!inSearch) {
                        // This is the first time we have taken a look at this node, so do some basic one-time setup
                        neighbor.SetH(neighbor.GetDistance(destination));
                        toSearch.AddSorted(neighbor);
                    } else {
                        // We just found a better connection for the neighbor, so re-sort the list
                        toSearch.Remove(neighborBeingSearched);
                        toSearch.AddSorted(neighborBeingSearched);
                    }
                }
            }
        }
        
        // We ran out of nodes to search without finding a way to the destination, so no path exists. Pick the best
        // possible alternative destination out of those we have searched.
        return ConstructBestAlternativePath(processed, startNode);
    }

    private Path ConstructPath(GridNode startNode, GridNode current, bool originalDestination) {
        GridNode currentPathNode = current;
        List<GridNode> pathNodes = new List<GridNode>();
        while (currentPathNode != startNode) {
            pathNodes.Add(currentPathNode);
            currentPathNode = currentPathNode.Connection;

            if (pathNodes.Count > 500) {
                throw new Exception("Frig bro, the path is too long");
            }
        }
        pathNodes.Add(startNode);
                
        // Reverse the path so that it is in the correct order
        pathNodes.Reverse();
        return new Path {
            Nodes = pathNodes,
            ContainsRequestedDestination = originalDestination
        };
    }

    private Path ConstructBestAlternativePath(IReadOnlyCollection<GridNode> processed, GridNode startNode) {
        GridNode bestAlternative;
                
        float minH = processed.Min(n => n.H);
        List<GridNode> closestNodes = processed.Where(n => Mathf.Approximately(n.H, minH)).ToList();
        if (closestNodes.Count > 1) {
            float minG = closestNodes.Min(n => n.G);
            bestAlternative = closestNodes.First(n => Mathf.Approximately(n.G, minG));
        } else {
            bestAlternative = closestNodes[0];
        }
        return ConstructPath(startNode, bestAlternative, false);
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
    public static bool CanEntityEnterCell(Vector2Int cellPosition, EntityData entityData, GridEntity.Team entityTeam, List<GridEntity> entitiesToIgnore = null, bool forRallying = false) {
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
        // So the only entities here do indeed allow for non-structures to share space with them.
        // Still need to check if this is a structure. Can't put a structure on a structure!
        // Though if this is for the purpose of determining whether a production structure can rally here, then ignore the 
        // fact that this is a structure
        if (!forRallying && entityData.IsStructure && entitiesAtLocation.Any(e => e.EntityData.IsStructure)) {
            return false;
        }
        
        // TODO Check to see if tile type allows for this entity

        return true;
    }
}