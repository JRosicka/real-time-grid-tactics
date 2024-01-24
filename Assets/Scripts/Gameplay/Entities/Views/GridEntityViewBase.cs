using System;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("UnitView")] [SerializeField] 
        private Transform _unitView;
        [FormerlySerializedAs("UnitAnimator")] [SerializeField] private Animator _unitAnimator;

        [FormerlySerializedAs("SecondsToMoveToAdjacentCell")]
        [Header("Config")]
        [SerializeField] private float _secondsToMoveToAdjacentCell;
        [SerializeField] private float _attackAnimationIntro_lengthSeconds;
        [SerializeField] private AnimationCurve _attackAnimationIntro_curve;
        [SerializeField] private float _attackAnimationOutro_lengthSeconds;
        [SerializeField] private AnimationCurve _attackAnimationOutro_curve;
        
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
            // Need to do attack after movement in order to properly handle when both are happening
            UpdateAttack();
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
                    DoGenericAttackAnimation((AttackAbility) ability);
                    break;
                default:
                    Debug.LogWarning($"Unexpected entity ability: {ability.AbilityData}");
                    break;
            }
        }

        #region Shmovement
        
        private Vector2 _movementStartPosition;
        private Vector2 _movementTargetPosition;
        private float _moveTime;
        private bool _moving;
        
        private void DoGenericMoveAnimation(MoveAbility moveAbility) {
            _movementStartPosition = transform.position;
            _movementTargetPosition = GameManager.Instance.GridController.GetWorldPosition(moveAbility.AbilityParameters.NextMoveCell);
            _moveTime = 0;
            _moving = true;

            // Face the x-direction that we are going
            SetFacingDirection(_movementStartPosition, _movementTargetPosition);
            
            // Animate
            _unitAnimator.Play("GenericMove");
        }

        private void UpdateMove() {
            if (!_moving) return;
            
            _moveTime += Time.deltaTime;
            transform.position = Vector2.Lerp(_movementStartPosition, _movementTargetPosition, _moveTime / _secondsToMoveToAdjacentCell);

            if (_moveTime > _secondsToMoveToAdjacentCell) {
                _moving = false;
            }
        }
        
        #endregion
        
        #region Attacking

        private Vector2 _attackStartPosition;
        private Vector2 _attackTargetPosition;
        private Vector2 _attackReturnPosition;
        private float _attackTime;
        private bool _attacking;
        
        private void DoGenericAttackAnimation(AttackAbility attackAbility) {
            _moving = false;
            
            _attackStartPosition = transform.position;    // Might be different from the entity location if we are in the middle of a move animation
            _attackTargetPosition = GameManager.Instance.GridController.GetWorldPosition(attackAbility.AbilityParameters.Target.Location);
            _attackReturnPosition = GameManager.Instance.GridController.GetWorldPosition(Entity.Location);
            _attackTime = 0;
            _attacking = true;
            
            // Face the x-direction that we are attacking
            SetFacingDirection(_attackReturnPosition, _attackTargetPosition);
        }
        
        private void UpdateAttack() {
            if (!_attacking) return;

            _attackTime += Time.deltaTime;
            if (_attackTime <= _attackAnimationIntro_lengthSeconds) {
                float evaluationProgress = _attackAnimationIntro_curve.Evaluate(_attackTime / _attackAnimationIntro_lengthSeconds);
                transform.position = Vector2.Lerp(_attackStartPosition, _attackTargetPosition, evaluationProgress);
            } else {
                float time = _attackTime - _attackAnimationIntro_lengthSeconds;
                float evaluationProgress = _attackAnimationOutro_curve.Evaluate(time / _attackAnimationOutro_lengthSeconds);
                transform.position = Vector2.Lerp(_attackReturnPosition, _attackTargetPosition, evaluationProgress);
            }

            if (_attackTime > _attackAnimationIntro_lengthSeconds + _attackAnimationOutro_lengthSeconds) {
                _attacking = false;
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

        private void SetFacingDirection(Vector2 currentPosition, Vector2 targetPosition) {
            float xDifference = targetPosition.x - currentPosition.x;
            if (Mathf.Approximately(xDifference, 0)) return;
            
            bool faceRight = targetPosition.x - currentPosition.x > 0;
            var localScale = _unitView.transform.localScale;
            float scaleX = localScale.x;
            
            if ((faceRight && scaleX > 0) || (!faceRight && scaleX < 0)) return;
            
            localScale = new Vector3(scaleX * -1, localScale.y, localScale.z);
            _unitView.transform.localScale = localScale;
        }
    }
}