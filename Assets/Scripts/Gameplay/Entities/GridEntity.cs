using System;
using Mirror;
using UnityEngine;

/// <summary>
/// Represents an entity that exists at a specific position on the gameplay grid.
/// Has an <see cref="IInteractBehavior"/> field to handle player input. 
/// </summary>
public abstract class GridEntity : NetworkBehaviour {
    public enum Team {
        Neutral = -1,
        Player1 = 1,
        Player2 = 2
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

    [ClientRpc]
    public void RpcInitialize(Team team) {
        DoInitialize(team);
    }

    public void DoInitialize(Team team) {
        MyTeam = team;

        _mainSprite.sprite = MainImage;
        _teamColorSprite.color = GameManager.Instance.GetPlayer(team).Data.TeamColor;

        InteractBehavior = MyTeam switch {
            Team.Player1 => new OwnerInteractBehavior(),
            Team.Player2 => new EnemyInteractBehavior(),
            Team.Neutral => new NeutralInteractBehavior(),
            _ => throw new Exception($"Unexpected team ({MyTeam}) for entity ({UnitName})")
        };
        
        GameManager.Instance.CommandController.RegisterEntity(this);    // TODO we check for the registered flag on the entity, so it probably won't get registered twice (once from each client). But, there might be a better way to do this with authority
    }

    public abstract bool CanTargetThings();
    public abstract bool CanMove();
    
    public void Select() {
        Debug.Log($"Selecting {UnitName}");
        // Deselect the currently selected entity
        GameManager.Instance.SelectedEntity = null;
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
        GameManager.Instance.CommandController.MoveEntityToCell(this, targetCell);
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
        // For now, any attack just kills this
        Kill();
    }

    private void Kill() {
        GameManager.Instance.CommandController.UnRegisterAndDestroyEntity(this);
    }
}
