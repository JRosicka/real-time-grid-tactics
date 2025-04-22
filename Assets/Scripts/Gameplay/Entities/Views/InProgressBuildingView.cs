using Gameplay.Config;
using Gameplay.Entities.Abilities;
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

            buildAbility.Performer.UnregisteredEvent += RemoveView;
        }

        public void Initialize(GameTeam team, EntityData entityData, bool dimmed) {
            _buildingVisual_mainImage.sprite = entityData.BaseSprite;
            _buildingVisual_mainImage.GetComponent<Canvas>().sortingOrder += entityData.GetStackOrder();
            Color mainImageColor = _buildingVisual_mainImage.color;
            mainImageColor.a = dimmed ? _dimmedAlpha : 1;
            _buildingVisual_mainImage.color = mainImageColor;
            _buildingVisual_teamColorImage.sprite = entityData.TeamColorSprite;
            Color teamColorsImageColor = GameManager.Instance.GetPlayerForTeam(team).Data.TeamColor;
            teamColorsImageColor.a = dimmed ? _dimmedAlpha : 1;
            _buildingVisual_teamColorImage.color = teamColorsImageColor;
            _buildingVisual_teamColorImage.GetComponent<Canvas>().sortingOrder += entityData.GetStackOrder();
        }

        public void RemoveView() {
            if (!this) return;
            Destroy(gameObject);
        }
    }
}