using Gameplay.Config.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles displaying ability icons on the map when hovering the mouse over a tile with an
    /// <see cref="ITargetableAbilityData"/> selected
    /// </summary>
    public class GridIconDisplayer : MonoBehaviour {
        [SerializeField] private Image _icon;
        
        public void DisplayOverHoveredCell(ITargetableAbilityData data, Vector2Int? cell) {
            if (cell == null || !data.ShowIconOnGridWhenSelected) {
                _icon.gameObject.SetActive(false);
                return;
            }
            
            _icon.gameObject.SetActive(true);
            _icon.sprite = data.OverrideIconForGrid ? data.OverrideIconForGrid : data.Icon;
            _icon.color = data.GridIconColor;
            _icon.transform.position = GameManager.Instance.GridController.GetWorldPosition(cell.Value);
            _icon.transform.localScale = data.FlipGridIcon ? new Vector3(-1, 1, 1) : Vector3.one;
        }

        public void DisplayOverCurrentHoveredCell(ITargetableAbilityData data) {
            Vector2Int? currentHoveredCell = GameManager.Instance.GridInputController.CurrentHoveredCell;
            if (currentHoveredCell != null) {
                DisplayOverHoveredCell(data, currentHoveredCell.Value);
            }
        }
    }
}