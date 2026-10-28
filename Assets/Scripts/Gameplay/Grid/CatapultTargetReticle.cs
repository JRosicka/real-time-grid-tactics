using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Grid {
    /// <summary>
    /// A single icon indicating where a catapult is aiming or firing
    /// </summary>
    public class CatapultTargetReticle : MonoBehaviour {
        [SerializeField] private Image _image;

        public void SetTeam(PlayerColorData teamColors) {
            _image.color = teamColors.TeamColor;
        }
        
        public void UpdateLocation(HashSet<Vector2Int> cellsAroundVertex) {
            gameObject.SetActive(true);
            transform.position = GameManager.Instance.GridController.GetWorldPosition(cellsAroundVertex);
            
            int yMax = cellsAroundVertex.Select(c => c.y).Max();
            bool wideTop = cellsAroundVertex.Count(c => c.y == yMax) == 2;
            transform.eulerAngles = wideTop ? new Vector3(0, 0, 0) : new Vector3(0, 0, 60);
        }

        public void Hide() {
            gameObject.SetActive(false);
        }
    }
}