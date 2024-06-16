using Gameplay.Grid;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAttackLogic"/> for an archer ranged attack
    /// </summary>
    public class ArcherArrowAttackLogic : IAttackLogic {
        private ArcherArrow ArrowPrefab => GameManager.Instance.PrefabAtlas.ArcherArrow;
        private GridController GridController => GameManager.Instance.GridController;
        
        public void DoAttack(GridEntity attacker, GridEntity target) {
            Vector2Int attackLocation = attacker.Location;
            Vector3 attackWorldPosition = GridController.GetWorldPosition(attackLocation);
            
            ArcherArrow arrow = Object.Instantiate(ArrowPrefab, attackWorldPosition, Quaternion.identity, 
                GameManager.Instance.CommandManager.SpawnBucket);

            if (NetworkClient.active) {
                // MP Server
                NetworkServer.Spawn(arrow.gameObject);
                arrow.Initialize(attacker, target);
            } else if (!NetworkClient.active) {
                // SP
                arrow.Initialize(attacker, target);
            }
        }
    }
}