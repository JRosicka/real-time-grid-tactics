using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Handles build-ability-specific behavior for an <see cref="AbilitySlot"/>
    /// </summary>
    public class BuildAbilitySlotBehavior : IAbilitySlotBehavior {
        public PurchasableData Buildable { get; }
        private readonly BuildAbilityData _buildAbilityData;
        protected readonly GridEntity SelectedEntity;

        public IAbilityData AbilityData => _buildAbilityData;
        public bool IsAvailabilitySensitiveToResources => true;
        public bool CaresAboutAbilityChannels => false;
        public bool IsAbilityTargetable => _buildAbilityData.Targeted;
        private GridController GridController => GameManager.Instance.GridController;
        private GridEntityCollection EntitiesOnGrid => GameManager.Instance.CommandManager.EntitiesOnGrid;

        public BuildAbilitySlotBehavior(BuildAbilityData buildData, PurchasableData buildable, GridEntity selectedEntity) {
            _buildAbilityData = buildData;
            Buildable = buildable;
            SelectedEntity = selectedEntity;
        }
        
        public virtual void SelectSlot() {
            if (_buildAbilityData.Targetable) {
                GameManager.Instance.EntitySelectionManager.SelectTargetableAbility(_buildAbilityData, Buildable);
                GameManager.Instance.SelectionInterface.TooltipView.ToggleForTargetableAbility(_buildAbilityData, this);
                List<Vector2Int> viableTargets = GetViableTargets();
                if (viableTargets != null) {
                    GridController.UpdateSelectableCells(viableTargets, SelectedEntity);
                }
            } else {
                // Try to perform the build ability, but only if the build queue is not full
                if (!SelectedEntity.BuildQueue.HasSpace) {
                    return;
                }
                Vector2Int? selectedEntityLocation = SelectedEntity.Location;
                if (selectedEntityLocation == null) return;
                SelectedEntity.PerformAbility(_buildAbilityData, new BuildAbilityParameters {
                    Buildable = Buildable, 
                    BuildLocation = selectedEntityLocation.Value
                }, true, false);
            }
        }

        public virtual AbilitySlot.AvailabilityResult GetAvailability() {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(SelectedEntity.MyTeam);
            List<PurchasableData> ownedPurchasables = player.OwnedPurchasablesController.OwnedPurchasables;

            if (Buildable is UpgradeData && ownedPurchasables.Contains(Buildable)) {
                // Upgrade that we already own
                return AbilitySlot.AvailabilityResult.NoLongerAvailable;
            } else if (SelectedEntity.CanUseAbility(_buildAbilityData, AbilityData.SelectableWhenBlocked)
                       && GameManager.Instance.GetPlayerForTeam(SelectedEntity.MyTeam).ResourcesController.CanAfford(Buildable.Cost)
                       && Buildable.Requirements.All(r => ownedPurchasables.Contains(r))) {
                // This entity can build this and we can afford this
                return AbilitySlot.AvailabilityResult.Selectable;
            } else {
                return AbilitySlot.AvailabilityResult.Unselectable;
            }
        }

        public void SetUpSprites(Image abilityImage, Image secondaryAbilityImage, Canvas teamColorsCanvas) {
            abilityImage.sprite = Buildable.BaseSpriteIconOverride == null ? Buildable.BaseSprite : Buildable.BaseSpriteIconOverride;

            if (Buildable.TeamColorSprite == null) {
                teamColorsCanvas.sortingOrder = 1;
            } else {
                secondaryAbilityImage.sprite = Buildable.TeamColorSprite;
                secondaryAbilityImage.color = GameManager.Instance.GetPlayerForTeam(SelectedEntity.MyTeam).Data.TeamColor;
                secondaryAbilityImage.gameObject.SetActive(true);
                teamColorsCanvas.sortingOrder = Buildable.DisplayTeamColorOverMainSprite ? 2 : 1;
            }
        }
        
        private List<Vector2Int> GetViableTargets() {
            if (Buildable is not EntityData buildableEntity) return null;
            
            List<Vector2Int> viableTargets = new List<Vector2Int>();
            
            // Add each cell with a tile that the structure can be built on
            foreach (GameplayTile tile in buildableEntity.EligibleStructureLocations) {
                viableTargets.AddRange(GridController.GridData.GetCells(tile).Select(c => c.Location));
            }

            // Remove any cells that have opponent entities or friendly structures
            List<Vector2Int> ret = new List<Vector2Int>();
            foreach (Vector2Int viableTarget in viableTargets) {
                var entities = EntitiesOnGrid.EntitiesAtLocation(viableTarget);
                if (entities?.Entities == null) {
                    // No entities there, so it is eligible
                    ret.Add(viableTarget);
                    continue;
                }
                if (!entities.Entities.Any(e =>
                        (e.Entity.MyTeam != SelectedEntity.MyTeam && e.Entity.MyTeam != GridEntity.Team.Neutral) || e.Entity.EntityData.IsStructure)) {
                    // Entities there, but none of them are opponents or friendly structures, so eligible
                    ret.Add(viableTarget);
                }
            }

            return ret;
        }
    }
}