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
            cooldownView.Initialize(cooldownTimer, true);
        }

        public abstract void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer);
        public abstract void Selected();
        public abstract void AttackReceived();
        public abstract void Killed();

        protected void KillAnimationFinished() {
            KillAnimationFinishedEvent?.Invoke();
        }

        public class UnexpectedEntityAbilityException : Exception {
            public UnexpectedEntityAbilityException(IAbilityData data) : base($"Unexpected entity ability: {data}") { }
        }
    }
}