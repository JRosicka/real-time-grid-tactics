using System;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;
using UnityEngine.Serialization;
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
        
        [Header("References")] 
        [SerializeField]
        private Image _mainImage;
        [SerializeField] 
        private Image _teamColorImage;
        [SerializeField]
        private GridEntityParticularView _particularView;
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
        [FormerlySerializedAs("_attackAnimationIntro_curve")] 
        [SerializeField] private AnimationCurve _attackAnimationIntro_curveFromNoMove;
        [SerializeField] private AnimationCurve _attackAnimationIntro_curveFromMove;
        [SerializeField] private float _distanceTowardsTargetToMove;
        [SerializeField] private float _attackAnimationOutro_lengthSeconds;
        [SerializeField] private AnimationCurve _attackAnimationOutro_curve;
        
        [HideInInspector] 
        public GridEntity Entity;
        
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
        // If true, then our attack animation is going straight from the middle of a move. Use a different animation curve so it don't look like garb.
        private bool _attackFromMove;
        
        private void DoGenericAttackAnimation(AttackAbility attackAbility) {
            _attackFromMove = _moving;
            _moving = false;
            
            _attackStartPosition = transform.position;    // Might be different from the entity location if we are in the middle of a move animation
            _attackReturnPosition = GameManager.Instance.GridController.GetWorldPosition(Entity.Location);
            
            // We don't want to go all the way to the target location, just part of the way
            Vector2Int targetCell = attackAbility.AbilityParameters.Target != null
                ? attackAbility.AbilityParameters.Target.Location
                : attackAbility.AbilityParameters.Destination;
            Vector2 targetLocation = GameManager.Instance.GridController.GetWorldPosition(targetCell);
            _attackTargetPosition = Vector2.Lerp(_attackReturnPosition, targetLocation, _distanceTowardsTargetToMove);
            
            _attackTime = 0;
            _attacking = true;
            
            // Face the x-direction that we are attacking
            SetFacingDirection(_attackReturnPosition, _attackTargetPosition);
        }
        
        private void UpdateAttack() {
            if (!_attacking) return;

            _attackTime += Time.deltaTime;
            if (_attackTime <= _attackAnimationIntro_lengthSeconds) {
                AnimationCurve curve = _attackFromMove ? _attackAnimationIntro_curveFromMove : _attackAnimationIntro_curveFromNoMove;
                float evaluationProgress = curve.Evaluate(_attackTime / _attackAnimationIntro_lengthSeconds);
                transform.position = Vector2.LerpUnclamped(_attackStartPosition, _attackTargetPosition, evaluationProgress);
            } else {
                float time = _attackTime - _attackAnimationIntro_lengthSeconds;
                float evaluationProgress = _attackAnimationOutro_curve.Evaluate(time / _attackAnimationOutro_lengthSeconds);
                transform.position = Vector2.LerpUnclamped(_attackReturnPosition, _attackTargetPosition, evaluationProgress);
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
            AbilityTimerCooldownView cooldownView = Instantiate(_timerCooldownViewPrefab, timerLocation);
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