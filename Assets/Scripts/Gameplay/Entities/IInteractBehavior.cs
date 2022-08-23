using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles input for a <see cref="GridEntity"/>
/// </summary>
public interface IInteractBehavior {
    /// <summary>
    /// The user clicked to select this unit
    /// </summary>
    void Select(GridEntity entity);
    
    /// <summary>
    /// The user clicked to target this entity with a selected unit
    /// </summary>
    void TargetWithSelectedUnit(GridEntity targetEntity);
}
