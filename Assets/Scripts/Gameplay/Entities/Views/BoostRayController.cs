using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Handles enabling/disabling/initializing/aiming a <see cref="BoostRayEffect"/>
    /// </summary>
    public class BoostRayController : MonoBehaviour {
        [SerializeField] private BoostRayEffect _effect;

        private GridEntity _booster;
        private GridEntity _target;
        private TeamFogOfWarTracker _fowTracker;
        private bool _hiddenByFoW;
        
        public void Initialize(GridEntity booster) {
            _booster = booster;
            
            PlayerColorData colorData = GameManager.Instance.GetPlayerForTeam(booster).ColorData;
            _effect.SetColors(colorData);
            
            InitializeFogOfWar();
        }

        public void TearDown() {
            Deactivate();
            _fowTracker.FoWUpdated -= FogOfWarUpdated;
        }
        
        public void Activate(GridEntity target) {
            _target = target;
            if (target == null || _hiddenByFoW) {
                Deactivate();
                return;
            }
            if (target.View == null) return;
            
            _effect.Initialize(target.View.BoostPosition);
        }

        private void Deactivate() {
            _effect.Hide();
        }
        
        #region Fog of War

        private void InitializeFogOfWar() {
            _fowTracker = GameManager.Instance.FogOfWarManager!.GetLocalTeamTracker();
            if (_fowTracker != null) {
                TryToggleFoWHiddenState();
                _fowTracker.FoWUpdated += FogOfWarUpdated;
            }
        }
        
        private void FogOfWarUpdated(List<TeamFogOfWarTracker.FoWCell> foWCells) {
            TryToggleFoWHiddenState();
        }

        /// <summary>
        /// Assesses whether the boost effect should be hidden. If the hidden status should change, apply it.
        /// Hidden if either the booster or the target is hidden.
        /// </summary>
        private void TryToggleFoWHiddenState() {
            bool newHidden;
            if (_fowTracker.IsEntityHidden(_booster)) {
                newHidden = true;
            } else if (_target != null && _fowTracker.IsEntityHidden(_target)) {
                newHidden = true;
            } else {
                newHidden = false;
            }

            if (newHidden != _hiddenByFoW) {
                _hiddenByFoW = newHidden;
                DoToggleFoWHiddenState(newHidden);
            }
        }

        private void DoToggleFoWHiddenState(bool hidden) {
            _hiddenByFoW = hidden;
            if (hidden) {
                Deactivate();
            } else {
                Activate(_target);
            }
        }
        
        #endregion
    }
}