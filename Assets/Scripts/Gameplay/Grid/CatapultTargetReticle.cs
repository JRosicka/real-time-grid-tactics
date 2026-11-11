using System.Collections.Generic;
using System.Linq;
using Gameplay.Entities;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Grid {
    /// <summary>
    /// A single icon indicating where a catapult is aiming or firing
    /// </summary>
    public class CatapultTargetReticle : MonoBehaviour {
        private const string FastAnimation = "FastCatapultReticleFlash";
        private const string AimAnimation = "FastCatapultReticleAim";
        private const string HideAnimationName = "HideCatapultReticle";
        
        [SerializeField] private Image _outerReticle;
        [SerializeField] private Image _innerReticle;
        [SerializeField] private Animator _animator;

        private HashSet<Vector2Int> _location;
        private bool _hiddenByFoW;
        private bool _active;
        private bool _actuallyActive;
        private string _animationName;
        private bool _markedForDestruction;
        
        private GameManager GameManager => GameManager.Instance;
        private TeamFogOfWarTracker FowTracker => GameManager.Instance.FogOfWarManager!.GetLocalTeamTracker();

        public void Initialize(GameTeam team, bool playFastAnimation) {
            IGamePlayer localPlayer = GameManager.GetPlayerForTeam(team);
            if (localPlayer == null) {
                Destroy(gameObject);
                return;
            }
            
            Color color = localPlayer.ColorData.TeamColor;
            _outerReticle.color = color;
            _innerReticle.color = color;

            _animationName = playFastAnimation ? FastAnimation : AimAnimation;
            
            // Set of fog of war
            if (GameManager.Instance.FogOfWarManager == null) {
                GameManager.Instance.FogOfWarInitialized += InitializeFoW;
            } else {
                InitializeFoW();
            }
        }
        
        public void UpdateLocation(HashSet<Vector2Int> vertex) {
            _active = true;
            
            transform.position = GameManager.Instance.GridController.GetWorldPosition(vertex);
            _location = vertex;
            
            UpdateFoWHiddenStatus(null);
            
            int yMax = vertex.Select(c => c.y).Max();
            bool wideTop = vertex.Count(c => c.y == yMax) == 2;
            transform.eulerAngles = wideTop ? new Vector3(0, 0, 0) : new Vector3(0, 0, 60);
        }

        public void Hide(bool markForDestruction) {
            _active = false;
            UpdateVisibility();
            if (markForDestruction) {
                _markedForDestruction = true;
            }
        }

        public void HideAnimationFinished() {
            if (_markedForDestruction) {
                Destroy(gameObject);
            } else {
                gameObject.SetActive(false);
            }
        }
        
        private void InitializeFoW() {
            GameManager.Instance.FogOfWarInitialized -= InitializeFoW;
            if (FowTracker == null) return;
            
            FowTracker.FoWUpdated += UpdateFoWHiddenStatus;
            UpdateFoWHiddenStatus(null);
        }

        private void UpdateFoWHiddenStatus(List<TeamFogOfWarTracker.FoWCell> _) {
            if (_location == null) return;
            if (FowTracker == null) return;

            _hiddenByFoW = _location.All(c => FowTracker.IsLocationHidden(c));
            UpdateVisibility();
        }

        private void UpdateVisibility() {
            bool wasActuallyActive = _actuallyActive;
            _actuallyActive = _active && !_hiddenByFoW;
            if (_actuallyActive == wasActuallyActive) return;
            
            if (_actuallyActive) {
                gameObject.SetActive(true);
                _animator.Play(_animationName);
            } else {
                // The animation will set the object inactive when finished
                _animator.Play(HideAnimationName);
            }
        }
    }
}