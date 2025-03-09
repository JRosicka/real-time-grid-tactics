using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Gameplay Tile", menuName = "Tiles/Rule Tile")]
public class GameplayTile : HexagonalRuleTile<GameplayTile.Neighbor> {
    public bool Selectable;
    public string DisplayName;
    public string ShortDescription;
    public string LongDescription;
    /// <summary>
    /// Any entities with these tags will be slowed when trying to move into cells of this tile type
    /// </summary>
    public List<EntityData.EntityTag> SlowTags;
    /// <summary>
    /// Any entities with these tags will not be able to move into cells of this tile type
    /// </summary>
    public List<EntityData.EntityTag> InaccessibleTags;

    [Space] 
    [Range(0, 1)]
    [Tooltip("Percentage of base damage that affected units take when attacked on this tile. E.g. a value of .75 means that affected units take 75% of the damage they normally would.")]
    [SerializeField]
    private float _defenseModifier = 1f;
    /// <summary>
    /// Any entities with these tags will receive the defense bonus
    /// </summary>
    [SerializeField]
    private List<EntityData.EntityTag> _defenseBoostTags;
    public float GetDefenseModifier(EntityData entityData) {
        return entityData.Tags.Any(tag => _defenseBoostTags.Contains(tag)) ? _defenseModifier : 1f;
    }

    public class Neighbor : HexagonalRuleTile.TilingRule.Neighbor {
        public const int Null = 3;
        public const int NotNull = 4;
    }

    // public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
    //     bool evenY = position.y % 2 == 0;
    //     tileData.sprite = evenY ? SpriteA : SpriteB;
    // }

    // public override bool RuleMatch(int neighbor, TileBase tile) {
    //     switch (neighbor) {
    //         case Neighbor.Null: return tile == null;
    //         case Neighbor.NotNull: return tile != null;
    //     }
    //     return base.RuleMatch(neighbor, tile);
    // }
}