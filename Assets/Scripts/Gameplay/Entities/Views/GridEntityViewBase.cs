using System;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// The view portion of a <see cref="GridEntity"/>, handling movements, images, animations, and timers
    /// </summary>
    public abstract class GridEntityViewBase : MonoBehaviour {
        [Header("References")] 
        [SerializeField]
        private SpriteRenderer _mainSprite;
        [SerializeField] 
        private SpriteRenderer _teamColorSprite;

        protected GridEntity Entity;
        public void Initialize(GridEntity entity) {
            Entity = entity;
            
            _mainSprite.sprite = entity.Data.BaseSprite;
            _teamColorSprite.sprite = entity.Data.TeamColorSprite;
            _teamColorSprite.color = GameManager.Instance.GetPlayerForTeam(entity.MyTeam).Data.TeamColor;
            
            entity.AbilityPerformedEvent += DoAbilityAnimation;
            entity.MovedEvent += Move;
            entity.SelectedEvent += Selected;
            entity.AttackPerformedEvent += Attack;
            entity.AttackReceivedEvent += AttackReceived;
            entity.KilledEvent += Killed;
        }
        public abstract void DoAbilityAnimation(IAbility ability);
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