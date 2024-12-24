using Gameplay.Config;
using Gameplay.Entities.Abilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// View for a building being built or selected. Looks the same as its <see cref="GridEntityView"/> counterpart,
    /// but with only the entity images.
    /// </summary>
    public class InProgressBuildingView : MonoBehaviour {
        [SerializeField] private Image _buildingVisual_mainImage;
        [SerializeField] private Image _buildingVisual_teamColorImage;
        [SerializeField] private float _dimmedAlpha = .7f;

        public void Initialize(BuildAbility buildAbility) {
            EntityData entityData = (EntityData)buildAbility.AbilityParameters.Buildable;
            GameTeam team = buildAbility.Performer.Team;
            
            Initialize(team, entityData, false);

            buildAbility.Performer.KilledEvent += RemoveView;
        }

        public void Initialize(GameTeam team, EntityData entityData, bool dimmed) {
            _buildingVisual_mainImage.sprite = entityData.BaseSprite;
            _buildingVisual_mainImage.GetComponent<Canvas>().sortingOrder += entityData.GetStackOrder();
            _buildingVisual_mainImage.color = _buildingVisual_mainImage.color.WithAlpha(dimmed ? _dimmedAlpha : 1);
            _buildingVisual_teamColorImage.sprite = entityData.TeamColorSprite;
            _buildingVisual_teamColorImage.color = GameManager.Instance.GetPlayerForTeam(team).Data.TeamColor.WithAlpha(dimmed ? _dimmedAlpha : 1);;
            _buildingVisual_teamColorImage.GetComponent<Canvas>().sortingOrder += entityData.GetStackOrder();
        }

        public void RemoveView() {
            if (!this) return;
            Destroy(gameObject);
        }
    }
}