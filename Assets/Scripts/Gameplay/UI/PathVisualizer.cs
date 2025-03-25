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
        [SerializeField] private AbstractDirectionalLine _directionalLinePrefab;
        [SerializeField] private AbstractDirectionalLine _thickDirectionalLinePrefab;
        [SerializeField] private Transform _lineBucket;
        [SerializeField] private int _directionalLinePoolSize;
        [SerializeField] private int _thickDirectionalLinePoolSize;

        private GameObjectPool<AbstractDirectionalLine> _linePool;
        private GameObjectPool<AbstractDirectionalLine> _thickLinePool;
        private readonly Dictionary<string, List<AbstractDirectionalLine>> _currentlyDisplayedLines = new();

        private GridController GridController => GameManager.Instance.GridController;
        private PathfinderService PathfinderService => GameManager.Instance.PathfinderService;

        public void Initialize() {
            _linePool = new GameObjectPool<AbstractDirectionalLine>(_directionalLinePrefab, _lineBucket, _directionalLinePoolSize);
            _thickLinePool = new GameObjectPool<AbstractDirectionalLine>(_thickDirectionalLinePrefab, _lineBucket, _thickDirectionalLinePoolSize);
        }

        /// <summary>
        /// Lay out a set of <see cref="AbstractDirectionalLine"/>s along a path
        /// </summary>
        public void Visualize(PathfinderService.Path path, bool attack, bool hidePathDestination, bool thickLines) {
            ClearPath(thickLines);
            List<GridNode> pathNodes = path.Nodes;
            
            // If the path is too short, then no need to place any lines
            if (pathNodes.Count < 1 
                || (pathNodes.Count < 2 && path.ContainsRequestedDestination) 
                || (pathNodes.Count < 3 && hidePathDestination)) return;
            
            (List<AbstractDirectionalLine> lines, GameObjectPool<AbstractDirectionalLine> linePool) = GetLineGroup(thickLines);
            
            // No need to visualise for the final cell
            float currentAngle = -1f;
            for (int i = 0; i < pathNodes.Count - 1; i++) {
                AbstractDirectionalLine line = linePool.GetObject();
                lines.Add(line);
                
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
                    line.SetMask(AbstractDirectionalLine.LineType.StartHalf);
                    if (pathNodes.Count == 2) {
                        // This is the only line being displayed, so we should show the destination icon. 
                        line.ShowDestinationIcon(attack);
                    }
                } else if (i == pathNodes.Count - 2) {
                    // This is the last node we care about visualizing
                    if (hidePathDestination) {
                        line.SetMask(AbstractDirectionalLine.LineType.EndHalf);
                    } else {
                        line.SetMask(AbstractDirectionalLine.LineType.Full);
                        line.ShowDestinationIcon(attack);
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

        public void ClearPath(bool thickLines) {
            (List<AbstractDirectionalLine> lines, GameObjectPool<AbstractDirectionalLine> linePool) = GetLineGroup(thickLines);
            foreach (AbstractDirectionalLine line in lines) {
                line.Discard();
                linePool.AddAndHideObject(line);
            }
            lines.Clear();
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