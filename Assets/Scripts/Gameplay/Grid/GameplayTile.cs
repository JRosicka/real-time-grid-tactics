using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using UnityEngine;

[CreateAssetMenu(fileName = "Gameplay Tile", menuName = "Tiles/Rule Tile")]
public class GameplayTile : HexagonalRuleTile<GameplayTile.Neighbor> {
    [Serializable]
    public class SlowedEntityTag {
        public EntityData.EntityTag Tag;
        public float SlowFactor;
    }
    
    public bool Selectable;
    public string DisplayName;
    public string ShortDescription;
    public string LongDescription;
    /// <summary>
    /// Any entities with these tags will be slowed when trying to move into cells of this tile type
    /// </summary>
    public List<SlowedEntityTag> SlowTags;
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

    public float GetMoveModifier(List<EntityData.EntityTag> tags) {
        SlowedEntityTag slowedTag = SlowTags.FirstOrDefault(s => tags.Contains(s.Tag));
        return slowedTag?.SlowFactor ?? 1f;
    }

    private const string DefenseFormat = "Provides a {0}% defense bonus for {1} units.";
    public string GetDefenseTooltip() {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_defenseBoostTags.Count == 0 || _defenseModifier == 1) return "";

        string tooltip = "";
        for (int i = 0; i < _defenseBoostTags.Count; i++) {
            tooltip += string.Format(DefenseFormat, Mathf.RoundToInt((1f / _defenseModifier - 1) * 100), _defenseBoostTags[i].ToString());
            if (i < _defenseBoostTags.Count - 1) tooltip += "<br>";
        }
        return tooltip;
    }

    private const string SlowFormat = "Slows {0} units by {1}%.";
    public string GetMovementTooltip() {
        if (SlowTags.Count == 0) return "";
        
        string tooltip = "";
        for (int i = 0; i < SlowTags.Count; i++) {
            tooltip += string.Format(SlowFormat, SlowTags[i].Tag.ToString(), Mathf.RoundToInt((SlowTags[i].SlowFactor - 1) * 100));
            if (i < SlowTags.Count - 1) tooltip += "<br>";
        }
        return tooltip;
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