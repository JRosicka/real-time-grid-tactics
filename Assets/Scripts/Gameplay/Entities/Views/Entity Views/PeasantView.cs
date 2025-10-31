using Gameplay.Config;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class PeasantView : GridEntityParticularView {
        [SerializeField] private InProgressBuildingView _buildingViewPrefab;
        [SerializeField] private Animator _animator;
        private InProgressBuildingView _buildingViewInstance;
        private GridEntity _entity;
        private GridEntity _currentResourceEntityBeingBuiltOn;

        public override void Initialize(GridEntity entity) {
            _entity = entity;
        }

        public override void LethalDamageReceived() {
            if (_currentResourceEntityBeingBuiltOn != null) {
                _currentResourceEntityBeingBuiltOn.ToggleView(true);
                _currentResourceEntityBeingBuiltOn = null;
            }
        }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability) {
                case BuildAbility buildAbility:
                    DoBuildAnimation(buildAbility, abilityTimer);
                    return false;
                default:
                    return true;
            }
        }
        
        private void DoBuildAnimation(BuildAbility buildAbility, AbilityTimer timer) {
            // Make a temporary image of the building being built
            Vector2 position = GameManager.Instance.GridController.GetWorldPosition(buildAbility.AbilityParameters.BuildLocation);
            _buildingViewInstance = Instantiate(_buildingViewPrefab, position, Quaternion.identity, GameManager.Instance.CommandManager.SpawnBucket);
            _buildingViewInstance.Initialize(buildAbility);
            timer.ExpiredEvent += BuildAbilityCompleted;
            
            // Hide the matching resource entity
            EntityData buildableData = (EntityData)buildAbility.AbilityParameters.Buildable;
            _currentResourceEntityBeingBuiltOn = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(_entity, buildableData);
            if (_currentResourceEntityBeingBuiltOn != null) {
                _currentResourceEntityBeingBuiltOn.ToggleView(false);
            }
            
            // Animate the peasant view with the build animation
            _animator.Play("WorkerBuild");

            if (GameManager.Instance.LocalTeam == GameTeam.Spectator || buildAbility.PerformerTeam == GameManager.Instance.LocalTeam) {
                GameManager.Instance.GameAudio.ConstructionSound();
            }
        }

        private void BuildAbilityCompleted(bool canceled) {
            // Hide the temporary building visual
            Destroy(_buildingViewInstance.gameObject);

            // Stop the peasant build animation
            _animator.Play("Idle");
            
            // Show the matching resource entity
            if (canceled && _currentResourceEntityBeingBuiltOn != null) {
                _currentResourceEntityBeingBuiltOn.ToggleView(true);
                _currentResourceEntityBeingBuiltOn = null;
            }
        }
    }
}