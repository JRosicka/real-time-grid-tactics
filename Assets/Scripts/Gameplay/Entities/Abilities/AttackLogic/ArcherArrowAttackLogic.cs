using Gameplay.Grid;
using Mirror;
using Scenes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAttackLogic"/> for an archer ranged attack
    /// </summary>
    public class ArcherArrowAttackLogic : IAttackLogic {
        private ArcherArrow ArrowPrefab => GameManager.Instance.PrefabAtlas.ArcherArrow;
        private GridController GridController => GameManager.Instance.GridController;
        
        public void DoAttack(GridEntity attacker, GridEntity target, int bonusDamage) {
            Vector2Int? attackLocation = attacker.Location;
            if (attackLocation == null) return;
            
            Vector3 attackWorldPosition = GridController.GetWorldPosition(attackLocation.Value);
            
            ArcherArrow arrow = Object.Instantiate(ArrowPrefab, attackWorldPosition, Quaternion.identity, 
                GameManager.Instance.CommandManager.SpawnBucket);

            if (GameTypeTracker.Instance.GameIsNetworked) {
                NetworkServer.Spawn(arrow.gameObject);
            }

            arrow.Initialize(attacker, target, bonusDamage);
        }
    }
}