using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities.Abilities;
using Gameplay.Managers;
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
        [SerializeField] private CanvasGroup _fowCanvasGroup;

        private Vector2Int? _location;
        private TeamFogOfWarTracker _fowTracker;

        public void Initialize(BuildAbility buildAbility) {
            EntityData entityData = (EntityData)buildAbility.AbilityParameters.Buildable;
            GameTeam team = buildAbility.PerformerTeam;
            
            Initialize(team, entityData, false, buildAbility.AbilityParameters.BuildLocation, team != GameManager.Instance.LocalTeam);

            buildAbility.Performer.UnregisteredEvent += RemoveView;
        }

        public void Initialize(GameTeam team, EntityData entityData, bool dimmed, Vector2Int? location, bool hiddenByFoW) {
            _location = location;
            
            _buildingVisual_mainImage.sprite = entityData.BaseSprite;
            _buildingVisual_mainImage.GetComponent<Canvas>().sortingOrder += entityData.GetStackOrder();
            Color mainImageColor = _buildingVisual_mainImage.color;
            mainImageColor.a = dimmed ? _dimmedAlpha : 1;
            _buildingVisual_mainImage.color = mainImageColor;
            _buildingVisual_teamColorImage.sprite = entityData.TeamColorSprite;
            Color teamColorsImageColor = GameManager.Instance.GetPlayerForTeam(team).ColorData.TeamColor;
            teamColorsImageColor.a = dimmed ? _dimmedAlpha : 1;
            _buildingVisual_teamColorImage.color = entityData.TeamColorSprite ? teamColorsImageColor : Color.clear;
            _buildingVisual_teamColorImage.GetComponent<Canvas>().sortingOrder += entityData.GetStackOrder();
            
            // FoW handling
            if (hiddenByFoW) {
                _fowTracker = GameManager.Instance.FogOfWarManager!.GetLocalTeamTracker();
                if (_fowTracker != null) {
                    _fowTracker.FoWUpdated += FogOfWarUpdated;
                    if (location != null) {
                        SetFoWVisibility(_fowTracker.IsLocationHidden(location.Value));
                    }
                }
            }
        }

        public void RemoveView() {
            if (!this) return;
            
            if (_fowTracker != null) {
                _fowTracker.FoWUpdated -= FogOfWarUpdated;
            }
            Destroy(gameObject);
        }

        private void SetFoWVisibility(bool hidden) {
            _fowCanvasGroup.alpha = hidden ? 0 : 1f;
        }

        private void FogOfWarUpdated(List<TeamFogOfWarTracker.FoWCell> foWCells) {
            if (_location == null) return;
            
            TeamFogOfWarTracker.FoWCell fowCell = foWCells.FirstOrDefault(c => c.Position == _location.Value);
            if (fowCell != null) {
                SetFoWVisibility(fowCell.Hidden);
            }
        }
    }
}