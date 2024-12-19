using Gameplay.Config;
using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// View for a building being built. Looks the same as its <see cref="GridEntityView"/> counterpart, but with only
    /// the entity images.
    /// </summary>
    public class InProgressBuildingView : MonoBehaviour {
        [SerializeField] private Image _buildingVisual_mainImage;
        [SerializeField] private Image _buildingVisual_teamColorImage;

        public void Initialize(BuildAbility buildAbility) {
            EntityData entityData = (EntityData)buildAbility.AbilityParameters.Buildable;
            GameTeam team = buildAbility.Performer.Team;
            
            _buildingVisual_mainImage.sprite = entityData.BaseSprite;
            _buildingVisual_mainImage.GetComponent<Canvas>().sortingOrder += entityData.GetStackOrder();
            _buildingVisual_teamColorImage.sprite = entityData.TeamColorSprite;
            _buildingVisual_teamColorImage.color = GameManager.Instance.GetPlayerForTeam(team).Data.TeamColor;
            _buildingVisual_teamColorImage.GetComponent<Canvas>().sortingOrder += entityData.GetStackOrder();

            buildAbility.Performer.KilledEvent += RemoveView;
        }

        private void RemoveView() {
            if (!this) return;
            Destroy(gameObject);
        }
    }
}