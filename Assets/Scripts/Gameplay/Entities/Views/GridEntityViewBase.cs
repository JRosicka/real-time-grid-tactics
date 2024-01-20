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
        
        [Header("References")] 
        [SerializeField]
        private Image _mainImage;
        [SerializeField] 
        private Image _teamColorImage;
        [SerializeField]
        private Transform _moveTimerLocation;
        [SerializeField]
        private Transform _attackTimerLocation;
        [SerializeField] 
        private Transform UnitView;

        [Header("Config")]
        public float SecondsToMoveToAdjacentCell;
        
        protected GridEntity Entity;

        public event Action KillAnimationFinishedEvent;
        
        public void Initialize(GridEntity entity, int stackOrder) {
            Entity = entity;
            
            _mainImage.sprite = entity.EntityData.BaseSprite;
            _mainImage.GetComponent<Canvas>().sortingOrder += stackOrder;
            _teamColorImage.sprite = entity.EntityData.TeamColorSprite;
            _teamColorImage.color = GameManager.Instance.GetPlayerForTeam(entity.MyTeam).Data.TeamColor;
            _teamColorImage.GetComponent<Canvas>().sortingOrder += stackOrder;
            
            entity.AbilityPerformedEvent += DoAbility;
            entity.CooldownTimerStartedEvent += CreateTimerView;
            entity.SelectedEvent += Selected;
            entity.HPChangedEvent += AttackReceived;
            entity.KilledEvent += Killed;
        }

        private void Update() {
            UpdateMove();
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
        protected void DoGenericAbility(IAbility ability) {
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

        #region shmovement
        
        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private float _moveTime;
        private bool _moving;
        
        private void DoGenericMoveAnimation(MoveAbility moveAbility) {
            _startPosition = transform.position;
            _targetPosition = GameManager.Instance.GridController.GetWorldPosition(moveAbility.AbilityParameters.NextMoveCell);
            _moveTime = 0;
            _moving = true;

            // Face the x-direction that we are going
            SetFacingDirection(_targetPosition.x - _startPosition.x > 0);
        }

        private void UpdateMove() {
            if (!_moving) return;
            
            _moveTime += Time.deltaTime;
            transform.position = Vector2.Lerp(_startPosition, _targetPosition, _moveTime / SecondsToMoveToAdjacentCell);

            if (_moveTime > SecondsToMoveToAdjacentCell) {
                _moving = false;
            }
        }
        
        #endregion
        
        // TODO can pass in things like color and timer location (maybe use a set of transform references) and stuff
        private void CreateTimerView(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            Transform timerLocation = _moveTimerLocation;
            if (cooldownTimer.Ability is AttackAbility) {
                timerLocation = _attackTimerLocation;
            }
            AbilityTimerCooldownView cooldownView = Instantiate(TimerCooldownViewPrefab, timerLocation);
            cooldownView.Initialize(cooldownTimer, true, true);
        }

        private void SetFacingDirection(bool faceRight) {
            var localScale = UnitView.transform.localScale;
            float scaleX = localScale.x;

            if ((faceRight && scaleX > 0) || (!faceRight && scaleX < 0)) return;
            
            localScale = new Vector3(scaleX * -1, localScale.y, localScale.z);
            UnitView.transform.localScale = localScale;
        }
    }
}