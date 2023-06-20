using System;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Entities {
    /// <summary>
    /// The view portion of a <see cref="GridEntity"/>, handling movements, images, animations, and timers
    /// </summary>
    public abstract class GridEntityViewBase : MonoBehaviour {
        [SerializeField]
        private AbilityTimerCooldownView TimerCooldownViewPrefab;
        [SerializeField]
        private Transform _timerLocation;
        
        [Header("References")] 
        [SerializeField]
        private Image _mainImage;
        [SerializeField] 
        private Image _teamColorImage;

        public event Action KillAnimationFinishedEvent;
        
        protected GridEntity Entity;
        public void Initialize(GridEntity entity) {
            Entity = entity;
            
            _mainImage.sprite = entity.EntityData.BaseSprite;
            _teamColorImage.sprite = entity.EntityData.TeamColorSprite;
            _teamColorImage.color = GameManager.Instance.GetPlayerForTeam(entity.MyTeam).Data.TeamColor;
            
            entity.AbilityPerformedEvent += DoAbility;
            entity.SelectedEvent += Selected;
            entity.HPChangedEvent += AttackReceived;
            entity.KilledEvent += Killed;
        }

        // TODO can pass in things like color and timer location (maybe use a set of transform references) and stuff
        protected void CreateTimerView(AbilityCooldownTimer cooldownTimer) {
            AbilityTimerCooldownView cooldownView = Instantiate(TimerCooldownViewPrefab, _timerLocation);
            cooldownView.Initialize(cooldownTimer, true, true);
        }

        public abstract void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer);
        public abstract void Selected();
        public abstract void AttackReceived();
        public abstract void Killed();

        protected void KillAnimationFinished() {
            KillAnimationFinishedEvent?.Invoke();
        }

        /// <summary>
        /// Catch-all for generic ability view behavior. Subclasses should call this in their default cases for their
        /// <see cref="DoAbility"/> overrides.
        /// </summary>
        protected void DoGenericAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            switch (ability.AbilityData) {
                case MoveAbilityData moveAbility:
                    CreateTimerView(cooldownTimer);
                    DoGenericMoveAnimation((MoveAbility)ability);
                    break;
                case AttackAbilityData attackAbility:
                    CreateTimerView(cooldownTimer);
                    // TODO generic attack animation
                    break;
                default:
                    Debug.LogWarning($"Unexpected entity ability: {ability.AbilityData}");
                    break;
            }
        }

        private void DoGenericMoveAnimation(MoveAbility moveAbility) {
            // Just instantly move the entity to the destination
            Entity.transform.position = GameManager.Instance.GridController.GetWorldPosition(((MoveAbilityParameters)moveAbility.BaseParameters).Destination);
        }
    }
}