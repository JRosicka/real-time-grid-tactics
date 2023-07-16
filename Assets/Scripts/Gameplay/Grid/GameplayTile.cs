using System.Collections;
using System.Collections.Generic;
using Gameplay.Config;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Gameplay Tile", menuName = "Tiles/Rule Tile")]
public class GameplayTile : HexagonalRuleTile<GameplayTile.Neighbor> {
    /// <summary>
    /// Any entities with these tags will be slowed when trying to move into cells of this tile type
    /// </summary>
    public List<EntityData.EntityTag> SlowTags;
    /// <summary>
    /// Any entities with these tags will not be able to move into cells of this tile type
    /// </summary>
    public List<EntityData.EntityTag> InaccessibleTags;

    public class Neighbor : HexagonalRuleTile.TilingRule.Neighbor {
        public const int Null = 3;
        public const int NotNull = 4;
    }

    // public override bool RuleMatch(int neighbor, TileBase tile) {
    //     switch (neighbor) {
    //         case Neighbor.Null: return tile == null;
    //         case Neighbor.NotNull: return tile != null;
    //     }
    //     return base.RuleMatch(neighbor, tile);
    // }
}