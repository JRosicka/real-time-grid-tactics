using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Represents an entity that exists at a specific position on the gameplay grid.
/// Has an <see cref="IInteractBehavior"/> field to handle player input. 
/// </summary>
public abstract class GridEntity : NetworkBehaviour {
    public enum Team {
        Neutral = -1,
        One = 1,
        Two = 2
    }
    
    [Header("References")]
    [SerializeField] private SpriteRenderer _mainSprite;
    [SerializeField] private SpriteRenderer _teamColorSprite;

    [Header("Config")]
    public string UnitName;
    public Sprite MainImage;
    public Sprite TeamColorImage;
    public Team MyTeam;
    
    [HideInInspector]
    public bool Registered;
    protected IInteractBehavior InteractBehavior;

    private enum TargetType {
        Enemy = 1,
        Ally = 2,
        Neutral = 3
    }
    
    private void Awake() {
        _mainSprite.sprite = MainImage;
        _teamColorSprite.sprite = TeamColorImage;

        InteractBehavior = MyTeam switch {
            // TODO actually check MyTeam against the team in some manager
            Team.One => new OwnerInteractBehavior(),
            Team.Two => new EnemyInteractBehavior(),
            Team.Neutral => new NeutralInteractBehavior(),
            _ => throw new Exception($"Unexpected team ({MyTeam}) for entity ({UnitName})")
        };
        ;
    }
    
    void Start() {
        GameManager.Instance.EntityManager.RegisterEntity(this);
    }

    void Update()
    {
        
    }

    public abstract bool CanTargetThings();
    public abstract bool CanMove();
    
    public void Select() {
        Debug.Log($"Selecting {UnitName}");
        // Deselect the currently selected entity
        GameManager.Instance.EntityManager.SelectedEntity = null;
        InteractBehavior.Select(this);
    }
    /// <summary>
    /// Try to move or use an ability on the indicated location
    /// </summary>
    public void InteractWithCell(Vector3Int location) {
        InteractBehavior.TargetCellWithUnit(this, location);
    }

    public void MoveToCell(Vector3Int targetCell) {
        Debug.Log($"Moving {UnitName} to {targetCell}");
        GameManager.Instance.EntityManager.MoveEntityToPosition(this, targetCell);
    }

    public void TryTargetEntity(GridEntity targetEntity, Vector3Int targetCell) {
        TargetType targetType = GetTargetType(this, targetEntity);
        
        // TODO figure out if target is in range
        
        if (targetType == TargetType.Enemy) {
            targetEntity.ReceiveAttackFromEntity(this);
        }
    }
    
    private static TargetType GetTargetType(GridEntity originEntity, GridEntity targetEntity) {
        if (targetEntity.MyTeam == Team.Neutral || originEntity.MyTeam == Team.Neutral) {
            return TargetType.Neutral;
        }

        return originEntity.MyTeam == targetEntity.MyTeam ? TargetType.Ally : TargetType.Enemy;
    }

    
    public void ReceiveAttackFromEntity(GridEntity sourceEntity) {
        Debug.Log($"Attacked!!!! And from a {sourceEntity.UnitName} no less! OW");
    }
}
