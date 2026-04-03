using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Gameplay Tile", menuName = "Tiles/Rule Tile")]
public class GameplayTile : HexagonalRuleTile<GameplayTile.Neighbor> {
    [Serializable]
    public class SlowedEntityTag {
        public EntityTag Tag;
        [Tooltip("Multiplier to apply to the entity's move time. 2 means the entity will take twice as long to move through this tile, 0.5 means it will take half as long.")]
        public float SlowFactor;
    }

    public string TileID;
    public float IconScaleInSelectionInterface = 1.4f;
    public bool Selectable;
    public string DisplayName;
    public string ShortDescription;
    public string LongDescription;
    /// <summary>
    /// Any entities with these tags will have their move time modified when trying to move into cells of this tile type
    /// </summary>
    [FormerlySerializedAs("SlowTags")] public List<SlowedEntityTag> SpeedModifierTags;
    /// <summary>
    /// Any entities with these tags will not be able to move into cells of this tile type
    /// </summary>
    public List<EntityTag> InaccessibleTags;
    /// <summary>
    /// These entities will not be able to move into cells of this tile type
    /// </summary>
    public List<EntityData> InaccessibleEntities;

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
        SlowedEntityTag slowedTag = SpeedModifierTags.FirstOrDefault(s => tags.Contains(s.Tag));
        return slowedTag?.SlowFactor ?? 1f;
    }

    public bool IsSlowed(List<EntityTag> tags) {
        return SpeedModifierTags.Any(s => tags.Contains(s.Tag) && s.SlowFactor > 1f);
    }

    private const string DefenseFormat = "{0} occupying this tile receive {1} less damage from attacks.";
    public string GetDefenseTooltip() {
        if (_defenseBoostTags.Count == 0 || _armorBonus == 0) return "";

        string tooltip = "";
        for (int i = 0; i < _defenseBoostTags.Count; i++) {
            tooltip += string.Format(DefenseFormat, _defenseBoostTags[i].UnitDescriptorPlural(), _armorBonus).FirstCharacterToUpper();
            if (i < _defenseBoostTags.Count - 1) tooltip += "<br>";
        }
        return tooltip;
    }

    private const string SlowFormat = "Slows {0} by {1}%.";
    private const string QuickenFormat = "Speeds up {0} by {1}%.";
    public string GetMovementTooltip() {
        if (SpeedModifierTags.Count == 0) return "";
        
        string tooltip = "";
        for (int i = 0; i < SpeedModifierTags.Count; i++) {
            string unitDescriptor = SpeedModifierTags[i].Tag.UnitDescriptorPlural();
            if (SpeedModifierTags[i].SlowFactor >= 1f) {
                tooltip += string.Format(SlowFormat, unitDescriptor, Mathf.RoundToInt((1 - 1 / SpeedModifierTags[i].SlowFactor) * 100));
            } else {
                tooltip += string.Format(QuickenFormat, unitDescriptor, -Mathf.RoundToInt((1 - 1 / SpeedModifierTags[i].SlowFactor) * 100));
            }
            if (i < SpeedModifierTags.Count - 1) tooltip += "<br>";
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