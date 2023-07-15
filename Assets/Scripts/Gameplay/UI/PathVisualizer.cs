using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// Handles creating (from a pool) a bunch of directional arrows pointing from tile to tile to visualize a path
    /// </summary>
    public class PathVisualizer : MonoBehaviour {
        // TODO Move to PathfinderService or something
        public struct GridPath {
            public List<Vector2Int> Cells;
        }

        [SerializeField] private DirectionalLine _directionalLinePrefab;
        [SerializeField] private Transform _lineBucket;
        [SerializeField] private int _poolSize;

        private GameObjectPool<DirectionalLine> _linePool;
        private List<DirectionalLine> _currentlyDisplayedLines = new List<DirectionalLine>();

        private GridController GridController => GameManager.Instance.GridController;
        private PathfinderService PathfinderService => GameManager.Instance.PathfinderService;

        public void Initialize() {
            _linePool = new GameObjectPool<DirectionalLine>(_directionalLinePrefab, _lineBucket, _poolSize);
        }

        /// <summary>
        /// Lay out a set of <see cref="DirectionalLine"/>s along a path
        /// </summary>
        public void Visualize(GridPath path) {
            ClearPath();
            
            // If the path is only 2 cells, then no need to place any lines
            if (path.Cells.Count < 3) return;
            
            // No need to visualise for the final cell
            float currentAngle = -1f;
            for (int i = 0; i < path.Cells.Count - 1; i++) {
                DirectionalLine line = _linePool.GetObject();
                _currentlyDisplayedLines.Add(line);
                
                // Reveal the line and move it in place
                line.gameObject.SetActive(true);
                line.transform.position = GridController.GetWorldPosition(path.Cells[i]);
                
                // Rotate the line to link to the next cell in the path
                var previousAngle = currentAngle;
                currentAngle = PathfinderService.AngleBetweenCells(path.Cells[i], path.Cells[i + 1]);
                line.transform.rotation = Quaternion.Euler(0, 0, currentAngle);
                
                // Hide/adjust parts of the line if this is the first or last cell in the path. 
                if (i == 0) {
                    line.SetMask(DirectionalLine.LineType.StartHalf);
                } else if (i == path.Cells.Count - 2) {
                    line.SetMask(DirectionalLine.LineType.EndHalf);
                    // Hide/show the previous line's dot if it has a different angle than this one.
                    _currentlyDisplayedLines[i-1].ToggleEndDot(!Mathf.Approximately(currentAngle, previousAngle));
                } else {
                    line.SetMask(DirectionalLine.LineType.Full);
                    // Hide/show the previous line's dot if it has a different angle than this one.
                    _currentlyDisplayedLines[i-1].ToggleEndDot(!Mathf.Approximately(currentAngle, previousAngle));
                }
            }
        }

        private void ClearPath() {
            foreach (DirectionalLine line in _currentlyDisplayedLines) {
                _linePool.AddAndHideObject(line);
            }
            _currentlyDisplayedLines.Clear();
        }
    }
}