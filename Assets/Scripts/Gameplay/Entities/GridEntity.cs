using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Represents an entity that exists at a specific position on the gameplay grid.
/// Has an <see cref="IInteractBehavior"/> field to handle player input. 
/// </summary>
public abstract class GridEntity : MonoBehaviour, IPointerUpHandler {
    [Header("References")]
    [SerializeField] private SpriteRenderer _mainSprite;
    [SerializeField] private SpriteRenderer _teamColorSprite;

    [Header("Config")]
    public string UnitName;
    public Sprite MainImage;
    public Sprite TeamColorImage;
    
    protected IInteractBehavior InteractBehavior;

    private void Awake() {
        _mainSprite.sprite = MainImage;
        _teamColorSprite.sprite = TeamColorImage;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public abstract bool CanTargetEntities();

    public void ReceiveAttackFromEntity(GridEntity sourceEntity) {
        Debug.Log($"Attacked!!!! And from a {sourceEntity.UnitName} no less! OW");
    }

    public void OnPointerUp(PointerEventData eventData) {
        switch (eventData.button) {
            case PointerEventData.InputButton.Left:
                InteractBehavior.Select(this);
                break;
            case PointerEventData.InputButton.Right:
                InteractBehavior.TargetWithSelectedUnit(this);
                break;
            case PointerEventData.InputButton.Middle:
                // Do nothing
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
