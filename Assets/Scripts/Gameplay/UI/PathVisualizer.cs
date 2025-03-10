using System.Collections.Generic;
using Gameplay.Grid;
using Gameplay.Pathfinding;
using UnityEngine;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// Handles creating (from a pool) a bunch of directional arrows pointing from tile to tile to visualize a path
    /// </summary>
    public class PathVisualizer : MonoBehaviour {
        [SerializeField] private DirectionalLine _directionalLinePrefab;
        [SerializeField] private Transform _lineBucket;
        [SerializeField] private int _poolSize;

        private GameObjectPool<DirectionalLine> _linePool;
        private readonly List<DirectionalLine> _currentlyDisplayedLines = new List<DirectionalLine>();

        private GridController GridController => GameManager.Instance.GridController;
        private PathfinderService PathfinderService => GameManager.Instance.PathfinderService;

        public void Initialize() {
            _linePool = new GameObjectPool<DirectionalLine>(_directionalLinePrefab, _lineBucket, _poolSize);
        }

        /// <summary>
        /// Lay out a set of <see cref="DirectionalLine"/>s along a path
        /// </summary>
        public void Visualize(PathfinderService.Path path, bool attack, bool hidePathDestination) {
            ClearPath();
            List<GridNode> pathNodes = path.Nodes;
            
            // If the path is too short, then no need to place any lines
            if (pathNodes.Count < 1 
                || (pathNodes.Count < 2 && path.ContainsRequestedDestination) 
                || (pathNodes.Count < 3 && hidePathDestination)) return;
            
            // No need to visualise for the final cell
            float currentAngle = -1f;
            for (int i = 0; i < pathNodes.Count - 1; i++) {
                DirectionalLine line = _linePool.GetObject();
                _currentlyDisplayedLines.Add(line);
                
                // Reveal the line and move it in place
                line.gameObject.SetActive(true);
                line.SetColor(attack);
                line.transform.position = GridController.GetWorldPosition(pathNodes[i].Location);
                
                // Rotate the line to link to the next cell in the path
                var previousAngle = currentAngle;
                currentAngle = PathfinderService.AngleBetweenCells(pathNodes[i].Location, pathNodes[i + 1].Location);
                line.SetRotation(currentAngle);
                
                // Hide/adjust parts of the line if this is the first or last cell in the path. 
                if (i == 0) {
                    line.SetMask(DirectionalLine.LineType.StartHalf);
                    if (pathNodes.Count == 2) {
                        // This is the only line being displayed, so we should show the destination icon. 
                        line.ShowDestinationIcon(attack);
                    }
                } else if (i == pathNodes.Count - 2) {
                    // This is the last node we care about visualizing
                    if (hidePathDestination) {
                        line.SetMask(DirectionalLine.LineType.EndHalf);
                    } else {
                        line.SetMask(DirectionalLine.LineType.Full);
                        line.ShowDestinationIcon(attack);
                    }
                    
                    // Hide/show the previous line's dot if it has a different angle than this one.
                    _currentlyDisplayedLines[i-1].ToggleEndDot(!Mathf.Approximately(currentAngle, previousAngle));
                } else {
                    line.SetMask(DirectionalLine.LineType.Full);
                    // Hide/show the previous line's dot if it has a different angle than this one.
                    _currentlyDisplayedLines[i-1].ToggleEndDot(!Mathf.Approximately(currentAngle, previousAngle));
                }
            }
        }

        public void ClearPath() {
            foreach (DirectionalLine line in _currentlyDisplayedLines) {
                line.Discard();
                _linePool.AddAndHideObject(line);
            }
            _currentlyDisplayedLines.Clear();
        }
    }
}