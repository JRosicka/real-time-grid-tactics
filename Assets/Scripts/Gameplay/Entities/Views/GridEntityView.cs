using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Gameplay.Grid;
using Gameplay.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;

namespace Gameplay.Entities {
    /// <summary>
    /// The view portion of a <see cref="GridEntity"/>, handling movements, images, animations, and timers.
    ///
    /// This covers the generic view functionality for all <see cref="GridEntity"/>s. For entity-type-specific functionality,
    /// see <see cref="GridEntityParticularView"/>
    /// </summary>
    public sealed class GridEntityView : MonoBehaviour {
        [Header("References")] 
        [SerializeField] private CanvasGroup _mainCanvasGroup;
        [SerializeField] private Image _mainImage;
        [SerializeField] private Image _teamColorImage;
        [SerializeField] private GridEntityParticularView _particularView;
        [SerializeField] private GameObject _bottomUISection;
        [SerializeField] private AbilityTimerFill _moveTimerFill;
        [SerializeField] private AbilityTimerFill _attackTimerFill;
        [SerializeField] private Transform _buildTimerLocation;
        [SerializeField] private Transform _uniqueAbilityTimerLocation;
        [FormerlySerializedAs("UnitAnimator")] [SerializeField] private Animator _unitAnimator;
        [SerializeField] private Animator _healAnimator;
        [SerializeField] private Transform _directionContainer;
        [SerializeField] private PerlinShakeBehaviour ShakeBehaviour;
        [SerializeField] private ColorFlashBehaviour ColorFlashBehaviour;
        [SerializeField] private VisualBar _healthBar;
        [SerializeField] private List<CanvasGroup> _thingsToHideWhenDying;
        [SerializeField] private CanvasGroup _mainImageGroup;
        [SerializeField] private ParticleSystem _deathParticleSystem;
        
        [Header("Config")]
        [FormerlySerializedAs("SecondsToMoveToAdjacentCell")]
        [SerializeField] private float _secondsToMoveToAdjacentCell;
        [SerializeField] private float _attackAnimationIntro_lengthSeconds;
        [FormerlySerializedAs("_attackAnimationIntro_curve")] 
        [SerializeField] private AnimationCurve _attackAnimationIntro_curveFromNoMove;
        [SerializeField] private AnimationCurve _attackAnimationIntro_curveFromMove;
        [SerializeField] private float _distanceTowardsTargetToMove;
        [SerializeField] private float _attackAnimationOutro_lengthSeconds;
        [SerializeField] private AnimationCurve _attackAnimationOutro_curve;
        [SerializeField] private float _attackShakeTriggerTime;
        [SerializeField] private float _timeToDisappearWhenDying = .5f;

        [HideInInspector] 
        public GridEntity Entity;
        
        public event Action KillAnimationFinishedEvent;
        
        public void Initialize(GridEntity entity, int stackOrder) {
            Entity = entity;
            
            _mainImage.sprite = entity.EntityData.BaseSprite;
            _mainImage.GetComponent<Canvas>().sortingOrder += stackOrder;
            _teamColorImage.sprite = entity.EntityData.TeamColorSprite;
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(entity.Team);
            if (player != null) {
                _teamColorImage.color = player.Data.TeamColor;
                _teamColorImage.GetComponent<Canvas>().sortingOrder += stackOrder;
            } else {
                _teamColorImage.color = Color.clear;
            }
            
            entity.PerformAnimationEvent += DoAbility;
            entity.CooldownTimerStartedEvent += CreateTimerView;
            entity.SelectedEvent += Selected;
            entity.HPHandler.AttackedEvent += AttackReceived;
            entity.HPHandler.HealedEvent += HealReceived;
            entity.KilledEvent += Killed;
            
            _healthBar.Initialize(new HealthBarLogic(entity));
            _bottomUISection.SetActive(entity.EntityData.MovementAndAttackUI);

            _particularView.Initialize(entity);
        }

        public void ToggleView(bool show) {
            _mainCanvasGroup.alpha = show ? 1 : 0;
        }

        private void OnDestroy() {
            if (Entity == null) return;
            
            Entity.PerformAnimationEvent -= DoAbility;
            Entity.CooldownTimerStartedEvent -= CreateTimerView;
            Entity.SelectedEvent -= Selected;
            Entity.HPHandler.AttackedEvent -= AttackReceived;
            Entity.HPHandler.HealedEvent -= HealReceived;
            Entity.KilledEvent -= Killed;

            if (_dying) {
                _particularView.LethalDamageReceived();
            }
        }

        private void Update() {
            UpdateMove();
            // Need to do attack after movement in order to properly handle when both are happening
            UpdateAttack();
            UpdateDeath();
        }

        private void DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            if (Entity == null || Entity.DeadOrDying) return;
            if (_particularView.DoAbility(ability, cooldownTimer)) {
                DoGenericAbility(ability);
            }
        }

        private void Selected() {
            if (Entity.EntityData.SelectionSound.Clip != null) {
                GameManager.Instance.AudioPlayer.TryPlaySFX(Entity.EntityData.SelectionSound);
            }
        }

        private async void AttackReceived(bool lethal) {
            // Delay so that the shake times up with the attacker's animation
            await Task.Delay((int)(_attackShakeTriggerTime * 1000));
            ShakeBehaviour.Shake();
            ColorFlashBehaviour.Flash();
            if (lethal) {
                _particularView.LethalDamageReceived();
            }
        }

        private void HealReceived() {
            _healAnimator.Play("Heal");
        }

        /// <summary>
        /// Catch-all for generic ability view behavior. Subclasses should call this in their default cases for their
        /// <see cref="DoAbility"/> overrides.
        /// </summary>
        private void DoGenericAbility(IAbility ability) {
            switch (ability.AbilityData) {
                case ChargeAbilityData:
                    DoGenericChargeAnimation((ChargeAbility)ability);
                    break;
                case MoveAbilityData:
                    DoGenericMoveAnimation((MoveAbility)ability);
                    break;
                case AttackAbilityData:
                    DoAttackAnimationFromAttackAbility((AttackAbility) ability);
                    break;
            }
        }

        #region Shmovement
        
        private Vector2 _movementStartPosition;
        private Vector2 _movementTargetPosition;
        private float _moveTime;
        private bool _moving;

        private void DoGenericChargeAnimation(ChargeAbility chargeAbility) {
            DoMoveAnimation(chargeAbility.AbilityParameters.MoveDestination);
            if (chargeAbility.AbilityParameters.Attacking) {
                DoGenericAttackAnimation(chargeAbility.AbilityParameters.Destination);
            }
        }
        
        private void DoGenericMoveAnimation(MoveAbility moveAbility) {
            DoMoveAnimation(moveAbility.AbilityParameters.NextMoveCell);
        }

        private void DoMoveAnimation(Vector2Int targetCell) {
            _movementStartPosition = transform.position;
            _movementTargetPosition = GameManager.Instance.GridController.GetWorldPosition(targetCell);
            _moveTime = 0;
            _moving = true;

            // Face the x-direction that we are going
            SetFacingDirection(_movementStartPosition, _movementTargetPosition);
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

        private Vector2 _attackTargetLocalPosition;
        private float _attackTime;
        private bool _attacking;
        // If true, then our attack animation is going straight from the middle of a move. Use a different animation curve so it don't look like garb.
        private bool _attackFromMove;
        private bool _triggeredAttackShake;

        private void DoAttackAnimationFromAttackAbility(AttackAbility attackAbility) {
            DoGenericAttackAnimation(attackAbility.AbilityParameters.Target.Location);
        }
        
        private void DoGenericAttackAnimation(Vector2Int? targetLocation) {
            Vector2Int? performerLocation = Entity.Location;
            if (performerLocation == null || targetLocation == null) return;
            
            _attackFromMove = _moving;
            
            // Figure out the direction from the performer location to the target location, and convert that to the distance of one cell in world space
            Vector2 performerWorldPosition = GameManager.Instance.GridController.GetWorldPosition(performerLocation.Value);
            Vector2 targetWorldPosition = GameManager.Instance.GridController.GetWorldPosition(targetLocation.Value);
            Vector2 normalizedRelativeDirection = (targetWorldPosition - performerWorldPosition).normalized;
            Vector2 targetRelativeLocation = normalizedRelativeDirection * GridController.CellWidth;
            
            // We don't want to go all the way to the target location, just part of the way
            _attackTargetLocalPosition = Vector2.Lerp(Vector2.zero, targetRelativeLocation, _distanceTowardsTargetToMove);
            
            _attackTime = 0;
            _attacking = true;
            _triggeredAttackShake = false;
            
            // Face the x-direction that we are attacking
            SetFacingDirection(performerLocation.Value, targetLocation.Value);
        }
        
        private void UpdateAttack() {
            if (!_attacking) return;

            _attackTime += Time.deltaTime;
            
            // Attack animation
            Vector3 newLocalPosition;
            if (_attackTime <= _attackAnimationIntro_lengthSeconds) {
                AnimationCurve curve = _attackFromMove ? _attackAnimationIntro_curveFromMove : _attackAnimationIntro_curveFromNoMove;
                float evaluationProgress = curve.Evaluate(_attackTime / _attackAnimationIntro_lengthSeconds);
                newLocalPosition = Vector2.Lerp(Vector2.zero, _attackTargetLocalPosition, evaluationProgress) / _mainImageGroup.transform.lossyScale;
            } else {
                float time = _attackTime - _attackAnimationIntro_lengthSeconds;
                float evaluationProgress = _attackAnimationOutro_curve.Evaluate(time / _attackAnimationOutro_lengthSeconds);
                newLocalPosition = Vector2.Lerp(Vector2.zero, _attackTargetLocalPosition, evaluationProgress) / _mainImageGroup.transform.lossyScale;
            }

            // For the purpose of this translation, reverse the direction if mirrored
            if (_directionContainer.localScale.x < 0) {
                newLocalPosition.x *= -1;
            }
            _mainImageGroup.transform.localPosition = newLocalPosition;
            
            // Attack shake
            if (!_triggeredAttackShake && _attackTime > _attackShakeTriggerTime) {
                _triggeredAttackShake = true;
                ShakeBehaviour.Shake();
            }

            if (_attackTime > _attackAnimationIntro_lengthSeconds + _attackAnimationOutro_lengthSeconds) {
                _mainImageGroup.transform.localPosition = Vector2.zero;
                _attacking = false;
            }
        }
        
        #endregion
        #region Death

        private bool _dying;
        private float _dyingTime;

        private async void Killed() {
            _dying = true;
            
            // Hide UI elements
            _thingsToHideWhenDying.ForEach(t => t.alpha = 0);
            
            // Play death particles
            SetDeathParticleColors();
            _deathParticleSystem.Play();
            
            // Mark as dead after the death particles have had some time to do their thing
            await Task.Delay(TimeSpan.FromSeconds(2f));
            KillAnimationFinished();
        }

        private void KillAnimationFinished() {
            KillAnimationFinishedEvent?.Invoke();
        }

        private void SetDeathParticleColors() {
            ParticleSystem.MainModule main = _deathParticleSystem.main;
            ParticleSystem.MinMaxGradient colors = _deathParticleSystem.main.startColor;
            PlayerData playerData = GameManager.Instance.GetPlayerForTeam(Entity.Team).Data;
            colors.colorMin = playerData.DeathParticlesColor1;
            colors.colorMax = playerData.DeathParticlesColor2;
            main.startColor = colors;
        }

        private void UpdateDeath() {
            if (!_dying) return;

            _dyingTime += Time.deltaTime;
            _mainImageGroup.alpha = Mathf.Lerp(1, 0, _dyingTime / _timeToDisappearWhenDying);
            if (_dyingTime > _timeToDisappearWhenDying) {
                _dying = false;
            }
        }
        
        #endregion
        
        // TODO can pass in things like color and timer location (maybe use a set of transform references) and stuff
        private void CreateTimerView(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            if (!ability.ShouldShowCooldownTimer) return;
            if (ability.AbilityData.AbilityTimerCooldownViewPrefab == null) return;

            Transform timerLocation = cooldownTimer.Ability switch {
                AttackAbility => transform,
                BuildAbility when cooldownTimer.Ability.AbilityData.Targeted => _buildTimerLocation,
                MoveAbility => transform,
                _ => _uniqueAbilityTimerLocation
            };
            AbilityTimerFill timerFill = cooldownTimer.Ability switch {
                AttackAbility => _attackTimerFill,
                MoveAbility => _moveTimerFill,
                _ => null
            };
            AbilityTimerCooldownView cooldownView = Instantiate(ability.AbilityData.AbilityTimerCooldownViewPrefab, timerLocation);
            cooldownView.Initialize(cooldownTimer, true, true, timerFill);
        }

        public void SetFacingDirection(Vector2 currentPosition, Vector2 targetPosition) {
            float xDifference = targetPosition.x - currentPosition.x;
            if (Mathf.Approximately(xDifference, 0)) return;
            
            bool faceRight = targetPosition.x - currentPosition.x > 0;
            Vector3 localScale = _directionContainer.transform.localScale;
            float scaleX = localScale.x;
            
            if ((faceRight && scaleX > 0) || (!faceRight && scaleX < 0)) return;
            
            localScale = new Vector3(scaleX * -1, localScale.y, localScale.z);
            _directionContainer.transform.localScale = localScale;
        }
    }
}