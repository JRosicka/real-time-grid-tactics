using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using UnityEngine;

[CreateAssetMenu(fileName = "Gameplay Tile", menuName = "Tiles/Rule Tile")]
public class GameplayTile : HexagonalRuleTile<GameplayTile.Neighbor> {
    [Serializable]
    public class SlowedEntityTag {
        public EntityTag Tag;
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
    public List<EntityTag> InaccessibleTags;

    [Space] 
    [Range(0, 6)]
    [Tooltip("Armor bonus (damage reduction per incoming attack) to apply to units that contain a defense boost tag")]
    [SerializeField]
    private int _armorBonus;
    /// <summary>
    /// Any entities with these tags will receive the defense bonus
    /// </summary>
    [SerializeField]
    private List<EntityTag> _defenseBoostTags;
    public int GetDefenseModifier(EntityData entityData) {
        return entityData.Tags.Any(tag => _defenseBoostTags.Contains(tag)) ? _armorBonus : 0;
    }

    public float GetMoveModifier(List<EntityTag> tags) {
        SlowedEntityTag slowedTag = SlowTags.FirstOrDefault(s => tags.Contains(s.Tag));
        return slowedTag?.SlowFactor ?? 1f;
    }

    private const string DefenseFormat = "Reduces incoming attack damage by {0} for {1}.";
    public string GetDefenseTooltip() {
        if (_defenseBoostTags.Count == 0 || _armorBonus == 0) return "";

        string tooltip = "";
        for (int i = 0; i < _defenseBoostTags.Count; i++) {
            tooltip += string.Format(DefenseFormat, _armorBonus, _defenseBoostTags[i].UnitDescriptorPlural());
            if (i < _defenseBoostTags.Count - 1) tooltip += "<br>";
        }
        return tooltip;
    }

    private const string SlowFormat = "Slows {0} by {1}%.";
    public string GetMovementTooltip() {
        if (SlowTags.Count == 0) return "";
        
        string tooltip = "";
        for (int i = 0; i < SlowTags.Count; i++) {
            tooltip += string.Format(SlowFormat, SlowTags[i].Tag.UnitDescriptorPlural(), Mathf.RoundToInt((SlowTags[i].SlowFactor - 1) * 100));
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