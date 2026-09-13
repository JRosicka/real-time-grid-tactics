using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Grid;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// A single particle system for the inspiring presence VFX. Has a relative position to its king entity. 
    /// </summary>
    public class InspiringPresencePositionedParticles : MonoBehaviour {
        [SerializeField] private bool _adjacent;
        [SerializeField] private CellDistanceLogic.DirectionAngle _relativeDirection;
        [SerializeField] private List<ParticleSystem> _particleSystems;

        private GridEntity _entity;
        private bool _active;
        
        public void Initialize(GridEntity entity, PlayerColorData colorData) {
            _entity = entity;
            entity.EntityMovedClientEvent += EntityMoved;

            foreach (ParticleSystem particles in _particleSystems) {
                ParticleSystem.MainModule main = particles.main;
                ParticleSystem.MinMaxGradient colors = main.startColor;
                colors.colorMin = colorData.BrightParticlesColor1;
                colors.colorMax = colorData.BrightParticlesColor2;
                main.startColor = colors;
            }
        }

        private Vector2Int Position => _adjacent
            ? CellDistanceLogic.NeighborInDirection(_entity.Location!.Value, _relativeDirection)
            : _entity.Location!.Value;

        public void ToggleActive(bool active) {
            _active = active;
            ToggleView(active);
        }

        public void UpdateFoW(List<FogOfWarManager.FoWCell> fowCells) {
            if (!_active) return;
            
            Vector2Int position = Position;
            FogOfWarManager.FoWCell fowCell = fowCells.FirstOrDefault(c => c.Position == position);
            if (fowCell == null) return;
            
            ToggleView(!fowCell.Hidden);
        }

        private void EntityMoved() {
            ToggleView(_active);
        }

        private void ToggleView(bool active) {
            // Additional condition: NEVER have this be active if out of bounds
            if (active && !GameManager.Instance.GridController.IsInBounds(Position)) {
                active = false;
            }
            
            if (active) {
                _particleSystems.ForEach(p => p.Play());
            } else {
                _particleSystems.ForEach(p => p.Stop(true, ParticleSystemStopBehavior.StopEmitting));
            }
        }
    }
}