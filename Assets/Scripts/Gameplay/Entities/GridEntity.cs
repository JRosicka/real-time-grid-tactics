using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Represents an entity that exists at a specific position on the gameplay grid.
/// Has an <see cref="IInteractBehavior"/> field to handle player input. 
/// </summary>
public abstract class GridEntity : MonoBehaviour {
    [Header("References")]
    [SerializeField] private SpriteRenderer _mainSprite;
    [SerializeField] private SpriteRenderer _teamColorSprite;

    [Header("Config")]
    public string UnitName;
    public Sprite MainImage;
    public Sprite TeamColorImage;
    public bool IsFriendly;
    
    [HideInInspector]
    public bool Registered;
    protected IInteractBehavior InteractBehavior;

    private void Awake() {
        _mainSprite.sprite = MainImage;
        _teamColorSprite.sprite = TeamColorImage;
        
        if (IsFriendly) {
            InteractBehavior = new OwnerInteractBehavior();
        } else {
            InteractBehavior = new EnemyInteractBehavior();
        } // TODO neutral
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
    
    public void ReceiveAttackFromEntity(GridEntity sourceEntity) {
        Debug.Log($"Attacked!!!! And from a {sourceEntity.UnitName} no less! OW");
    }
}
