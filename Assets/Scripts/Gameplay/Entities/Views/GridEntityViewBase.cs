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
        private AbilityTimerView _timerViewPrefab;
        [SerializeField]
        private Transform _timerLocation;
        
        [Header("References")] 
        [SerializeField]
        private Image _mainImage;
        [SerializeField] 
        private Image _teamColorImage;

        protected GridEntity Entity;
        public void Initialize(GridEntity entity) {
            Entity = entity;
            
            _mainImage.sprite = entity.Data.BaseSprite;
            _teamColorImage.sprite = entity.Data.TeamColorSprite;
            _teamColorImage.color = GameManager.Instance.GetPlayerForTeam(entity.MyTeam).Data.TeamColor;
            
            entity.AbilityPerformedEvent += DoAbility;
            entity.MovedEvent += Move;
            entity.SelectedEvent += Selected;
            entity.AttackPerformedEvent += Attack;
            entity.AttackReceivedEvent += AttackReceived;
            entity.KilledEvent += Killed;
        }

        // TODO can pass in things like color and timer location (maybe use a set of transform references) and stuff
        protected void CreateTimerView(AbilityTimer timer) {
            AbilityTimerView view = Instantiate(_timerViewPrefab, _timerLocation);
            view.Instantiate(timer);
        }

        public abstract void DoAbility(IAbility ability, AbilityTimer timer);
        public abstract void Move(Vector2Int targetCell);
        public abstract void Selected();
        public abstract void Attack(Vector2Int targetCell);
        public abstract void AttackReceived();
        public abstract void Killed();

        public class UnexpectedEntityAbilityException : Exception {
            public UnexpectedEntityAbilityException(IAbilityData data) : base($"Unexpected entity ability: {data}") { }
        }
    }
}