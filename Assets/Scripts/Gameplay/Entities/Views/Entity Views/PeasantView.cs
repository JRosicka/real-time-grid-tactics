using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class PeasantView : GridEntityParticularView {
        [SerializeField] private InProgressBuildingView _buildingViewPrefab;
        private InProgressBuildingView _buildingViewInstance;
        
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Debug.Log($"{nameof(DoAbility)}: {ability}");
            switch (ability) {
                case BuildAbility buildAbility:
                    DoBuildAnimation(buildAbility, cooldownTimer);
                    return false;
                default:
                    return true;
            }
        }
        
        private void DoBuildAnimation(BuildAbility buildAbility, AbilityCooldownTimer timer) {
            // Make a temporary image of the building being built
            Vector2 position = GameManager.Instance.GridController.GetWorldPosition(buildAbility.AbilityParameters.BuildLocation);
            _buildingViewInstance = Instantiate(_buildingViewPrefab, position, Quaternion.identity, GameManager.Instance.CommandManager.SpawnBucket);
            _buildingViewInstance.Initialize(buildAbility);
            timer.ExpiredEvent += BuildAbilityCompleted;
            
            // Animate the peasant view with the build animation TODO
        }

        private void BuildAbilityCompleted() {
            // Hide the temporary building visual
            Destroy(_buildingViewInstance.gameObject);

            // Stop the peasant build animation TODO
        }
    }
}