using System;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// The view portion of a <see cref="GridEntity"/>, handling movements, images, animations, and timers.
    ///
    /// This covers the generic view functionality for all <see cref="GridEntity"/>s. For entity-type-specific functionality,
    /// see 
    /// </summary>
    public sealed class GridEntityView : MonoBehaviour {
        [SerializeField]
        private AbilityTimerCooldownView _timerCooldownViewPrefab;
        [SerializeField]
        private Transform _moveTimerLocation;
        [SerializeField]
        private Transform _attackTimerLocation;
        
        [Header("References")] 
        [SerializeField]
        private Image _mainImage;
        [SerializeField] 
        private Image _teamColorImage;
        [SerializeField]
        private GridEntityParticularView _particularView;

        public event Action KillAnimationFinishedEvent;
        
        [HideInInspector] 
        public GridEntity Entity;
        public void Initialize(GridEntity entity) {
            Entity = entity;
            
            _mainImage.sprite = entity.EntityData.BaseSprite;
            _teamColorImage.sprite = entity.EntityData.TeamColorSprite;
            _teamColorImage.color = GameManager.Instance.GetPlayerForTeam(entity.MyTeam).Data.TeamColor;
            
            entity.AbilityPerformedEvent += DoAbility;
            entity.CooldownTimerStartedEvent += CreateTimerView;
            entity.SelectedEvent += Selected;
            entity.HPChangedEvent += AttackReceived;
            entity.KilledEvent += Killed;
        }

        public void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            if (_particularView.DoAbility(ability, cooldownTimer)) {
                DoGenericAbility(ability);
            }
        }

        public void Selected() {
            Debug.Log(nameof(Selected));
        }

        public void AttackReceived() {
            Debug.Log(nameof(AttackReceived));
        }

        public void Killed() {
            Debug.Log(nameof(Killed));
            // TODO wait until we actually do a kill animation before calling this
            KillAnimationFinished();
        }

        private void KillAnimationFinished() {
            KillAnimationFinishedEvent?.Invoke();
        }

        /// <summary>
        /// Catch-all for generic ability view behavior. Subclasses should call this in their default cases for their
        /// <see cref="DoAbility"/> overrides.
        /// </summary>
        private void DoGenericAbility(IAbility ability) {
            switch (ability.AbilityData) {
                case MoveAbilityData moveAbility:
                    DoGenericMoveAnimation((MoveAbility)ability);
                    break;
                case AttackAbilityData attackAbility:
                    // TODO generic attack animation
                    break;
                default:
                    Debug.LogWarning($"Unexpected entity ability: {ability.AbilityData}");
                    break;
            }
        }

        private void DoGenericMoveAnimation(MoveAbility moveAbility) {
            // Just instantly move the entity to the destination for now
            Entity.transform.position = GameManager.Instance.GridController.GetWorldPosition(moveAbility.AbilityParameters.NextMoveCell);
        }
        
        // TODO can pass in things like color and timer location (maybe use a set of transform references) and stuff
        private void CreateTimerView(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Transform timerLocation = _moveTimerLocation;
            if (cooldownTimer.Ability is AttackAbility) {
                timerLocation = _attackTimerLocation;
            }
            AbilityTimerCooldownView cooldownView = Instantiate(_timerCooldownViewPrefab, timerLocation);
            cooldownView.Initialize(cooldownTimer, true, true);
        }
    }
}