using System.Collections.Generic;
using System.Linq;
using Gameplay.Grid;
using Gameplay.Pathfinding;
using UnityEngine;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// Handles creating (from a pool) a bunch of directional arrows pointing from tile to tile to visualize a path.
    /// - First draws modular "regular" arrows along the defined path from node to node
    /// - Then draws a thin straight path from the last node to the intended target cell to indicate the remainder of
    ///   the path that the unit can not currently travel
    /// </summary>
    public class PathVisualizer : MonoBehaviour {
        public enum PathType {
            Move = 0,
            AttackMove = 1,
            TargetAttack = 2
        }
        
        [SerializeField] private AbstractDirectionalLine _directionalLinePrefab;
        [SerializeField] private AbstractDirectionalLine _thickDirectionalLinePrefab;
        [SerializeField] private Transform _lineBucket;
        [SerializeField] private int _directionalLinePoolSize;
        [SerializeField] private int _thickDirectionalLinePoolSize;
        [SerializeField] private StraightDirectionalLine _straightDirectionalLine;

        private GameObjectPool<AbstractDirectionalLine> _linePool;
        private GameObjectPool<AbstractDirectionalLine> _thickLinePool;
        private readonly Dictionary<string, List<AbstractDirectionalLine>> _currentlyDisplayedLines = new();

        private GridController GridController => GameManager.Instance.GridController;
        private PathfinderService PathfinderService => GameManager.Instance.PathfinderService;

        public void Initialize() {
            _linePool = new GameObjectPool<AbstractDirectionalLine>(_directionalLinePrefab, _lineBucket, _directionalLinePoolSize);
            _thickLinePool = new GameObjectPool<AbstractDirectionalLine>(_thickDirectionalLinePrefab, _lineBucket, _thickDirectionalLinePoolSize);
            _straightDirectionalLine.Initialize();
        }

        /// <summary>
        /// Lay out a set of <see cref="AbstractDirectionalLine"/>s along a path
        /// </summary>
        public void Visualize(PathfinderService.Path path, PathType pathType, Vector2Int targetLocation, bool hidePathDestination, bool thickLines) {
            ClearPath(thickLines);
            
            // If the path is too short, then no need to place any lines
            if (path.Nodes.Count < 1 
                || (path.Nodes.Count < 2 && path.ContainsRequestedDestination)) return;
            
            VisualizeRegularLines(path, pathType, hidePathDestination, thickLines);
            if (!thickLines) {
                VisualizeStraightLine(path, pathType, targetLocation, hidePathDestination);
            }
        }
        
        private void VisualizeRegularLines(PathfinderService.Path path, PathType pathType, bool hidePathDestination, bool thickLines) {
            List<GridNode> pathNodes = path.Nodes;
            if (pathNodes.Count < 3 && hidePathDestination && path.ContainsRequestedDestination) return;

            (List<AbstractDirectionalLine> lines, GameObjectPool<AbstractDirectionalLine> linePool) = GetLineGroup(thickLines);
            
            // No need to visualise for the final cell
            float currentAngle = -1f;
            for (int i = 0; i < pathNodes.Count - 1; i++) {
                AbstractDirectionalLine line = linePool.GetObject();
                lines.Add(line);
                
                // Reveal the line and move it in place
                line.gameObject.SetActive(true);
                line.SetColor(pathType);
                line.transform.position = GridController.GetWorldPosition(pathNodes[i].Location);
                
                // Rotate the line to link to the next cell in the path
                var previousAngle = currentAngle;
                currentAngle = PathfinderService.AngleBetweenCells(pathNodes[i].Location, pathNodes[i + 1].Location);
                line.SetRotation(currentAngle);
                
                // Hide/adjust parts of the line if this is the first or last cell in the path. 
                if (i == 0) {
                    line.SetMask(AbstractDirectionalLine.LineType.StartHalf);
                    if (pathNodes.Count == 2) {
                        // This is the only line being displayed, so we should show the destination icon. 
                        line.ShowDestinationIcon(pathType);
                    }
                } else if (i == pathNodes.Count - 2) {
                    // This is the last node we care about visualizing
                    if (hidePathDestination && path.ContainsRequestedDestination) {
                        line.SetMask(AbstractDirectionalLine.LineType.EndHalf);
                    } else {
                        line.SetMask(AbstractDirectionalLine.LineType.Full);
                        line.ShowDestinationIcon(pathType);
                    }
                    
                    // Hide/show the previous line's dot if it has a different angle than this one.
                    lines[i-1].ToggleEndDot(!Mathf.Approximately(currentAngle, previousAngle));
                } else {
                    line.SetMask(AbstractDirectionalLine.LineType.Full);
                    // Hide/show the previous line's dot if it has a different angle than this one.
                    lines[i-1].ToggleEndDot(!Mathf.Approximately(currentAngle, previousAngle));
                }
            }
        }

        /// <summary>
        /// Handles drawing a straight line from the final node in the path to the target location. Visualizes the
        /// remainder of the path that currently can not be traversed. 
        /// </summary>
        private void VisualizeStraightLine(PathfinderService.Path path, PathType pathType, Vector2Int targetLocation, bool hidePathDestination) {
            List<GridNode> pathNodes = path.Nodes;
            Vector2Int startLocation = pathNodes[0].Location;
            Vector2Int finalNodeLocation = pathNodes.Last().Location;
            
            // The regular-lines portion of the path gets all the way to the end, so nothing more to draw here
            if (pathNodes.Last().Location == targetLocation) return;

            bool includeTargetNode = pathType != PathType.TargetAttack && !hidePathDestination;
            
            // If the entity is at the end of the path and adjacent to the destination AND we don't want to draw on the last node, then nothing to do
            if (startLocation == finalNodeLocation && !includeTargetNode && CellDistanceLogic.DistanceBetweenCells(finalNodeLocation, targetLocation) == 1) return;
            
            // Figure out where the base line should be (middle of final cell to middle of target cell)
            GridController gridController = GameManager.Instance.GridController;
            Vector2 lineStartLocation = gridController.GetWorldPosition(finalNodeLocation);
            Vector2 lineEndLocation = gridController.GetWorldPosition(targetLocation);
            _straightDirectionalLine.SetLine(lineStartLocation, lineEndLocation, pathType);
            
            if (startLocation == finalNodeLocation) {
                // The entity is at the final cell in the path, so draw from the edge of the final cell instead of the middle
                _straightDirectionalLine.MaskOrigin();
            }
            
            if (!includeTargetNode) {
                // We only want to go to the edge of the target cell instead of the middle
                _straightDirectionalLine.MaskEnd();
            }
        }

        public void ClearPath(bool thickLines) {
            (List<AbstractDirectionalLine> lines, GameObjectPool<AbstractDirectionalLine> linePool) = GetLineGroup(thickLines);
            foreach (AbstractDirectionalLine line in lines) {
                line.Discard();
                linePool.AddAndHideObject(line);
            }
            lines.Clear();

            if (!thickLines) {
                _straightDirectionalLine.ClearLine();
            }
        }

        private (List<AbstractDirectionalLine>, GameObjectPool<AbstractDirectionalLine>) GetLineGroup(bool thickLines) {
            string key = thickLines ? "thickLines" : "defaultLines";
            if (!_currentlyDisplayedLines.TryGetValue(key, out List<AbstractDirectionalLine> lineGroup)) {
                _currentlyDisplayedLines.Add(key, lineGroup = new List<AbstractDirectionalLine>());
            }
            GameObjectPool<AbstractDirectionalLine> linePool = thickLines ? _thickLinePool : _linePool;
            return (lineGroup, linePool);
        }
    }
}